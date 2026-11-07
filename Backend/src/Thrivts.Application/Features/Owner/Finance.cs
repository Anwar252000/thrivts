using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Services;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Common;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Finance;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;

namespace Thrivts.Application.Features.Owner;

public sealed record HisabRowDto(Guid SupplierId, string Name, string? City, string? Phone, int Invoices, int NewLots, decimal Billed, decimal Paid, decimal Due);

public sealed record HisabDto(decimal TotalDue, int Suppliers, int WithBalance, IReadOnlyList<HisabRowDto> Rows);

internal static class Ledger
{
    public static async Task<Dictionary<Guid, (decimal Billed, int Invoices)>> BilledAsync(IAppDbContext db, CancellationToken ct) =>
        (await db.VendorInvoices.AsNoTracking().GroupBy(i => i.SupplierId)
            .Select(g => new { g.Key, Billed = g.Sum(i => i.Total), Count = g.Count() }).ToListAsync(ct))
        .ToDictionary(x => x.Key, x => (x.Billed, x.Count));

    public static async Task<Dictionary<Guid, decimal>> PaidAsync(IAppDbContext db, CancellationToken ct) =>
        (await db.VendorPayments.AsNoTracking().GroupBy(p => p.SupplierId)
            .Select(g => new { g.Key, Paid = g.Sum(p => p.Amount) }).ToListAsync(ct))
        .ToDictionary(x => x.Key, x => x.Paid);

    /// <summary>A lot shows under its supplier as soon as its count is finalized, until it has an invoice.</summary>
    public static IQueryable<Lot> Uninvoiced(IAppDbContext db) =>
        db.Lots.AsNoTracking().Where(l => l.CountFinalizedAt != null && !db.VendorInvoices.Any(i => i.LotId == l.Id));
}

/// <summary>Each supplier is its own account: billed (invoice totals) − paid = due.</summary>
public sealed record GetHisabQuery(string? Search) : IQuery<HisabDto>;

public sealed class GetHisabHandler(IAppDbContext db) : IQueryHandler<GetHisabQuery, HisabDto>
{
    public async ValueTask<HisabDto> Handle(GetHisabQuery query, CancellationToken ct)
    {
        var suppliers = await db.Suppliers.AsNoTracking().Where(s => !s.IsArchived).Select(s => new { s.Id, s.Name, s.City, s.Phone }).ToListAsync(ct);
        var billed = await Ledger.BilledAsync(db, ct);
        var paid = await Ledger.PaidAsync(db, ct);
        var newLots = await Ledger.Uninvoiced(db).GroupBy(l => l.SupplierId).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        var rows = suppliers.Select(s =>
        {
            var (b, invoices) = billed.GetValueOrDefault(s.Id);
            var p = paid.GetValueOrDefault(s.Id);
            return new HisabRowDto(s.Id, s.Name, s.City, s.Phone, invoices, newLots.GetValueOrDefault(s.Id), b, p, Math.Max(0, b - p));
        });

        if (!string.IsNullOrWhiteSpace(query.Search))
            rows = rows.Where(r => r.Name.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase));

        var list = rows.OrderByDescending(r => r.Due).ThenByDescending(r => r.NewLots).ThenBy(r => r.Name).ToList();
        return new HisabDto(list.Sum(r => r.Due), suppliers.Count, list.Count(r => r.Due > 0), list);
    }
}

public sealed record InvoiceSummaryDto(Guid Id, string Number, string? LotSerial, InvoiceMode Mode, InvoiceStatus Status, decimal Total, int Rejected, int Accepted, int Returned, string? RejectionReasons, DateTimeOffset GeneratedAt);

public sealed record PaymentDto(Guid Id, decimal Amount, PaymentMethod Method, DateOnly PaidOn, string? Note, string? InvoiceNumber);

public sealed record NewLotDto(Guid LotId, string Serial, int Pieces, LotStage Stage, DateOnly ArrivalDate);

public sealed record SupplierAccountDto(Guid Id, string Name, string? Phone, string? City, string? Since, string? Notes, decimal Billed, decimal Paid, decimal Due,
    IReadOnlyList<NewLotDto> NewLots, IReadOnlyList<InvoiceSummaryDto> Invoices, IReadOnlyList<PaymentDto> Payments);

