using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Common;
using Thrivts.Domain.Finance;
using Thrivts.Domain.Lots;

namespace Thrivts.Application.Features.Owner;

public enum FloorStage
{
    Gate,
    Counting,
    Grading,
    Cosign,
    Pricing,
    Finalized,
}

public sealed record FloorLotDto(Guid Id, string Serial, string SupplierName, int Sacks, int Pieces, FloorStage Stage, DateTimeOffset GatedAt);

public sealed record FloorDto(int PiecesOnFloor, int Lots, int WaitingForPrice, int Finalized, int Suppliers, IReadOnlyList<FloorLotDto> Items);

/// <summary>
/// The owner's Live Floor: every lot by stage. "Pricing" means co-signed but not yet invoiced at the
/// owner's buy prices; "Finalized" means invoiced. Finalized lots are capped to the most recent ones
/// so the board stays fast as history grows.
/// </summary>
public sealed record GetFloorQuery(int FinalizedLimit = 60) : IQuery<FloorDto>;

public sealed class GetFloorHandler(IAppDbContext db) : IQueryHandler<GetFloorQuery, FloorDto>
{
    public async ValueTask<FloorDto> Handle(GetFloorQuery query, CancellationToken ct)
    {
        var invoiced = db.VendorInvoices.AsNoTracking().Where(i => i.LotId != null && i.Mode == InvoiceMode.Final).Select(i => i.LotId);

        var open = await db.Lots.AsNoTracking()
            .Where(l => l.Stage != LotStage.Finalized)
            .OrderByDescending(l => l.GatedAt)
            .Select(l => new { l.Id, l.Serial, l.SupplierName, l.Sacks, l.Stage, l.GatedAt, Items = l.Items.Count, Pieces = l.Items.Sum(i => i.Received), Invoiced = false })
            .ToListAsync(ct);

        var pricing = await db.Lots.AsNoTracking()
            .Where(l => l.Stage == LotStage.Finalized && !invoiced.Contains(l.Id))
            .OrderByDescending(l => l.CosignedAt)
            .Select(l => new { l.Id, l.Serial, l.SupplierName, l.Sacks, l.Stage, l.GatedAt, Items = l.Items.Count, Pieces = l.Items.Sum(i => i.QtyA + i.QtyC), Invoiced = false })
            .ToListAsync(ct);

        var finalized = await db.Lots.AsNoTracking()
            .Where(l => l.Stage == LotStage.Finalized && invoiced.Contains(l.Id))
            .OrderByDescending(l => l.CosignedAt)
            .Take(Math.Clamp(query.FinalizedLimit, 10, 500))
            .Select(l => new { l.Id, l.Serial, l.SupplierName, l.Sacks, l.Stage, l.GatedAt, Items = l.Items.Count, Pieces = l.Items.Sum(i => i.QtyA + i.QtyC), Invoiced = true })
            .ToListAsync(ct);

        var lots = open.Concat(pricing).Concat(finalized).Select(l => new FloorLotDto(l.Id, l.Serial, l.SupplierName, l.Sacks, l.Pieces, l.Stage switch
        {
            LotStage.Counting when l.Items == 0 => FloorStage.Gate,
            LotStage.Counting => FloorStage.Counting,
            LotStage.Grading => FloorStage.Grading,
            LotStage.AwaitingCosign => FloorStage.Cosign,
            _ => l.Invoiced ? FloorStage.Finalized : FloorStage.Pricing,
        }, l.GatedAt)).ToList();

        var finalizedCount = await db.Lots.AsNoTracking().CountAsync(l => l.Stage == LotStage.Finalized && invoiced.Contains(l.Id), ct);
        return new FloorDto(lots.Where(l => l.Stage != FloorStage.Finalized).Sum(l => l.Pieces), lots.Count, pricing.Count, finalizedCount,
            lots.Select(l => l.SupplierName).Distinct().Count(), lots);
    }
}

// ---------- Owner catalogue (with prices) ----------

public sealed record OwnerItemDto(
    Guid Id, string Name, string? Category, string? SkuCategory, string? GradeNote, decimal? WeightKg, ItemCriteria Criteria, string? RejectRule,
    bool IsArchived, decimal? BuyPrice, decimal? SellPrice, decimal? SellA, decimal? SellC, int? MarginPercent);

