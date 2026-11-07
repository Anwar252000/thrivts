using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Services;
using Thrivts.Domain.Common;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;

namespace Thrivts.Application.Features.Lots;

/// <summary>Loads a lot with its items for editing, applies the change and saves. Every mutation
/// on a lot goes through here so loading, not-found handling and the realtime push stay identical.</summary>
public sealed class LotEditor(IAppDbContext db, IRealtimeNotifier realtime)
{
    public async Task<ErrorOr<Success>> EditAsync(Guid lotId, Action<Lot> change, CancellationToken ct)
    {
        var lot = await db.Lots.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == lotId, ct);
        if (lot is null)
            return Error.NotFound(description: "Lot not found.");

        change(lot);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Lots, RealtimeAudience.All, new { lotId }, ct);
        return Result.Success;
    }
}

public sealed record GradeLine(Guid LineId, int QtyA, int QtyB, int QtyC, int Rejected, RejectionReason? RejectionReason);

public sealed class GradeLineValidator : AbstractValidator<GradeLine>
{
    public GradeLineValidator()
    {
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.QtyA).InclusiveBetween(0, Lot.MaxPiecesPerLine);
        RuleFor(x => x.QtyB).InclusiveBetween(0, Lot.MaxPiecesPerLine);
        RuleFor(x => x.QtyC).InclusiveBetween(0, Lot.MaxPiecesPerLine);
        RuleFor(x => x.Rejected).InclusiveBetween(0, Lot.MaxPiecesPerLine);
    }
}

// ---------- Counting ----------

public sealed record AddLotItemCommand(Guid LotId, string ItemName, int Received) : ICommand<ErrorOr<Guid>>;

public sealed class AddLotItemValidator : AbstractValidator<AddLotItemCommand>
{
    public AddLotItemValidator()
    {
        RuleFor(x => x.ItemName).NotEmpty().MaximumLength(Domain.Catalog.Item.MaxNameLength);
        RuleFor(x => x.Received).InclusiveBetween(0, Lot.MaxPiecesPerLine);
    }
}

public sealed class AddLotItemHandler(IAppDbContext db, IRealtimeNotifier realtime) : ICommandHandler<AddLotItemCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(AddLotItemCommand command, CancellationToken ct)
    {
        var lot = await db.Lots.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == command.LotId, ct);
        if (lot is null)
            return Error.NotFound(description: "Lot not found.");

        // Link to the catalogue item when the name matches; free-text items are still allowed.
        var key = Text.Normalize(command.ItemName);
        var catalogItem = await db.Items.AsNoTracking().Where(i => i.NormalizedName == key).Select(i => new { i.Id, i.Name }).FirstOrDefaultAsync(ct);

        var line = lot.AddItem(catalogItem?.Id, catalogItem?.Name ?? command.ItemName, command.Received);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Lots, RealtimeAudience.All, new { lotId = lot.Id }, ct);
        return line.Id;
    }
}

public sealed record SetLotItemReceivedCommand(Guid LotId, Guid LineId, int Received) : ICommand<ErrorOr<Success>>;

public sealed class SetLotItemReceivedHandler(LotEditor editor) : ICommandHandler<SetLotItemReceivedCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetLotItemReceivedCommand command, CancellationToken ct) =>
        await editor.EditAsync(command.LotId, lot => lot.SetReceived(command.LineId, command.Received), ct);
}

public sealed record RemoveLotItemCommand(Guid LotId, Guid LineId) : ICommand<ErrorOr<Success>>;

public sealed class RemoveLotItemHandler(LotEditor editor) : ICommandHandler<RemoveLotItemCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(RemoveLotItemCommand command, CancellationToken ct) =>
        await editor.EditAsync(command.LotId, lot => lot.RemoveItem(command.LineId), ct);
}

public sealed record FinalizeCountCommand(Guid LotId) : ICommand<ErrorOr<Success>>;

public sealed class FinalizeCountHandler(LotEditor editor, IClock clock, ActivityRecorder activity) : ICommandHandler<FinalizeCountCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(FinalizeCountCommand command, CancellationToken ct) =>
        await editor.EditAsync(command.LotId, lot =>
        {
            lot.FinalizeCount(clock.UtcNow);
            activity.Record(ActivityArea.Lots, "count-finalized", $"{lot.Serial} count finalized — {lot.TotalReceived:N0} pcs", lot.Id);
        }, ct);
}

public sealed record ReopenCountCommand(Guid LotId) : ICommand<ErrorOr<Success>>;

public sealed class ReopenCountHandler(LotEditor editor) : ICommandHandler<ReopenCountCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(ReopenCountCommand command, CancellationToken ct) =>
        await editor.EditAsync(command.LotId, lot => lot.ReopenCount(), ct);
}

