using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Models;
using Thrivts.Application.Common.Services;
using Thrivts.Application.Features.Inventory;
using Thrivts.Domain.Common;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;
using Thrivts.Domain.Wash;

namespace Thrivts.Application.Features.Wash;

// ---------- Vendors, rates and categories ----------

public sealed record WashVendorDto(Guid Id, string Name, bool IsActive, IReadOnlyDictionary<string, decimal> Rates);

public sealed record GetWashVendorsQuery(bool IncludeInactive = false) : IQuery<IReadOnlyList<WashVendorDto>>;

public sealed class GetWashVendorsHandler(IAppDbContext db) : IQueryHandler<GetWashVendorsQuery, IReadOnlyList<WashVendorDto>>
{
    public async ValueTask<IReadOnlyList<WashVendorDto>> Handle(GetWashVendorsQuery query, CancellationToken ct)
    {
        var vendors = await db.WashVendors.AsNoTracking().Include(v => v.Rates)
            .Where(v => query.IncludeInactive || v.IsActive)
            .OrderBy(v => v.Name)
            .ToListAsync(ct);

        return vendors.Select(v => new WashVendorDto(v.Id, v.Name, v.IsActive, v.Rates.ToDictionary(r => r.Category, r => r.RatePerPiece))).ToList();
    }
}

public sealed record CreateWashVendorCommand(string Name) : ICommand<ErrorOr<Guid>>;

public sealed class CreateWashVendorValidator : AbstractValidator<CreateWashVendorCommand>
{
    public CreateWashVendorValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(WashVendor.MaxNameLength);
}

public sealed class CreateWashVendorHandler(IAppDbContext db) : ICommandHandler<CreateWashVendorCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(CreateWashVendorCommand command, CancellationToken ct)
    {
        var key = Text.Normalize(command.Name);
        if (await db.WashVendors.AnyAsync(v => v.NormalizedName == key, ct))
            return Error.Conflict(description: $"A wash vendor called {Text.Clean(command.Name)} already exists.");

        var vendor = new WashVendor(command.Name);
        db.WashVendors.Add(vendor);
        await db.SaveChangesAsync(ct);
        return vendor.Id;
    }
}

public sealed record WashRateInput(string Category, decimal Rate);

public sealed record SetWashVendorCommand(Guid VendorId, bool IsActive, IReadOnlyList<WashRateInput> Rates) : ICommand<ErrorOr<Success>>;

public sealed class SetWashVendorValidator : AbstractValidator<SetWashVendorCommand>
{
    public SetWashVendorValidator()
    {
        RuleFor(x => x.Rates).Must(r => r.Count <= WashCategories.All.Count);
        RuleForEach(x => x.Rates).ChildRules(r =>
        {
            r.RuleFor(x => x.Category).Must(WashCategories.IsValid).WithMessage("Unknown wash category.");
            r.RuleFor(x => x.Rate).InclusiveBetween(0, 100_000);
        });
    }
}