public sealed record GetSupplierAccountQuery(Guid SupplierId) : IQuery<ErrorOr<SupplierAccountDto>>;

public sealed class GetSupplierAccountHandler(IAppDbContext db) : IQueryHandler<GetSupplierAccountQuery, ErrorOr<SupplierAccountDto>>
{
    public async ValueTask<ErrorOr<SupplierAccountDto>> Handle(GetSupplierAccountQuery query, CancellationToken ct)
    {
        var supplier = await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == query.SupplierId, ct);
        if (supplier is null)
            return Error.NotFound(description: "Supplier not found.");

        var invoices = await db.VendorInvoices.AsNoTracking().Where(i => i.SupplierId == supplier.Id).OrderByDescending(i => i.GeneratedAt)
            .Select(i => new
            {
                i.Id, i.Number, i.LotSerial, i.Mode, i.Status, i.Total, i.GeneratedAt,
                Rejected = i.Rejections.Sum(r => r.Rejected),
                Accepted = i.Rejections.Sum(r => r.Accepted),
                Reasons = i.Rejections.Where(r => r.Reason != null).Select(r => r.ItemName + ": " + r.Reason).ToList(),
            })
            .ToListAsync(ct);

        var payments = await (from p in db.VendorPayments.AsNoTracking()
                              join i in db.VendorInvoices.AsNoTracking() on p.VendorInvoiceId equals i.Id into inv
                              from i in inv.DefaultIfEmpty()
                              where p.SupplierId == supplier.Id
                              orderby p.PaidOn descending, p.CreatedAt descending
                              select new PaymentDto(p.Id, p.Amount, p.Method, p.PaidOn, p.Note, i != null ? i.Number : null))
            .ToListAsync(ct);

        var newLots = await Ledger.Uninvoiced(db).Where(l => l.SupplierId == supplier.Id).OrderByDescending(l => l.GatedAt)
            .Select(l => new NewLotDto(l.Id, l.Serial, l.Stage == LotStage.Finalized ? l.Items.Sum(i => i.QtyA + i.QtyC) : l.Items.Sum(i => i.Received), l.Stage, l.ArrivalDate))
            .ToListAsync(ct);

        var billed = invoices.Sum(i => i.Total);
        var paid = payments.Sum(p => p.Amount);
        return new SupplierAccountDto(supplier.Id, supplier.Name, supplier.Phone, supplier.City, supplier.Since, supplier.Notes, billed, paid, Math.Max(0, billed - paid), newLots,
            invoices.Select(i => new InvoiceSummaryDto(i.Id, i.Number, i.LotSerial, i.Mode, i.Status, i.Total, i.Rejected, i.Accepted, i.Rejected - i.Accepted,
                i.Reasons.Count == 0 ? null : string.Join("; ", i.Reasons), i.GeneratedAt)).ToList(),
            payments);
    }
}

public sealed record SaveSupplierCommand(Guid? Id, string Name, string? Phone, string? City, string? Since, string? Notes) : ICommand<ErrorOr<Guid>>;

public sealed class SaveSupplierValidator : AbstractValidator<SaveSupplierCommand>
{
    public SaveSupplierValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Supplier.MaxNameLength);
        RuleFor(x => x.Phone).MaximumLength(40);
        RuleFor(x => x.City).MaximumLength(60);
        RuleFor(x => x.Since).MaximumLength(40);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class SaveSupplierHandler(IAppDbContext db, ICacheService cache) : ICommandHandler<SaveSupplierCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(SaveSupplierCommand command, CancellationToken ct)
    {
        var key = Text.Normalize(command.Name);
        if (await db.Suppliers.AnyAsync(s => s.NormalizedName == key && s.Id != command.Id, ct))
            return Error.Conflict(description: "A supplier with that name already exists.");

        Supplier supplier;
        if (command.Id is { } id)
        {
            var existing = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, ct);
            if (existing is null)
                return Error.NotFound(description: "Supplier not found.");
            existing.Rename(command.Name);
            supplier = existing;
        }
        else
        {
            supplier = new Supplier(command.Name);
            db.Suppliers.Add(supplier);
        }

        supplier.UpdateDetails(command.Phone?.Trim(), command.City?.Trim(), command.Since?.Trim(), command.Notes?.Trim());
        await db.SaveChangesAsync(ct);
        await cache.InvalidateAsync(CacheTags.Suppliers, ct);
        return supplier.Id;
    }
}