// ---------- Grading ----------

/// <summary>Saves every line's grades in one request. With IsDraft the lot stays open ("save
/// grading — finish later"); grades are validated per line but the lot need not balance yet.</summary>
public sealed record SaveGradesCommand(Guid LotId, IReadOnlyList<GradeLine> Lines, bool IsDraft) : ICommand<ErrorOr<Success>>;

public sealed class SaveGradesValidator : AbstractValidator<SaveGradesCommand>
{
    public SaveGradesValidator()
    {
        RuleFor(x => x.Lines).NotNull().Must(l => l.Count <= Lot.MaxItems);
        RuleForEach(x => x.Lines).SetValidator(new GradeLineValidator());
    }
}

public sealed class SaveGradesHandler(LotEditor editor, IClock clock) : ICommandHandler<SaveGradesCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SaveGradesCommand command, CancellationToken ct) =>
        await editor.EditAsync(command.LotId, lot =>
        {
            foreach (var g in command.Lines)
                lot.SetGrades(g.LineId, g.QtyA, g.QtyB, g.QtyC, g.Rejected, g.RejectionReason);

            if (command.IsDraft)
                lot.SaveGradingDraft(clock.UtcNow);
        }, ct);
}

/// <summary>Applies the final grades (if sent) and signs in one step, so what was signed is
/// exactly what was on screen.</summary>
public sealed record SignLotCommand(Guid LotId, string Signature, IReadOnlyList<GradeLine>? Lines) : ICommand<ErrorOr<Success>>;

public sealed class SignLotValidator : AbstractValidator<SignLotCommand>
{
    public SignLotValidator()
    {
        RuleFor(x => x.Signature).NotEmpty().MaximumLength(SignatureStore.MaxBytes * 2);
        RuleForEach(x => x.Lines).SetValidator(new GradeLineValidator());
    }
}

public sealed class SignLotHandler(LotEditor editor, SignatureStore signatures, ICurrentUser user, IClock clock, ActivityRecorder activity)
    : ICommandHandler<SignLotCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SignLotCommand command, CancellationToken ct) =>
        await editor.EditAsync(command.LotId, lot =>
        {
            foreach (var g in command.Lines ?? [])
                lot.SetGrades(g.LineId, g.QtyA, g.QtyB, g.QtyC, g.Rejected, g.RejectionReason);

            lot.SignBySupervisor(user.RequireUserId(), signatures.Store(command.Signature), clock.UtcNow);
            var net = lot.Items.Sum(i => i.Net);
            activity.Record(ActivityArea.Lots, "signed", $"{lot.Serial} signed by {user.Name} — {net:N0} net pcs, sent for co-sign", lot.Id);
        }, ct);
}

public sealed record UnlockLotCommand(Guid LotId) : ICommand<ErrorOr<Success>>;

public sealed class UnlockLotHandler(LotEditor editor) : ICommandHandler<UnlockLotCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(UnlockLotCommand command, CancellationToken ct) =>
        await editor.EditAsync(command.LotId, lot => lot.UnlockSignature(), ct);
}

// ---------- Co-sign ----------

public sealed record SendBackLotCommand(Guid LotId, string? Reason) : ICommand<ErrorOr<Success>>;

public sealed class SendBackLotValidator : AbstractValidator<SendBackLotCommand>
{
    public SendBackLotValidator() => RuleFor(x => x.Reason).MaximumLength(500);
}

public sealed class SendBackLotHandler(LotEditor editor, ICurrentUser user, IClock clock, ActivityRecorder activity) : ICommandHandler<SendBackLotCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SendBackLotCommand command, CancellationToken ct) =>
        await editor.EditAsync(command.LotId, lot =>
        {
            lot.SendBack(user.RequireUserId(), command.Reason, clock.UtcNow);
            activity.Record(ActivityArea.Lots, "sent-back", $"{lot.Serial} sent back to the supervisor{(command.Reason is null ? "" : $": {command.Reason}")}", lot.Id);
        }, ct);
}

public sealed record CosignLotCommand(Guid LotId, string Signature) : ICommand<ErrorOr<Success>>;

public sealed class CosignLotValidator : AbstractValidator<CosignLotCommand>
{
    public CosignLotValidator() => RuleFor(x => x.Signature).NotEmpty().MaximumLength(SignatureStore.MaxBytes * 2);
}