/// <summary>Saves a vendor's whole rate card at once (legacy "Save rates").</summary>
public sealed class SetWashVendorHandler(IAppDbContext db) : ICommandHandler<SetWashVendorCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetWashVendorCommand command, CancellationToken ct)
    {
        var vendor = await db.WashVendors.Include(v => v.Rates).FirstOrDefaultAsync(v => v.Id == command.VendorId, ct);
        if (vendor is null)
            return Error.NotFound(description: "Wash vendor not found.");

        vendor.SetActive(command.IsActive);
        foreach (var rate in command.Rates)
            vendor.SetRate(rate.Category, rate.Rate);

        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

public sealed record WashCategoriesDto(IReadOnlyList<string> Categories, IReadOnlyDictionary<string, string> ItemOverrides);

public sealed record GetWashCategoriesQuery : IQuery<WashCategoriesDto>;

public sealed class GetWashCategoriesHandler(IAppDbContext db) : IQueryHandler<GetWashCategoriesQuery, WashCategoriesDto>
{
    public async ValueTask<WashCategoriesDto> Handle(GetWashCategoriesQuery query, CancellationToken ct) =>
        new(WashCategories.All, await db.WashItemCategories.AsNoTracking().ToDictionaryAsync(c => c.NormalizedItemName, c => c.Category, ct));
}

/// <summary>Remembers which wash category an item belongs to, so future washes price it right.</summary>
public sealed record SetWashItemCategoryCommand(string ItemName, string Category) : ICommand<ErrorOr<Success>>;

public sealed class SetWashItemCategoryValidator : AbstractValidator<SetWashItemCategoryCommand>
{
    public SetWashItemCategoryValidator()
    {
        RuleFor(x => x.ItemName).NotEmpty().MaximumLength(Domain.Catalog.Item.MaxNameLength);
        RuleFor(x => x.Category).Must(WashCategories.IsValid).WithMessage("Unknown wash category.");
    }
}

public sealed class SetWashItemCategoryHandler(IAppDbContext db) : ICommandHandler<SetWashItemCategoryCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetWashItemCategoryCommand command, CancellationToken ct)
    {
        var key = Text.Normalize(command.ItemName);
        var existing = await db.WashItemCategories.FirstOrDefaultAsync(c => c.NormalizedItemName == key, ct);
        if (existing is null)
            db.WashItemCategories.Add(new WashItemCategory(command.ItemName, command.Category));
        else
            existing.Set(command.Category);

        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

internal static class WashCategoryLookup
{
    public static async Task<Func<string, string>> LoadAsync(IAppDbContext db, IEnumerable<string> itemNames, CancellationToken ct)
    {
        var keys = itemNames.Select(Text.Normalize).Distinct().ToList();
        var overrides = await db.WashItemCategories.AsNoTracking()
            .Where(c => keys.Contains(c.NormalizedItemName))
            .ToDictionaryAsync(c => c.NormalizedItemName, c => c.Category, ct);

        return name => overrides.GetValueOrDefault(Text.Normalize(name)) ?? WashCategories.Classify(name);
    }
}

// ---------- What can be washed ----------

public sealed record WashCandidateDto(Guid StockBalanceId, string ItemName, Grade Grade, int Available, string Category);

public sealed record WashLotCandidatesDto(Guid LotId, string LotSerial, string SupplierName, IReadOnlyList<WashCandidateDto> Items);

/// <summary>"Add from lot": every free piece of a finalized lot, with its guessed wash category.</summary>
public sealed record GetWashCandidatesQuery(string LotSerial) : IQuery<ErrorOr<WashLotCandidatesDto>>;

public sealed class GetWashCandidatesHandler(IAppDbContext db) : IQueryHandler<GetWashCandidatesQuery, ErrorOr<WashLotCandidatesDto>>
{
    public async ValueTask<ErrorOr<WashLotCandidatesDto>> Handle(GetWashCandidatesQuery query, CancellationToken ct)
    {
        var serial = query.LotSerial.Trim().ToUpperInvariant();
        var lot = await db.Lots.AsNoTracking()
            .Where(l => l.Serial == serial && l.Stage == LotStage.Finalized)
            .Select(l => new { l.Id, l.Serial, l.SupplierName })
            .FirstOrDefaultAsync(ct);
        if (lot is null)
            return Error.NotFound(description: $"No finalized lot {serial} — only co-signed stock can go to the washer.");

        var balances = await db.StockBalances.AsNoTracking()
            .Where(b => b.LotId == lot.Id && b.OnHand - b.InSacks > 0)
            .OrderBy(b => b.ItemName).ThenBy(b => b.Grade)
            .Select(b => new { b.Id, b.ItemName, b.Grade, Available = b.OnHand - b.InSacks })
            .ToListAsync(ct);

        var categoryOf = await WashCategoryLookup.LoadAsync(db, balances.Select(b => b.ItemName), ct);
        return new WashLotCandidatesDto(lot.Id, lot.Serial, lot.SupplierName,
            balances.Select(b => new WashCandidateDto(b.Id, b.ItemName, b.Grade, b.Available, categoryOf(b.ItemName))).ToList());
    }
}

// ---------- Batches ----------

public sealed record WashLineDto(
    Guid Id, Guid StockBalanceId, string ItemName, Grade Grade, string Category, int Sent, decimal PricePerPiece,
    int BackA, int BackC, int Rejected, int StillOut, decimal AmountDue);

public sealed record WashBatchDto(
    Guid Id, string Number, Guid LotId, string LotSerial, Guid VendorId, string VendorName, DateOnly SentDate, DateOnly? ReceivedDate,
    WashStatus Status, WashPaymentStatus PaymentStatus, int SentPieces, int Good, int Rejected, int StillOut,
    decimal SentValue, decimal AmountDue, string? Notes, DateTimeOffset CreatedAt, IReadOnlyList<WashLineDto> Lines);

internal static class WashMapping
{
    public static WashBatchDto ToDto(WashBatch b) => new(
        b.Id, b.Number, b.LotId, b.LotSerial, b.VendorId, b.VendorName, b.SentDate, b.ReceivedDate, b.Status, b.PaymentStatus,
        b.SentPieces, b.Lines.Sum(l => l.Good), b.Lines.Sum(l => l.Rejected), b.StillOut,
        b.Lines.Sum(l => l.Sent * l.PricePerPiece), b.AmountDue, b.Notes, b.CreatedAt,
        b.Lines.Select(l => new WashLineDto(l.Id, l.StockBalanceId, l.ItemName, l.Grade, l.Category, l.Sent, l.PricePerPiece,
            l.BackA, l.BackC, l.Rejected, l.StillOut, l.AmountDue)).ToList());
}

public sealed record GetWashBatchesQuery(WashStatus? Status, string? Search, string? Cursor, int? Size) : IQuery<CursorPage<WashBatchDto>>;

public sealed class GetWashBatchesHandler(IAppDbContext db) : IQueryHandler<GetWashBatchesQuery, CursorPage<WashBatchDto>>
{
    public async ValueTask<CursorPage<WashBatchDto>> Handle(GetWashBatchesQuery query, CancellationToken ct)
    {
        var size = CursorPage<WashBatchDto>.NormalizeSize(query.Size);
        IQueryable<WashBatch> batches = db.WashBatches.AsNoTracking().Include(b => b.Lines);
        if (query.Status is { } status)
            batches = batches.Where(b => b.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            batches = batches.Where(b => b.Number.ToLower().Contains(term) || b.VendorName.ToLower().Contains(term));
        }

        if (Cursor.Decode(query.Cursor) is { } c)
            batches = batches.Where(b => b.CreatedAt < c.At || (b.CreatedAt == c.At && b.Id.CompareTo(c.Id) < 0));

        var page = await batches.OrderByDescending(b => b.CreatedAt).ThenByDescending(b => b.Id).Take(size).ToListAsync(ct);
        var next = page.Count == size ? new Cursor(page[^1].CreatedAt, page[^1].Id).Encode() : null;
        return new CursorPage<WashBatchDto>(page.Select(WashMapping.ToDto).ToList(), next);
    }
}

public sealed record GetWashBatchQuery(Guid Id) : IQuery<ErrorOr<WashBatchDto>>;

public sealed class GetWashBatchHandler(IAppDbContext db) : IQueryHandler<GetWashBatchQuery, ErrorOr<WashBatchDto>>
{
    public async ValueTask<ErrorOr<WashBatchDto>> Handle(GetWashBatchQuery query, CancellationToken ct)
    {
        var batch = await db.WashBatches.AsNoTracking().Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == query.Id, ct);
        return batch is null ? Error.NotFound(description: "Wash not found.") : WashMapping.ToDto(batch);
    }
}