// ---------- Invoice builder ----------

public sealed record InvoiceDraftLine(string ItemName, int Pieces, decimal? DefaultBuyPrice);

public sealed record InvoiceDraftRejection(string ItemName, int Rejected, string? Reason);

public sealed record InvoiceDraftDto(Guid LotId, string LotSerial, Guid SupplierId, string SupplierName, InvoiceMode SuggestedMode, int TotalPieces,
    IReadOnlyList<InvoiceDraftLine> Lines, IReadOnlyList<InvoiceDraftRejection> Rejections);

/// <summary>Starting point for the invoice builder: the lot's items (net A+C once graded, received
/// before), each item's current buy price, and the rejections with the supervisor's reasons.</summary>
public sealed record GetInvoiceDraftQuery(Guid LotId) : IQuery<ErrorOr<InvoiceDraftDto>>;

public sealed class GetInvoiceDraftHandler(IAppDbContext db) : IQueryHandler<GetInvoiceDraftQuery, ErrorOr<InvoiceDraftDto>>
{
    public async ValueTask<ErrorOr<InvoiceDraftDto>> Handle(GetInvoiceDraftQuery query, CancellationToken ct)
    {
        var lot = await db.Lots.AsNoTracking().Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == query.LotId, ct);
        if (lot is null)
            return Error.NotFound(description: "Lot not found.");
        if (lot.CountFinalizedAt is null)
            return Error.Validation(description: "The count for this lot isn't finalized yet.");

        var graded = lot.Stage is LotStage.AwaitingCosign or LotStage.Finalized;
        var keys = lot.Items.Select(i => i.NormalizedItemName).ToList();
        var prices = await (from i in db.Items.AsNoTracking()
                            join p in db.ItemPrices.AsNoTracking() on i.Id equals p.ItemId
                            where keys.Contains(i.NormalizedName)
                            select new { i.NormalizedName, p.BuyPrice })
            .ToDictionaryAsync(x => x.NormalizedName, x => x.BuyPrice, ct);

        // Pieces the washer ruined count as rejections owed back to the supplier, not as stock.
        var lines = lot.Items.OrderBy(i => i.SortOrder)
            .Select(i => new InvoiceDraftLine(i.ItemName, graded ? i.QtyA + i.QtyC - i.WashRejected : i.Received, prices.GetValueOrDefault(i.NormalizedItemName)))
            .ToList();
        var rejections = graded
            ? lot.Items.Where(i => i.TotalRejected > 0)
                .Select(i => new InvoiceDraftRejection(i.ItemName, i.TotalRejected, RejectionLabel(i)))
                .ToList()
            : [];

        return new InvoiceDraftDto(lot.Id, lot.Serial, lot.SupplierId, lot.SupplierName, graded ? InvoiceMode.Final : InvoiceMode.NonGraded,
            lines.Sum(l => l.Pieces), lines, rejections);
    }

    private static string? RejectionLabel(LotItem item) => (item.Rejected, item.WashRejected) switch
    {
        (> 0, > 0) => $"{item.RejectionReason} + {item.WashRejected} failed wash",
        (0, > 0) => "Failed wash",
        _ => item.RejectionReason?.ToString(),
    };
}

public sealed record InvoiceLineInput(string ItemName, int Pieces, decimal BuyPrice, IReadOnlyList<string>? MergedFrom);

public sealed record InvoiceRejectionInput(string ItemName, int Rejected, string? Reason, int Accepted, decimal AcceptPrice);

public sealed record CreateInvoiceCommand(Guid LotId, InvoiceMode Mode, IReadOnlyList<InvoiceLineInput> Lines, IReadOnlyList<InvoiceRejectionInput>? Rejections)
    : ICommand<ErrorOr<Guid>>;