public sealed record GetOwnerItemsQuery(bool IncludeArchived) : IQuery<IReadOnlyList<OwnerItemDto>>;

public sealed class GetOwnerItemsHandler(IAppDbContext db) : IQueryHandler<GetOwnerItemsQuery, IReadOnlyList<OwnerItemDto>>
{
    public async ValueTask<IReadOnlyList<OwnerItemDto>> Handle(GetOwnerItemsQuery query, CancellationToken ct)
    {
        var rows = await (from i in db.Items.AsNoTracking()
                          join p in db.ItemPrices.AsNoTracking() on i.Id equals p.ItemId into prices
                          from p in prices.DefaultIfEmpty()
                          where query.IncludeArchived || !i.IsArchived
                          orderby i.Name
                          select new { i, p })
            .ToListAsync(ct);

        return rows.Select(r => new OwnerItemDto(r.i.Id, r.i.Name, r.i.Category, r.i.SkuCategory, r.i.GradeNote, r.i.WeightKg, r.i.Criteria, r.i.RejectRule, r.i.IsArchived,
            r.p?.BuyPrice, r.p?.SellPrice, r.p?.SellA, r.p?.SellC,
            r.p is { SellPrice: > 0, BuyPrice: > 0 } ? (int)Math.Round((r.p.SellPrice.Value - r.p.BuyPrice.Value) / r.p.SellPrice.Value * 100) : null)).ToList();
    }
}

public sealed record SaveOwnerItemCommand(
    Guid? Id, string Name, string? Category, string? SkuCategory, string? GradeNote, decimal? WeightKg, ItemCriteria? Criteria, string? RejectRule,
    decimal? BuyPrice, decimal? SellPrice, decimal? SellA, decimal? SellC) : ICommand<ErrorOr<Guid>>;

public sealed class SaveOwnerItemValidator : AbstractValidator<SaveOwnerItemCommand>
{
    public SaveOwnerItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Item.MaxNameLength);
        RuleFor(x => x.Category).MaximumLength(60);
        RuleFor(x => x.SkuCategory).MaximumLength(40);
        RuleFor(x => x.WeightKg).InclusiveBetween(0, 1000);
        RuleFor(x => x.RejectRule).MaximumLength(2000);
        RuleFor(x => x.BuyPrice).InclusiveBetween(0, 100_000_000);
        RuleFor(x => x.SellPrice).InclusiveBetween(0, 100_000_000);
        RuleFor(x => x.SellA).InclusiveBetween(0, 100_000_000);
        RuleFor(x => x.SellC).InclusiveBetween(0, 100_000_000);
        RuleFor(x => x.Criteria!.Brands).MaximumLength(1000).When(x => x.Criteria is not null);
        RuleFor(x => x.Criteria!.RejectIf).MaximumLength(1000).When(x => x.Criteria is not null);
        RuleFor(x => x.Criteria!.Notes).MaximumLength(1000).When(x => x.Criteria is not null);
    }
}

public sealed class SaveOwnerItemHandler(IAppDbContext db, ICacheService cache, IRealtimeNotifier realtime) : ICommandHandler<SaveOwnerItemCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(SaveOwnerItemCommand command, CancellationToken ct)
    {
        var key = Text.Normalize(command.Name);
        if (await db.Items.AnyAsync(i => i.NormalizedName == key && i.Id != command.Id, ct))
            return Error.Conflict(description: "Another item already has that name.");

        Item item;
        if (command.Id is { } id)
        {
            var existing = await db.Items.FirstOrDefaultAsync(i => i.Id == id, ct);
            if (existing is null)
                return Error.NotFound(description: "Item not found.");
            existing.Rename(command.Name);
            item = existing;
        }
        else
        {
            item = new Item(command.Name, command.Category);
            db.Items.Add(item);
        }

        item.UpdateDetails(command.Category?.Trim(), command.SkuCategory?.Trim(), command.GradeNote?.Trim(), command.WeightKg, command.RejectRule?.Trim());
        if (command.Criteria is not null)
            item.UpdateCriteria(command.Criteria);

        var price = await db.ItemPrices.FirstOrDefaultAsync(p => p.ItemId == item.Id, ct);
        if (price is null)
        {
            price = new ItemPrice(item.Id);
            db.ItemPrices.Add(price);
        }

        price.Set(command.BuyPrice, command.SellPrice, command.SellA, command.SellC);
        await db.SaveChangesAsync(ct);
        await cache.InvalidateAsync(CacheTags.Catalog, ct);
        await realtime.PublishAsync(RealtimeTopics.Catalog, RealtimeAudience.All, null, ct);
        return item.Id;
    }
}