public sealed record WashSummaryDto(int Batches, int AtWasher, int StillOutPieces, int ReadyToFinalize, decimal UnpaidAmount);

public sealed record GetWashSummaryQuery : IQuery<WashSummaryDto>;

public sealed class GetWashSummaryHandler(IAppDbContext db) : IQueryHandler<GetWashSummaryQuery, WashSummaryDto>
{
    public async ValueTask<WashSummaryDto> Handle(GetWashSummaryQuery query, CancellationToken ct)
    {
        var batches = db.WashBatches.AsNoTracking();
        var count = await batches.CountAsync(ct);
        var atWasher = await batches.CountAsync(b => b.Status == WashStatus.Sent || b.Status == WashStatus.Partial, ct);
        var ready = await batches.CountAsync(b => b.Status == WashStatus.Received, ct);
        var stillOut = await (from b in batches from l in b.Lines select (int?)(l.Sent - l.BackA - l.BackC - l.Rejected)).SumAsync(ct) ?? 0;
        var unpaid = await (from b in batches where b.PaymentStatus != WashPaymentStatus.Paid from l in b.Lines select (decimal?)((l.BackA + l.BackC) * l.PricePerPiece)).SumAsync(ct) ?? 0;
        return new WashSummaryDto(count, atWasher, stillOut, ready, unpaid);
    }
}