public sealed class CreateInvoiceValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceValidator()
    {
        RuleFor(x => x.Lines).NotEmpty().Must(l => l.Count <= 300);
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.ItemName).NotEmpty().MaximumLength(160);
            l.RuleFor(x => x.Pieces).InclusiveBetween(0, 10_000_000);
            l.RuleFor(x => x.BuyPrice).InclusiveBetween(0, 10_000_000);
        });
        RuleForEach(x => x.Rejections).ChildRules(r =>
        {
            r.RuleFor(x => x.ItemName).NotEmpty().MaximumLength(Item.MaxNameLength);
            r.RuleFor(x => x.Reason).MaximumLength(60);
            r.RuleFor(x => x.Accepted).GreaterThanOrEqualTo(0);
            r.RuleFor(x => x.AcceptPrice).InclusiveBetween(0, 10_000_000);
        });
    }
}

/// <summary>
/// Generates the vendor invoice for a lot and posts its total to the supplier's hisab. The client
/// sends lines and prices but never the totals: they are recomputed here. Lines may be merged and
/// renamed, but the total pieces must equal what the lot actually has, and rejection counts must
/// match the lot — so the invoice can't silently drop or invent pieces.
/// </summary>
public sealed class CreateInvoiceHandler(IAppDbContext db, ISerialGenerator serials, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<CreateInvoiceCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(CreateInvoiceCommand command, CancellationToken ct)
    {
        var draft = await new GetInvoiceDraftHandler(db).Handle(new GetInvoiceDraftQuery(command.LotId), ct);
        if (draft.IsError)
            return draft.Errors;

        var expected = draft.Value;
        if (command.Mode == InvoiceMode.Final && expected.SuggestedMode != InvoiceMode.Final)
            return Error.Validation(description: "This lot isn't graded yet — generate a non-graded invoice.");
        if (command.Lines.Sum(l => l.Pieces) != expected.TotalPieces)
            return Error.Validation(description: $"Invoice lines must cover exactly {expected.TotalPieces:N0} pieces (they total {command.Lines.Sum(l => l.Pieces):N0}).");

        var lotRejections = expected.Rejections.ToDictionary(r => Text.Normalize(r.ItemName), r => r.Rejected);
        foreach (var r in command.Rejections ?? [])
        {
            if (!lotRejections.TryGetValue(Text.Normalize(r.ItemName), out var actual) || actual != r.Rejected)
                return Error.Validation(description: $"{r.ItemName}: rejection count doesn't match the lot — reload the invoice.");
        }

        var id = await db.InTransactionAsync<ErrorOr<Guid>>(async token =>
        {
            if (await db.VendorInvoices.AnyAsync(i => i.LotId == command.LotId, token))
                return Error.Conflict(description: "This lot already has an invoice.");

            var number = $"INV-{await serials.NextAsync(SerialCounters.VendorInvoice, token):0000}";
            var invoice = new VendorInvoice(number, expected.SupplierId, expected.SupplierName, expected.LotId, expected.LotSerial, command.Mode, clock.UtcNow);
            foreach (var line in command.Lines)
                invoice.AddLine(line.ItemName.Trim(), line.Pieces, line.BuyPrice, line.MergedFrom);
            foreach (var r in command.Rejections ?? [])
                invoice.AddRejection(r.ItemName, r.Rejected, r.Reason, r.Accepted, r.AcceptPrice);

            db.VendorInvoices.Add(invoice);
            activity.Record(ActivityArea.Lots, "invoiced", $"Invoice {number} for {expected.LotSerial} — {expected.SupplierName}", invoice.Id);
            await db.SaveChangesAsync(token);
            return invoice.Id;
        }, ct);

        if (!id.IsError)
        {
            await realtime.PublishAsync(RealtimeTopics.Finance, RealtimeAudience.Owner, null, ct);
            await realtime.PublishAsync(RealtimeTopics.Lots, RealtimeAudience.Owner, null, ct);
        }

        return id;
    }
}

public sealed record InvoiceLineDto(string ItemName, int Pieces, decimal BuyPrice, decimal LineTotal, bool IsAcceptedRejection, IReadOnlyList<string>? MergedFrom);

public sealed record InvoiceRejectionDto(string ItemName, int Rejected, string? Reason, int Accepted, decimal AcceptPrice, int Returned);

