using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Common;
using Thrivts.Domain.Inventory;

namespace Thrivts.Application.Features.Inventory;

public sealed record StockDraw(StockBalance Balance, int Quantity);

/// <summary>
/// The only code allowed to change stock counters. Each change is a single conditional UPDATE
/// ("... WHERE available >= n"), so two devices racing for the last pieces can never both win:
/// the loser's UPDATE matches zero rows and gets a clear "not enough stock" conflict. Callers run
/// inside <see cref="IAppDbContext.InTransactionAsync{T}"/> so a multi-line operation is all or nothing.
/// </summary>
public sealed class StockLedger(IAppDbContext db, IClock clock, ICurrentUser user)
{
    /// <summary>Removes pieces from stock for a container or small-order dispatch.</summary>
    public async Task TakeForDispatchAsync(StockBalance balance, int quantity, DispatchType type, Guid dispatchId, string number, DateOnly businessDate, CancellationToken ct)
    {
        var isContainer = type == DispatchType.Container;
        var updated = await db.StockBalances
            .Where(b => b.Id == balance.Id && b.OnHand - b.InSacks >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.OnHand, b => b.OnHand - quantity)
                .SetProperty(b => b.DispatchedContainer, b => b.DispatchedContainer + (isContainer ? quantity : 0))
                .SetProperty(b => b.DispatchedSmall, b => b.DispatchedSmall + (isContainer ? 0 : quantity)), ct);

        if (updated == 0)
            throw await InsufficientAsync(balance.Id, quantity, ct);