public sealed record SendToWashLine(Guid StockBalanceId, int Quantity, string? Category, decimal? PricePerPiece);

/// <summary>Logs a wash: the pieces leave stock for the washer in one transaction, so they can't be
/// packed or dispatched while they're away. Category and price default from the item's wash
/// category and the vendor's rate card.</summary>
public sealed record SendToWashCommand(Guid LotId, Guid VendorId, DateOnly? SentDate, string? Notes, IReadOnlyList<SendToWashLine> Lines) : ICommand<ErrorOr<WashBatchDto>>;

public sealed class SendToWashValidator : AbstractValidator<SendToWashCommand>
{
    public SendToWashValidator(IClock clock)
    {
        RuleFor(x => x.SentDate).LessThanOrEqualTo(_ => clock.Today).When(x => x.SentDate is not null).WithMessage("The sent date can't be in the future.");
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one item with pieces.")
            .Must(l => l.Count <= WashBatch.MaxLines)
            .Must(l => l.Select(x => x.StockBalanceId).Distinct().Count() == l.Count).WithMessage("Each item and grade can appear only once.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.StockBalanceId).NotEmpty();
            l.RuleFor(x => x.Quantity).InclusiveBetween(1, 1_000_000);
            l.RuleFor(x => x.Category!).Must(WashCategories.IsValid).When(x => x.Category is not null).WithMessage("Unknown wash category.");
            l.RuleFor(x => x.PricePerPiece).InclusiveBetween(0, 100_000).When(x => x.PricePerPiece is not null);
        });
    }
}

public sealed class SendToWashHandler(IAppDbContext db, ISerialGenerator serials, StockLedger ledger, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<SendToWashCommand, ErrorOr<WashBatchDto>>
{
    public async ValueTask<ErrorOr<WashBatchDto>> Handle(SendToWashCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<WashBatch>>(async token =>
        {
            var lot = await db.Lots.AsNoTracking().FirstOrDefaultAsync(l => l.Id == command.LotId, token);
            if (lot is null || lot.Stage != LotStage.Finalized)
                return Error.NotFound(description: "Only a finalized lot can be sent to the washer.");

            var vendor = await db.WashVendors.AsNoTracking().Include(v => v.Rates).FirstOrDefaultAsync(v => v.Id == command.VendorId, token);
            if (vendor is null)
                return Error.NotFound(description: "Wash vendor not found.");

            var ids = command.Lines.Select(l => l.StockBalanceId).ToList();
            var balances = await db.StockBalances.AsNoTracking().Where(b => ids.Contains(b.Id)).ToDictionaryAsync(b => b.Id, token);
            if (balances.Count != ids.Count || balances.Values.Any(b => b.LotId != lot.Id))
                return Error.Validation(description: $"One lot per wash — every item must be stock from {lot.Serial}.");

            var categoryOf = await WashCategoryLookup.LoadAsync(db, balances.Values.Select(b => b.ItemName), token);
            var number = $"{lot.Serial}-W{await serials.NextAsync($"wash:{lot.Serial}", token)}";
            var batch = new WashBatch(number, lot.Id, lot.Serial, vendor, command.SentDate ?? clock.Today, command.Notes?.Trim());
            foreach (var input in command.Lines)
            {
                var balance = balances[input.StockBalanceId];
                var category = input.Category ?? categoryOf(balance.ItemName);
                batch.AddLine(balance, category, input.Quantity, input.PricePerPiece ?? vendor.RateFor(category));
                await ledger.SendToWashAsync(balance, input.Quantity, batch.Id, number, token);
            }

            db.WashBatches.Add(batch);
            activity.Record(ActivityArea.Wash, "sent", $"{number}: {batch.SentPieces:N0} pcs to {vendor.Name}", batch.Id);
            await db.SaveChangesAsync(token);
            return batch;
        }, ct);

        if (result.IsError)
            return result.Errors;

        await realtime.PublishAsync(RealtimeTopics.Stock, RealtimeAudience.Staff, null, ct);
        await realtime.PublishAsync(RealtimeTopics.Wash, RealtimeAudience.All, null, ct);
        return WashMapping.ToDto(result.Value);
    }
}

public sealed record ReceiveWashLine(Guid LineId, int BackA, int BackC, int Rejected, decimal PricePerPiece, string Category);

/// <summary>
/// Saves what has come back from the washer (running totals per line, like the legacy form —
/// saving again with different numbers corrects the earlier save). Good pieces go back into stock in
/// the grade they came back as; ruined pieces become rejections owed back to the supplier.
/// </summary>
public sealed record ReceiveWashCommand(Guid Id, IReadOnlyList<ReceiveWashLine> Lines, WashPaymentStatus? PaymentStatus) : ICommand<ErrorOr<WashBatchDto>>;

public sealed class ReceiveWashValidator : AbstractValidator<ReceiveWashCommand>
{
    public ReceiveWashValidator()
    {
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= WashBatch.MaxLines)
            .Must(l => l.Select(x => x.LineId).Distinct().Count() == l.Count).WithMessage("Each line can appear only once.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.BackA).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.BackC).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.Rejected).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.PricePerPiece).InclusiveBetween(0, 100_000);
            l.RuleFor(x => x.Category).Must(WashCategories.IsValid).WithMessage("Unknown wash category.");
        });
    }
}