public sealed record InvoiceDto(Guid Id, string Number, Guid SupplierId, string SupplierName, string? LotSerial, InvoiceMode Mode, InvoiceStatus Status, DateTimeOffset GeneratedAt,
    decimal Total, IReadOnlyList<InvoiceLineDto> Lines, IReadOnlyList<InvoiceRejectionDto> Rejections);

public sealed record GetInvoiceQuery(Guid Id) : IQuery<ErrorOr<InvoiceDto>>;

public sealed class GetInvoiceHandler(IAppDbContext db) : IQueryHandler<GetInvoiceQuery, ErrorOr<InvoiceDto>>
{
    public async ValueTask<ErrorOr<InvoiceDto>> Handle(GetInvoiceQuery query, CancellationToken ct)
    {
        var invoice = await db.VendorInvoices.AsNoTracking()
            .Where(i => i.Id == query.Id)
            .Select(i => new InvoiceDto(i.Id, i.Number, i.SupplierId, i.SupplierName, i.LotSerial, i.Mode, i.Status, i.GeneratedAt, i.Total,
                i.Lines.Select(l => new InvoiceLineDto(l.ItemName, l.Pieces, l.BuyPrice, l.LineTotal, l.IsAcceptedRejection, l.MergedFrom)).ToList(),
                i.Rejections.Select(r => new InvoiceRejectionDto(r.ItemName, r.Rejected, r.Reason, r.Accepted, r.AcceptPrice, r.Rejected - r.Accepted)).ToList()))
            .FirstOrDefaultAsync(ct);

        return invoice is null ? Error.NotFound(description: "Invoice not found.") : invoice;
    }
}

public sealed record DeleteInvoiceCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class DeleteInvoiceHandler(IAppDbContext db, ICurrentUser user, IClock clock, IRealtimeNotifier realtime) : ICommandHandler<DeleteInvoiceCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DeleteInvoiceCommand command, CancellationToken ct)
    {
        var invoice = await db.VendorInvoices.FirstOrDefaultAsync(i => i.Id == command.Id, ct);
        if (invoice is null)
            return Error.NotFound(description: "Invoice not found.");
        if (await db.VendorPayments.AnyAsync(p => p.VendorInvoiceId == invoice.Id, ct))
            return Error.Conflict(description: "Payments are recorded against this invoice — delete those first.");

        invoice.MarkDeleted(clock.UtcNow, user.UserId);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Finance, RealtimeAudience.Owner, null, ct);
        return Result.Success;
    }
}

// ---------- Payments ----------

public sealed record RecordVendorPaymentCommand(Guid SupplierId, decimal Amount, PaymentMethod Method, DateOnly? PaidOn, string? Note, Guid? InvoiceId) : ICommand<ErrorOr<Guid>>;

public sealed class RecordVendorPaymentValidator : AbstractValidator<RecordVendorPaymentCommand>
{
    public RecordVendorPaymentValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1_000_000_000);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class RecordVendorPaymentHandler(IAppDbContext db, IClock clock, IRealtimeNotifier realtime) : ICommandHandler<RecordVendorPaymentCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(RecordVendorPaymentCommand command, CancellationToken ct)
    {
        var supplier = await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == command.SupplierId, ct);
        if (supplier is null)
            return Error.NotFound(description: "Supplier not found.");
        if (command.InvoiceId is { } invoiceId && !await db.VendorInvoices.AnyAsync(i => i.Id == invoiceId && i.SupplierId == supplier.Id, ct))
            return Error.Validation(description: "That invoice doesn't belong to this supplier.");

        var payment = new VendorPayment(supplier.Id, supplier.Name, command.InvoiceId, command.Amount, command.Method, command.PaidOn ?? clock.Today, command.Note?.Trim());
        db.VendorPayments.Add(payment);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Finance, RealtimeAudience.Owner, null, ct);
        return payment.Id;
    }
}