        Record(balance, -quantity, isContainer ? StockMovementType.ContainerDispatch : StockMovementType.SmallOrderDispatch, dispatchId, number, businessDate);
    }

    /// <summary>Puts dispatched pieces back (dispatch edited or deleted).</summary>
    public async Task RestoreDispatchAsync(StockBalance balance, int quantity, DispatchType type, Guid dispatchId, string number, CancellationToken ct)
    {
        var isContainer = type == DispatchType.Container;
        await db.StockBalances
            .Where(b => b.Id == balance.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.OnHand, b => b.OnHand + quantity)
                .SetProperty(b => b.DispatchedContainer, b => b.DispatchedContainer - (isContainer ? quantity : 0))
                .SetProperty(b => b.DispatchedSmall, b => b.DispatchedSmall - (isContainer ? 0 : quantity)), ct);

        Record(balance, quantity, StockMovementType.DispatchReversal, dispatchId, number, clock.Today);
    }

    /// <summary>Commits available pieces into a sack (they stay on hand).</summary>
    public async Task ReserveForSackAsync(StockBalance balance, int quantity, CancellationToken ct)
    {
        var updated = await db.StockBalances
            .Where(b => b.Id == balance.Id && b.OnHand - b.InSacks >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.InSacks, b => b.InSacks + quantity), ct);

        if (updated == 0)
            throw await InsufficientAsync(balance.Id, quantity, ct);
    }

    public async Task ReleaseFromSackAsync(Guid balanceId, int quantity, CancellationToken ct)
    {
        var updated = await db.StockBalances
            .Where(b => b.Id == balanceId && b.InSacks >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.InSacks, b => b.InSacks - quantity), ct);

        if (updated == 0)
            throw new ConflictException("The sack contents changed — reload and try again.");
    }

    /// <summary>A sack leaves the building: its pieces drop out of both InSacks and OnHand.</summary>
    public async Task ShipFromSackAsync(StockBalance balance, int quantity, Guid referenceId, string reference, CancellationToken ct)
    {
        var updated = await db.StockBalances
            .Where(b => b.Id == balance.Id && b.InSacks >= quantity && b.OnHand >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.InSacks, b => b.InSacks - quantity)
                .SetProperty(b => b.OnHand, b => b.OnHand - quantity)
                .SetProperty(b => b.DispatchedSku, b => b.DispatchedSku + quantity), ct);

        if (updated == 0)
            throw new ConflictException("The sack contents changed — reload and try again.");

        Record(balance, -quantity, StockMovementType.SkuShipment, referenceId, reference, clock.Today);
    }

    /// <summary>Loose pieces dispatched straight from the pool (not via a sack).</summary>
    public async Task TakeLooseAsync(StockBalance balance, int quantity, Guid referenceId, string? reference, CancellationToken ct)
    {
        var updated = await db.StockBalances
            .Where(b => b.Id == balance.Id && b.OnHand - b.InSacks >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.OnHand, b => b.OnHand - quantity)
                .SetProperty(b => b.DispatchedSku, b => b.DispatchedSku + quantity), ct);

        if (updated == 0)
            throw await InsufficientAsync(balance.Id, quantity, ct);

        Record(balance, -quantity, StockMovementType.LooseDispatch, referenceId, reference, clock.Today);
    }

    /// <summary>
    /// Owner correction of a finalized lot: grows or shrinks what came in. Shrinking only takes
    /// pieces that are still free (not packed, dispatched or at the washer).
    /// </summary>
    public async Task CorrectAsync(StockBalance balance, int delta, Guid lotId, string lotSerial, CancellationToken ct)
    {
        if (delta == 0)
            return;

        if (delta > 0)
        {
            await db.StockBalances.Where(b => b.Id == balance.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.QuantityIn, b => b.QuantityIn + delta).SetProperty(b => b.OnHand, b => b.OnHand + delta), ct);
        }
        else
        {
            var take = -delta;
            var updated = await db.StockBalances
                .Where(b => b.Id == balance.Id && b.OnHand - b.InSacks >= take && b.QuantityIn >= take)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.QuantityIn, b => b.QuantityIn - take).SetProperty(b => b.OnHand, b => b.OnHand - take), ct);
            if (updated == 0)
                throw new ConflictException(await NotFreeMessageAsync(balance.Id, take, ct));
        }

        Record(balance, delta, StockMovementType.Correction, lotId, lotSerial, clock.Today);
    }

    /// <summary>Pieces leave for the washer: out of OnHand (so they can't be packed or dispatched)
    /// into AtWash.</summary>
    public async Task SendToWashAsync(StockBalance balance, int quantity, Guid batchId, string number, CancellationToken ct)
    {
        var updated = await db.StockBalances
            .Where(b => b.Id == balance.Id && b.OnHand - b.InSacks >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.OnHand, b => b.OnHand - quantity).SetProperty(b => b.AtWash, b => b.AtWash + quantity), ct);
        if (updated == 0)
            throw await InsufficientAsync(balance.Id, quantity, ct);

        Record(balance, -quantity, StockMovementType.WashOut, batchId, number, clock.Today);
    }

    /// <summary>Cancels a wash that never came back: pieces return to OnHand untouched.</summary>
    public async Task CancelWashAsync(StockBalance balance, int quantity, Guid batchId, string number, CancellationToken ct)
    {
        var updated = await db.StockBalances
            .Where(b => b.Id == balance.Id && b.AtWash >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.AtWash, b => b.AtWash - quantity).SetProperty(b => b.OnHand, b => b.OnHand + quantity), ct);
        if (updated == 0)
            throw new ConflictException("The wash's stock changed — reload and try again.");

        Record(balance, quantity, StockMovementType.WashReturn, batchId, number, clock.Today);
    }

    /// <summary>
    /// Moves <paramref name="quantity"/> washed pieces back into stock in <paramref name="target"/>
    /// (the source balance itself, or the lot item's other grade). A negative quantity undoes an
    /// earlier receipt — only possible while those pieces are still free.
    /// </summary>
    public async Task WashBackAsync(StockBalance source, StockBalance target, int quantity, Guid batchId, string number, CancellationToken ct)
    {
        if (quantity == 0)
            return;

        var sameGrade = source.Id == target.Id;
        if (quantity > 0)
        {
            int updated;
            if (sameGrade)
            {
                updated = await db.StockBalances.Where(b => b.Id == source.Id && b.AtWash >= quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.AtWash, b => b.AtWash - quantity).SetProperty(b => b.OnHand, b => b.OnHand + quantity), ct);
            }
            else
            {
                updated = await db.StockBalances.Where(b => b.Id == source.Id && b.AtWash >= quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.AtWash, b => b.AtWash - quantity).SetProperty(b => b.WashLost, b => b.WashLost + quantity), ct);
                if (updated > 0)
                {
                    await db.StockBalances.Where(b => b.Id == target.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(b => b.WashIn, b => b.WashIn + quantity).SetProperty(b => b.OnHand, b => b.OnHand + quantity), ct);
                }
            }

            if (updated == 0)
                throw new ConflictException("More pieces were received than are at the washer — reload and try again.");

            Record(target, quantity, sameGrade ? StockMovementType.WashReturn : StockMovementType.WashRegrade, batchId, number, clock.Today);
            return;
        }

        var undo = -quantity;
        int reverted;
        if (sameGrade)
        {
            reverted = await db.StockBalances.Where(b => b.Id == source.Id && b.OnHand - b.InSacks >= undo)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.OnHand, b => b.OnHand - undo).SetProperty(b => b.AtWash, b => b.AtWash + undo), ct);
        }
        else
        {
            reverted = await db.StockBalances.Where(b => b.Id == target.Id && b.OnHand - b.InSacks >= undo && b.WashIn >= undo)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.OnHand, b => b.OnHand - undo).SetProperty(b => b.WashIn, b => b.WashIn - undo), ct);
            if (reverted > 0)
            {
                reverted = await db.StockBalances.Where(b => b.Id == source.Id && b.WashLost >= undo)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.WashLost, b => b.WashLost - undo).SetProperty(b => b.AtWash, b => b.AtWash + undo), ct);
            }
        }

        if (reverted == 0)
            throw new ConflictException(await NotFreeMessageAsync(target.Id, undo, ct));

        Record(target, -undo, sameGrade ? StockMovementType.WashReturn : StockMovementType.WashRegrade, batchId, number, clock.Today);
    }

    /// <summary>Pieces the washer ruined: they leave AtWash for good (or, negative, come back to it).</summary>
    public async Task WashRejectAsync(StockBalance source, int quantity, CancellationToken ct)
    {
        if (quantity == 0)
            return;

        var updated = quantity > 0
            ? await db.StockBalances.Where(b => b.Id == source.Id && b.AtWash >= quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.AtWash, b => b.AtWash - quantity).SetProperty(b => b.WashLost, b => b.WashLost + quantity), ct)
            : await db.StockBalances.Where(b => b.Id == source.Id && b.WashLost >= -quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.WashLost, b => b.WashLost + quantity).SetProperty(b => b.AtWash, b => b.AtWash - quantity), ct);

        if (updated == 0)
            throw new ConflictException("The wash's stock changed — reload and try again.");
    }

    /// <summary>
    /// Plans a first-in-first-out draw of <paramref name="quantity"/> pieces of one item and grade
    /// across lots (oldest arrival first). Only plans — callers then reserve or take each draw, which
    /// re-checks availability atomically.
    /// </summary>
    public async Task<IReadOnlyList<StockDraw>> PlanFifoAsync(string itemName, Grade grade, int quantity, CancellationToken ct)
    {
        if (quantity <= 0)
            return [];

        var key = Text.Normalize(itemName);
        var candidates = await db.StockBalances
            .Where(b => b.NormalizedItemName == key && b.Grade == grade && b.OnHand - b.InSacks > 0)
            .OrderBy(b => b.ArrivalDate).ThenBy(b => b.FinalizedAt).ThenBy(b => b.Id)
            .AsNoTracking()
            .ToListAsync(ct);

        var draws = new List<StockDraw>();
        var remaining = quantity;
        foreach (var balance in candidates)
        {
            var take = Math.Min(balance.Available, remaining);
            draws.Add(new StockDraw(balance, take));
            remaining -= take;
            if (remaining == 0)
                break;
        }

        if (remaining > 0)
        {
            var available = quantity - remaining;
            throw new ConflictException($"Not enough {itemName} grade {grade}: {available} available, {quantity} needed.");
        }

        return draws;
    }

    private void Record(StockBalance balance, int quantity, StockMovementType type, Guid? referenceId, string? reference, DateOnly businessDate) =>
        db.StockMovements.Add(new StockMovement(balance, quantity, type, referenceId, reference, businessDate, clock.UtcNow, user.UserId));

    private async Task<string> NotFreeMessageAsync(Guid balanceId, int wanted, CancellationToken ct)
    {
        var current = await db.StockBalances.AsNoTracking()
            .Where(b => b.Id == balanceId)
            .Select(b => new { b.LotSerial, b.ItemName, b.Grade, Free = b.OnHand - b.InSacks })
            .FirstOrDefaultAsync(ct);

        return current is null
            ? "That stock no longer exists — reload and try again."
            : $"Only {current.Free} {current.ItemName} ({current.Grade}) in {current.LotSerial} are still free to change; {wanted} needed. Pieces already packed, dispatched or at the washer can't be corrected away.";
    }

    private async Task<ConflictException> InsufficientAsync(Guid balanceId, int wanted, CancellationToken ct)
    {
        var current = await db.StockBalances.AsNoTracking()
            .Where(b => b.Id == balanceId)
            .Select(b => new { b.LotSerial, b.ItemName, b.Grade, Available = b.OnHand - b.InSacks })
            .FirstOrDefaultAsync(ct);

        return current is null
            ? new ConflictException("That stock no longer exists — reload and try again.")
            : new ConflictException($"Only {current.Available} {current.ItemName} ({current.Grade}) left in {current.LotSerial}; {wanted} requested.");
    }
}