public sealed class ReceiveWashHandler(IAppDbContext db, StockLedger ledger, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<ReceiveWashCommand, ErrorOr<WashBatchDto>>
{
    public async ValueTask<ErrorOr<WashBatchDto>> Handle(ReceiveWashCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<WashBatch>>(async token =>
        {
            var batch = await db.WashBatches.Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == command.Id, token);
            if (batch is null)
                return Error.NotFound(description: "Wash not found.");

            var deltas = command.Lines
                .Select(l => batch.Receive(l.LineId, l.BackA, l.BackC, l.Rejected, l.PricePerPiece, l.Category, clock.Today))
                .ToList();
            if (command.PaymentStatus is { } payment)
                batch.SetPaymentStatus(payment);

            var lotItemIds = batch.Lines.Select(l => l.LotItemId).Distinct().ToList();
            var balances = await db.StockBalances.AsNoTracking().Where(b => lotItemIds.Contains(b.LotItemId)).ToListAsync(token);
            var lotItems = await db.LotItems.Where(i => lotItemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, token);

            async Task<StockBalance> BalanceAsync(WashLine line, Grade grade)
            {
                var found = balances.FirstOrDefault(b => b.LotItemId == line.LotItemId && b.Grade == grade);
                if (found is not null)
                    return found;

                // Came back in a grade this item never had: open that balance first.
                var source = balances.First(b => b.Id == line.StockBalanceId);
                var opened = StockBalance.OpenEmpty(source, grade);
                db.StockBalances.Add(opened);
                await db.SaveChangesAsync(token);
                balances.Add(opened);
                return opened;
            }

            // Undo first (pieces return to "at the washer"), then apply additions, so a corrected
            // save never needs more pieces at the washer than there really are.
            foreach (var pass in new[] { -1, 1 })
            {
                foreach (var d in deltas)
                {
                    var source = balances.First(b => b.Id == d.Line.StockBalanceId);
                    foreach (var (grade, qty) in new[] { (Grade.A, d.DeltaA), (Grade.C, d.DeltaC) })
                    {
                        if (Math.Sign(qty) == pass)
                            await ledger.WashBackAsync(source, await BalanceAsync(d.Line, grade), qty, batch.Id, batch.Number, token);
                    }

                    if (Math.Sign(d.DeltaRejected) == pass)
                    {
                        await ledger.WashRejectAsync(source, d.DeltaRejected, token);
                        lotItems[d.Line.LotItemId].ApplyWashRejected(d.DeltaRejected);
                    }
                }
            }

            var good = deltas.Sum(d => d.DeltaA + d.DeltaC);
            var rejected = deltas.Sum(d => d.DeltaRejected);
            if (good != 0 || rejected != 0)
                activity.Record(ActivityArea.Wash, "received", $"{batch.Number}: {good:+#;-#;0} good, {rejected:+#;-#;0} rejected — {batch.StillOut:N0} still at {batch.VendorName}", batch.Id);

            await db.SaveChangesAsync(token);
            return batch;
        }, ct);

        if (result.IsError)
            return result.Errors;

        await realtime.PublishAsync(RealtimeTopics.Stock, RealtimeAudience.All, null, ct);
        await realtime.PublishAsync(RealtimeTopics.Wash, RealtimeAudience.All, null, ct);
        return WashMapping.ToDto(result.Value);
    }
}

