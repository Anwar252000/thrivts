using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Application.Common.Services;
using Thrivts.Application.Features.Inventory;
using Thrivts.Domain.Communication;
using Thrivts.Domain.Inventory;
using Thrivts.Domain.Lots;

namespace Thrivts.Application.Features.Lots;

/// <summary>
/// Owner-only correction of a finalized lot line — a miscount found after co-sign. Stock follows
/// the new numbers through the ledger in the same transaction: added pieces go into stock,
/// removed pieces must still be free (not packed, dispatched or at the washer). Blocked once the
/// lot is invoiced, because the supplier's hisab was computed from the old numbers. Every
/// correction needs a reason and is kept in the activity feed and the audit log.
/// </summary>
public sealed record CorrectFinalizedLotItemCommand(Guid LotId, Guid LineId, int QtyA, int QtyC, int Rejected, RejectionReason? RejectionReason, string Reason)
    : ICommand<ErrorOr<Success>>;

public sealed class CorrectFinalizedLotItemValidator : AbstractValidator<CorrectFinalizedLotItemCommand>
{
    public CorrectFinalizedLotItemValidator()
    {
        RuleFor(x => x.QtyA).InclusiveBetween(0, Lot.MaxPiecesPerLine);
        RuleFor(x => x.QtyC).InclusiveBetween(0, Lot.MaxPiecesPerLine);
        RuleFor(x => x.Rejected).InclusiveBetween(0, Lot.MaxPiecesPerLine);
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Say why the numbers are being corrected.").MaximumLength(300);
    }
}

public sealed class CorrectFinalizedLotItemHandler(IAppDbContext db, StockLedger ledger, IClock clock, ICurrentUser user, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<CorrectFinalizedLotItemCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(CorrectFinalizedLotItemCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<Success>>(async token =>
        {
            var lot = await db.Lots.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == command.LotId, token);
            if (lot is null)
                return Error.NotFound(description: "Lot not found.");
            if (await db.VendorInvoices.AnyAsync(i => i.LotId == lot.Id, token))
                return Error.Conflict(description: "This lot is already invoiced — delete its invoice first, correct the numbers, then invoice again.");

            var line = lot.Items.FirstOrDefault(i => i.Id == command.LineId);
            if (line is null)
                return Error.NotFound(description: "That item is not in this lot.");

            var before = (line.QtyA, line.QtyC, line.Rejected);
            var (deltaA, deltaC) = lot.CorrectFinalizedItem(command.LineId, command.QtyA, command.QtyC, command.Rejected, command.RejectionReason);

            var balances = await db.StockBalances.AsNoTracking().Where(b => b.LotItemId == line.Id).ToListAsync(token);
            foreach (var (grade, delta) in new[] { (Grade.A, deltaA), (Grade.C, deltaC) })
            {
                if (delta == 0)
                    continue;

                var balance = balances.FirstOrDefault(b => b.Grade == grade);
                if (balance is null)
                {
                    if (delta < 0)
                        return Error.Conflict(description: "The stock for this item is missing — reload and try again.");

                    // A grade the line didn't have at co-sign: open its balance.
                    var sibling = balances.FirstOrDefault();
                    balance = sibling is null
                        ? new StockBalance(lot.Id, line.Id, line.ItemId, lot.Serial, lot.SupplierId, lot.SupplierName, lot.ArrivalDate, line.ItemName, grade, delta, lot.CosignedAt ?? clock.UtcNow)
                        : StockBalance.OpenEmpty(sibling, grade);
                    db.StockBalances.Add(balance);
                    balances.Add(balance);
                    if (sibling is null)
                    {
                        // Created with the quantity already in it.
                        db.StockMovements.Add(new StockMovement(balance, delta, StockMovementType.Correction, lot.Id, lot.Serial, clock.Today, clock.UtcNow, user.UserId));
                        continue;
                    }

                    await db.SaveChangesAsync(token);
                }

                await ledger.CorrectAsync(balance, delta, lot.Id, lot.Serial, token);
            }

            activity.Record(ActivityArea.Lots, "corrected",
                $"{lot.Serial} {line.ItemName} corrected: A {before.QtyA}→{command.QtyA}, C {before.QtyC}→{command.QtyC}, rejected {before.Rejected}→{command.Rejected} — {command.Reason.Trim()}",
                lot.Id);
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

/// <summary>Removes an item that was booked into a finalized lot by mistake. Only possible while
/// none of its stock has moved, and before the lot is invoiced.</summary>
public sealed record RemoveFinalizedLotItemCommand(Guid LotId, Guid LineId, string Reason) : ICommand<ErrorOr<Success>>;

public sealed class RemoveFinalizedLotItemValidator : AbstractValidator<RemoveFinalizedLotItemCommand>
{
    public RemoveFinalizedLotItemValidator() =>
        RuleFor(x => x.Reason).NotEmpty().WithMessage("Say why the item is being removed.").MaximumLength(300);
}

public sealed class RemoveFinalizedLotItemHandler(IAppDbContext db, IClock clock, ICurrentUser user, ActivityRecorder activity, IRealtimeNotifier realtime)
    : ICommandHandler<RemoveFinalizedLotItemCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(RemoveFinalizedLotItemCommand command, CancellationToken ct)
    {
        var result = await db.InTransactionAsync<ErrorOr<Success>>(async token =>
        {
            var lot = await db.Lots.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == command.LotId, token);
            if (lot is null)
                return Error.NotFound(description: "Lot not found.");
            if (await db.VendorInvoices.AnyAsync(i => i.LotId == lot.Id, token))
                return Error.Conflict(description: "This lot is already invoiced — delete its invoice first.");

            var balances = await db.StockBalances.AsNoTracking().Where(b => b.LotItemId == command.LineId).ToListAsync(token);
            var balanceIds = balances.Select(b => b.Id).ToList();
            if (balances.Any(b => !b.IsUntouched)
                || await db.SackComponents.AnyAsync(c => balanceIds.Contains(c.StockBalanceId), token)
                || await db.DispatchLines.AnyAsync(l => balanceIds.Contains(l.StockBalanceId), token)
                || await db.WashLines.AnyAsync(l => l.LotItemId == command.LineId, token))
                return Error.Conflict(description: "Some of this item's stock has already been packed, washed or dispatched — correct its numbers instead.");

            var line = lot.RemoveFinalizedItem(command.LineId);
            foreach (var balance in balances)
                db.StockMovements.Add(new StockMovement(balance, -balance.OnHand, StockMovementType.Correction, lot.Id, lot.Serial, clock.Today, clock.UtcNow, user.UserId));

            await db.StockBalances.Where(b => b.LotItemId == command.LineId).ExecuteDeleteAsync(token);
            activity.Record(ActivityArea.Lots, "item-removed", $"{lot.Serial}: {line.ItemName} removed ({line.Received:N0} pcs) — {command.Reason.Trim()}", lot.Id);
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