public sealed record SetItemArchivedCommand(Guid Id, bool Archived) : ICommand<ErrorOr<Success>>;

public sealed class SetItemArchivedHandler(IAppDbContext db, ICacheService cache) : ICommandHandler<SetItemArchivedCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(SetItemArchivedCommand command, CancellationToken ct)
    {
        var item = await db.Items.FirstOrDefaultAsync(i => i.Id == command.Id, ct);
        if (item is null)
            return Error.NotFound(description: "Item not found.");

        if (command.Archived) item.Archive(); else item.Restore();
        await db.SaveChangesAsync(ct);
        await cache.InvalidateAsync(CacheTags.Catalog, ct);
        return Result.Success;
    }
}

// ---------- Item request decisions ----------

public sealed record OwnerItemRequestDto(Guid Id, string ProposedName, string? Note, string RequestedBy, ItemRequestStatus Status, DateTimeOffset CreatedAt, string? DecisionNote);

public sealed record GetItemRequestsQuery(ItemRequestStatus? Status) : IQuery<IReadOnlyList<OwnerItemRequestDto>>;

public sealed class GetItemRequestsHandler(IAppDbContext db) : IQueryHandler<GetItemRequestsQuery, IReadOnlyList<OwnerItemRequestDto>>
{
    public async ValueTask<IReadOnlyList<OwnerItemRequestDto>> Handle(GetItemRequestsQuery query, CancellationToken ct) =>
        await db.ItemRequests.AsNoTracking()
            .Where(r => query.Status == null || r.Status == query.Status)
            .OrderByDescending(r => r.CreatedAt)
            .Take(300)
            .Select(r => new OwnerItemRequestDto(r.Id, r.ProposedName, r.Note, r.RequestedByName, r.Status, r.CreatedAt, r.DecisionNote))
            .ToListAsync(ct);
}

/// <summary>Approving prices and weighs the item and creates it in the catalogue in one step.</summary>
public sealed record DecideItemRequestCommand(Guid Id, bool Approve, string? Category, decimal? WeightKg, decimal? BuyPrice, decimal? SellPrice, string? Note) : ICommand<ErrorOr<Success>>;

public sealed class DecideItemRequestHandler(IAppDbContext db, ICurrentUser user, IClock clock, ICacheService cache, IRealtimeNotifier realtime)
    : ICommandHandler<DecideItemRequestCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(DecideItemRequestCommand command, CancellationToken ct)
    {
        var request = await db.ItemRequests.FirstOrDefaultAsync(r => r.Id == command.Id, ct);
        if (request is null)
            return Error.NotFound(description: "Request not found.");

        if (!command.Approve)
        {
            request.Reject(user.RequireUserId(), clock.UtcNow, command.Note);
        }
        else
        {
            if (await db.Items.AnyAsync(i => i.NormalizedName == request.NormalizedName, ct))
                return Error.Conflict(description: "An item with this name already exists.");

            var item = new Item(request.ProposedName, command.Category);
            item.UpdateDetails(command.Category, Domain.Sku.SkuCategoryClassifier.Classify(request.ProposedName), null, command.WeightKg, null);
            db.Items.Add(item);
            var price = new ItemPrice(item.Id);
            price.Set(command.BuyPrice, command.SellPrice, null, null);
            db.ItemPrices.Add(price);
            request.Approve(item.Id, user.RequireUserId(), clock.UtcNow, command.Note);
        }

        await db.SaveChangesAsync(ct);
        await cache.InvalidateAsync(CacheTags.Catalog, ct);
        await realtime.PublishAsync(RealtimeTopics.Catalog, RealtimeAudience.All, null, ct);
        return Result.Success;
    }
}