/// <summary>
/// Co-signing turns a graded lot into stock: one balance per item and grade (A and C) plus a
/// CosignIn movement each, all in the same transaction as the stage change — stock can never exist
/// for a lot that isn't finalized, or vice versa. Re-running it is impossible: the stage check
/// rejects a second co-sign, and the unique (lot item, grade) index backs that up.
/// </summary>
public sealed class CosignLotHandler(IAppDbContext db, SignatureStore signatures, ICurrentUser user, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<CosignLotCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(CosignLotCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<Success>>(async token =>
        {
            var lot = await db.Lots.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == command.LotId, token);
            if (lot is null)
                return Error.NotFound(description: "Lot not found.");

            var now = clock.UtcNow;
            lot.Cosign(user.RequireUserId(), signatures.Store(command.Signature), now);

            foreach (var line in lot.Items)
            {
                foreach (var (grade, quantity) in new[] { (Grade.A, line.QtyA), (Grade.C, line.QtyC) })
                {
                    if (quantity <= 0)
                        continue;

                    var balance = new StockBalance(lot.Id, line.Id, line.ItemId, lot.Serial, lot.SupplierId, lot.SupplierName,
                        lot.ArrivalDate, line.ItemName, grade, quantity, now);
                    db.StockBalances.Add(balance);
                    db.StockMovements.Add(new StockMovement(balance, quantity, StockMovementType.CosignIn, lot.Id, lot.Serial, clock.Today, now, user.UserId));
                }
            }

            var net = lot.Items.Sum(i => i.Net);
            activity.Record(ActivityArea.Lots, "finalized", $"{lot.Serial} co-signed by {user.Name} — {net:N0} pcs into stock", lot.Id);
            await db.SaveChangesAsync(token);
            return Result.Success;
        }, ct);

        if (!result.IsError)
        {
            await realtime.PublishAsync(RealtimeTopics.Lots, RealtimeAudience.All, new { lotId = command.LotId }, ct);
            await realtime.PublishAsync(RealtimeTopics.Stock, RealtimeAudience.All, null, ct);
        }

        return result;
    }
}

// ---------- Delete ----------

/// <summary>
/// Soft-deletes a lot. Before co-sign anyone with counting rights may do it. A finalized lot may only
/// be removed (owner app) while none of its stock has been used — nothing packed, dispatched,
/// returned or invoiced — and its balances are zeroed with a LotRemoval movement so history stays whole.
/// </summary>
public sealed record DeleteLotCommand(Guid LotId, bool AllowFinalized) : ICommand<ErrorOr<Success>>;

public sealed class DeleteLotHandler(IAppDbContext db, ICurrentUser user, IClock clock, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<DeleteLotCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DeleteLotCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<Success>>(async token =>
        {
            var lot = await db.Lots.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == command.LotId, token);
            if (lot is null)
                return Error.NotFound(description: "Lot not found.");

            if (lot.Stage == LotStage.Finalized)
            {
                if (!command.AllowFinalized)
                    return Error.Forbidden(description: "This lot is already in stock — only the owner can remove it.");

                var balances = await db.StockBalances.AsNoTracking().Where(b => b.LotId == lot.Id).ToListAsync(token);
                if (balances.Any(b => !b.IsUntouched))
                    return Error.Conflict(description: "Some of this lot's stock has already been packed, washed or dispatched — it can't be deleted.");
                if (lot.Items.Any(i => i.ReturnedRejected > 0))
                    return Error.Conflict(description: "Rejections from this lot were already returned — it can't be deleted.");
                if (await db.VendorInvoices.AnyAsync(i => i.LotId == lot.Id, token))
                    return Error.Conflict(description: "This lot has a vendor invoice — delete the invoice first.");

                foreach (var balance in balances)
                {
                    // The pieces never really entered stock: zero both sides so the balance still conserves.
                    var removed = await db.StockBalances
                        .Where(b => b.Id == balance.Id && b.OnHand == b.QuantityIn && b.InSacks == 0 && b.AtWash == 0)
                        .ExecuteUpdateAsync(s => s.SetProperty(b => b.OnHand, 0).SetProperty(b => b.QuantityIn, 0), token);
                    if (removed == 0)
                        return Error.Conflict(description: "This lot's stock just changed — reload and try again.");

                    db.StockMovements.Add(new StockMovement(balance, -balance.OnHand, StockMovementType.LotRemoval, lot.Id, lot.Serial, clock.Today, clock.UtcNow, user.UserId));
                }
            }

            // Explicit soft delete: Remove() would cascade-mark the tracked items as deleted rows.
            lot.MarkDeleted(clock.UtcNow, user.UserId);
            activity.Record(ActivityArea.Lots, "deleted", $"{lot.Serial} ({lot.SupplierName}) deleted by {user.Name}", lot.Id);
            await db.SaveChangesAsync(token);
            return Result.Success;
        }, ct);

        if (!result.IsError)
        {
            await realtime.PublishAsync(RealtimeTopics.Lots, RealtimeAudience.All, null, ct);
            await realtime.PublishAsync(RealtimeTopics.Stock, RealtimeAudience.All, null, ct);
        }

        return result;
    }
}