public sealed record DeleteVendorPaymentCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class DeleteVendorPaymentHandler(IAppDbContext db, IRealtimeNotifier realtime) : ICommandHandler<DeleteVendorPaymentCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DeleteVendorPaymentCommand command, CancellationToken ct)
    {
        var payment = await db.VendorPayments.FirstOrDefaultAsync(p => p.Id == command.Id, ct);
        if (payment is null)
            return Error.NotFound(description: "Payment not found.");

        db.VendorPayments.Remove(payment);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Finance, RealtimeAudience.Owner, null, ct);
        return Result.Success;
    }
}

// ---------- Dues & valuation ----------

public sealed record ReceivableDto(Guid OrderId, string OrderName, string? ClientName, string Status, decimal Value, decimal Paid, decimal Outstanding);

public sealed record DuesDto(decimal YouOwe, decimal ClientsOwe, int SuppliersWithBalance, IReadOnlyList<HisabRowDto> Payables, IReadOnlyList<ReceivableDto> Receivables);

public sealed record GetDuesQuery : IQuery<DuesDto>;

public sealed class GetDuesHandler(IAppDbContext db) : IQueryHandler<GetDuesQuery, DuesDto>
{
    public async ValueTask<DuesDto> Handle(GetDuesQuery query, CancellationToken ct)
    {
        var hisab = await new GetHisabHandler(db).Handle(new GetHisabQuery(null), ct);
        var receivables = await db.ClientOrders.AsNoTracking()
            .Where(o => !o.IsCompleted)
            .Select(o => new { o.Id, o.Name, o.ClientName, o.Status, o.TotalValue, Paid = o.Payments.Where(p => p.IsPaid).Sum(p => p.Amount) })
            .OrderByDescending(o => o.TotalValue)
            .ToListAsync(ct);

        var rows = receivables.Select(o => new ReceivableDto(o.Id, o.Name, o.ClientName, o.Status.ToString(), o.TotalValue, o.Paid, Math.Max(0, o.TotalValue - o.Paid))).ToList();
        var payables = hisab.Rows.Where(r => r.Due > 0).ToList();
        return new DuesDto(hisab.TotalDue, rows.Sum(r => r.Outstanding), payables.Count, payables, rows);
    }
}

public sealed record ValuationRowDto(string ItemName, string? Category, Grade Grade, string LotSerial, string SupplierName, int Quantity, decimal? PricePerPiece, decimal Value);

public sealed record ValuationDto(int Pieces, decimal Value, int Lines, IReadOnlyList<ValuationRowDto> Rows);

/// <summary>What we hold and what it's worth at sell price — A at the sell price, C at the C price
/// (half of A unless set).</summary>
public sealed record GetValuationQuery(string? Search) : IQuery<ValuationDto>;

public sealed class GetValuationHandler(IAppDbContext db) : IQueryHandler<GetValuationQuery, ValuationDto>
{
    public async ValueTask<ValuationDto> Handle(GetValuationQuery query, CancellationToken ct)
    {
        var balances = from b in db.StockBalances.AsNoTracking()
                       join l in db.Lots.AsNoTracking() on b.LotId equals l.Id
                       where b.OnHand > 0
                       select b;
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            balances = balances.Where(b => b.NormalizedItemName.Contains(term) || b.SupplierName.ToLower().Contains(term) || b.LotSerial.ToLower().Contains(term));
        }

        var rows = await (from b in balances
                          join i in db.Items.AsNoTracking() on b.NormalizedItemName equals i.NormalizedName into items
                          from i in items.DefaultIfEmpty()
                          join p in db.ItemPrices.AsNoTracking() on i.Id equals p.ItemId into prices
                          from p in prices.DefaultIfEmpty()
                          orderby b.ItemName, b.Grade, b.ArrivalDate
                          select new { b.ItemName, Category = i != null ? i.Category : null, b.Grade, b.LotSerial, b.SupplierName, b.OnHand, Price = p })
            .Take(20_000)
            .ToListAsync(ct);

        var result = rows.Select(r =>
        {
            var price = r.Price?.PriceFor(r.Grade == Grade.C);
            return new ValuationRowDto(r.ItemName, r.Category, r.Grade, r.LotSerial, r.SupplierName, r.OnHand, price, (price ?? 0) * r.OnHand);
        }).ToList();

        return new ValuationDto(result.Sum(r => r.Quantity), result.Sum(r => r.Value), result.Count, result);
    }
}