public sealed record FinalizeWashCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class FinalizeWashHandler(IAppDbContext db, ICurrentUser user, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<FinalizeWashCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(FinalizeWashCommand command, CancellationToken ct)
    {
        var batch = await db.WashBatches.Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == command.Id, ct);
        if (batch is null)
            return Error.NotFound(description: "Wash not found.");

        batch.FinalizeForPayment(user.RequireUserId(), clock.UtcNow);
        activity.Record(ActivityArea.Wash, "finalized", $"{batch.Number} finalized for payment — Rs {batch.AmountDue:N0} to {batch.VendorName}", batch.Id);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Wash, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

public sealed record ReopenWashCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class ReopenWashHandler(IAppDbContext db, ActivityRecorder activity, IRealtimeNotifier realtime) : ICommandHandler<ReopenWashCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(ReopenWashCommand command, CancellationToken ct)
    {
        var batch = await db.WashBatches.Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == command.Id, ct);
        if (batch is null)
            return Error.NotFound(description: "Wash not found.");

        batch.Reopen();
        activity.Record(ActivityArea.Wash, "reopened", $"{batch.Number} reopened", batch.Id);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Wash, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

public sealed record SetWashPaymentCommand(Guid Id, WashPaymentStatus Status) : ICommand<ErrorOr<Success>>;

public sealed class SetWashPaymentHandler(IAppDbContext db, IRealtimeNotifier realtime) : ICommandHandler<SetWashPaymentCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetWashPaymentCommand command, CancellationToken ct)
    {
        var batch = await db.WashBatches.FirstOrDefaultAsync(b => b.Id == command.Id, ct);
        if (batch is null)
            return Error.NotFound(description: "Wash not found.");

        batch.SetPaymentStatus(command.Status);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Wash, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}

/// <summary>Cancels a wash that was logged by mistake. Only while nothing has come back; the
/// pieces return to stock.</summary>
public sealed record DeleteWashCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class DeleteWashHandler(IAppDbContext db, StockLedger ledger, ICurrentUser user, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<DeleteWashCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DeleteWashCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<Success>>(async token =>
        {
            var batch = await db.WashBatches.Include(b => b.Lines).FirstOrDefaultAsync(b => b.Id == command.Id, token);
            if (batch is null)
                return Error.NotFound(description: "Wash not found.");

            batch.EnsureCancellable();
            var ids = batch.Lines.Select(l => l.StockBalanceId).ToList();
            var balances = await db.StockBalances.AsNoTracking().Where(b => ids.Contains(b.Id)).ToDictionaryAsync(b => b.Id, token);
            foreach (var line in batch.Lines)
                await ledger.CancelWashAsync(balances[line.StockBalanceId], line.Sent, batch.Id, batch.Number, token);

            batch.MarkDeleted(clock.UtcNow, user.UserId);
            activity.Record(ActivityArea.Wash, "deleted", $"{batch.Number} cancelled — {batch.SentPieces:N0} pcs back in stock", batch.Id);
            await db.SaveChangesAsync(token);
            return Result.Success;
        }, ct);

        if (!result.IsError)
        {
            await realtime.PublishAsync(RealtimeTopics.Stock, RealtimeAudience.All, null, ct);
            await realtime.PublishAsync(RealtimeTopics.Wash, RealtimeAudience.All, null, ct);
        }

        return result;
    }
}
