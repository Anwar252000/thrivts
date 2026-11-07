using ErrorOr;
using FluentValidation;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Catalog;
using Thrivts.Domain.Common;

namespace Thrivts.Application.Features.Catalog;

public sealed record CatalogItemDto(Guid Id, string Name, string? Category, string? SkuCategory, string? GradeNote, decimal? WeightKg, ItemCriteria Criteria, string? RejectRule);

public sealed record SupplierDto(Guid Id, string Name);

public sealed record SkuCategoryDto(Guid Id, string Name, int SackSize);

public sealed record LabourerDto(Guid Id, string Name);

// ---------- Items (price-free) ----------

public sealed record GetCatalogItemsQuery : IQuery<IReadOnlyList<CatalogItemDto>>;

public sealed class GetCatalogItemsHandler(IAppDbContext db, ICacheService cache) : IQueryHandler<GetCatalogItemsQuery, IReadOnlyList<CatalogItemDto>>
{
    public ValueTask<IReadOnlyList<CatalogItemDto>> Handle(GetCatalogItemsQuery query, CancellationToken ct) =>
        cache.GetOrCreateAsync<IReadOnlyList<CatalogItemDto>>("catalog:items", async token =>
            await db.Items.AsNoTracking()
                .Where(i => !i.IsArchived)
                .OrderBy(i => i.Name)
                .Select(i => new CatalogItemDto(i.Id, i.Name, i.Category, i.SkuCategory, i.GradeNote, i.WeightKg, i.Criteria, i.RejectRule))
                .ToListAsync(token),
            [CacheTags.Catalog], ct);
}

// ---------- Suppliers ----------

public sealed record GetSuppliersQuery : IQuery<IReadOnlyList<SupplierDto>>;

public sealed class GetSuppliersHandler(IAppDbContext db, ICacheService cache) : IQueryHandler<GetSuppliersQuery, IReadOnlyList<SupplierDto>>
{
    public ValueTask<IReadOnlyList<SupplierDto>> Handle(GetSuppliersQuery query, CancellationToken ct) =>
        cache.GetOrCreateAsync<IReadOnlyList<SupplierDto>>("catalog:suppliers", async token =>
            await db.Suppliers.AsNoTracking()
                .Where(s => !s.IsArchived)
                .OrderBy(s => s.CreatedAt)
                .Select(s => new SupplierDto(s.Id, s.Name))
                .ToListAsync(token),
            [CacheTags.Suppliers], ct);
}

public sealed record CreateSupplierResult(SupplierDto Supplier, bool AlreadyExisted);

/// <summary>Adding a supplier that already exists (any casing/spacing) just returns it — the gate
/// guard taps "+ New supplier" in a hurry and should never create "khurram" next to "Khurram".</summary>
public sealed record CreateSupplierCommand(string Name) : ICommand<CreateSupplierResult>;

public sealed class CreateSupplierValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(Supplier.MaxNameLength)
        .Matches(@"^[^<>""'`\\]+$").WithMessage("Name contains characters that aren't allowed.");
}

public sealed class CreateSupplierHandler(IAppDbContext db, ICacheService cache, IRealtimeNotifier realtime) : ICommandHandler<CreateSupplierCommand, CreateSupplierResult>
{
    public async ValueTask<CreateSupplierResult> Handle(CreateSupplierCommand command, CancellationToken ct)
    {
        var key = Text.Normalize(command.Name);
        var existing = await db.Suppliers.FirstOrDefaultAsync(s => s.NormalizedName == key, ct);
        if (existing is not null)
        {
            if (existing.IsArchived)
            {
                existing.Restore();
                await db.SaveChangesAsync(ct);
                await cache.InvalidateAsync(CacheTags.Suppliers, ct);
            }

            return new CreateSupplierResult(new SupplierDto(existing.Id, existing.Name), true);
        }

        var supplier = new Supplier(command.Name);
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(ct);
        await cache.InvalidateAsync(CacheTags.Suppliers, ct);
        await realtime.PublishAsync(RealtimeTopics.Catalog, RealtimeAudience.All, null, ct);

        return new CreateSupplierResult(new SupplierDto(supplier.Id, supplier.Name), false);
    }
}

// ---------- Sack sizes ----------

public sealed record GetSkuCategoriesQuery : IQuery<IReadOnlyList<SkuCategoryDto>>;

public sealed class GetSkuCategoriesHandler(IAppDbContext db, ICacheService cache) : IQueryHandler<GetSkuCategoriesQuery, IReadOnlyList<SkuCategoryDto>>
{
    public ValueTask<IReadOnlyList<SkuCategoryDto>> Handle(GetSkuCategoriesQuery query, CancellationToken ct) =>
        cache.GetOrCreateAsync<IReadOnlyList<SkuCategoryDto>>("catalog:sku-categories", async token =>
            await db.SkuCategories.AsNoTracking().OrderBy(c => c.Name)
                .Select(c => new SkuCategoryDto(c.Id, c.Name, c.SackSize)).ToListAsync(token),
            [CacheTags.SkuCategories], ct);
}

public sealed record UpsertSkuCategoryCommand(string Name, int SackSize) : ICommand<SkuCategoryDto>;

public sealed class UpsertSkuCategoryValidator : AbstractValidator<UpsertSkuCategoryCommand>
{
    public UpsertSkuCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(40);
        RuleFor(x => x.SackSize).InclusiveBetween(1, 1000);
    }
}

/// <summary>Changes the size for future packs only; existing sacks keep theirs.</summary>
public sealed class UpsertSkuCategoryHandler(IAppDbContext db, ICacheService cache) : ICommandHandler<UpsertSkuCategoryCommand, SkuCategoryDto>
{
    public async ValueTask<SkuCategoryDto> Handle(UpsertSkuCategoryCommand command, CancellationToken ct)
    {
        var key = Text.Normalize(command.Name);
        var category = await db.SkuCategories.FirstOrDefaultAsync(c => c.NormalizedName == key, ct);
        if (category is null)
        {
            category = new SkuCategory(command.Name, command.SackSize);
            db.SkuCategories.Add(category);
        }
        else
        {
            category.SetSackSize(command.SackSize);
        }

        await db.SaveChangesAsync(ct);
        await cache.InvalidateAsync(CacheTags.SkuCategories, ct);
        return new SkuCategoryDto(category.Id, category.Name, category.SackSize);
    }
}

// ---------- Workers ----------

public sealed record GetLabourersQuery : IQuery<IReadOnlyList<LabourerDto>>;

public sealed class GetLabourersHandler(IAppDbContext db) : IQueryHandler<GetLabourersQuery, IReadOnlyList<LabourerDto>>
{
    public async ValueTask<IReadOnlyList<LabourerDto>> Handle(GetLabourersQuery query, CancellationToken ct) =>
        await db.Labourers.AsNoTracking().Where(l => l.IsActive).OrderBy(l => l.CreatedAt)
            .Select(l => new LabourerDto(l.Id, l.Name)).ToListAsync(ct);
}

public sealed record AddLabourerCommand(string Name) : ICommand<LabourerDto>;

public sealed class AddLabourerValidator : AbstractValidator<AddLabourerCommand>
{
    public AddLabourerValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(40);
}

public sealed class AddLabourerHandler(IAppDbContext db) : ICommandHandler<AddLabourerCommand, LabourerDto>
{
    public async ValueTask<LabourerDto> Handle(AddLabourerCommand command, CancellationToken ct)
    {
        var key = Text.Normalize(command.Name);
        var labourer = await db.Labourers.FirstOrDefaultAsync(l => l.NormalizedName == key, ct);
        if (labourer is null)
        {
            labourer = new Labourer(command.Name);
            db.Labourers.Add(labourer);
        }
        else
        {
            labourer.Activate();
        }

        await db.SaveChangesAsync(ct);
        return new LabourerDto(labourer.Id, labourer.Name);
    }
}

/// <summary>Removes a worker from the list; past attendance records stay.</summary>
public sealed record RemoveLabourerCommand(Guid Id) : ICommand<ErrorOr<Success>>;

public sealed class RemoveLabourerHandler(IAppDbContext db) : ICommandHandler<RemoveLabourerCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(RemoveLabourerCommand command, CancellationToken ct)
    {
        var labourer = await db.Labourers.FirstOrDefaultAsync(l => l.Id == command.Id, ct);
        if (labourer is null)
            return Error.NotFound(description: "Worker not found.");

        labourer.Deactivate();
        await db.SaveChangesAsync(ct);
        return Result.Success;
    }
}

// ---------- Item requests (staff side) ----------

public sealed record ItemRequestDto(Guid Id, string ProposedName, string? Note, string RequestedBy, ItemRequestStatus Status, string? DecisionNote, DateTimeOffset CreatedAt, DateTimeOffset? DecidedAt);

public sealed record RequestItemCommand(string Name, string? Note) : ICommand<ErrorOr<ItemRequestDto>>;

public sealed class RequestItemValidator : AbstractValidator<RequestItemCommand>
{
    public RequestItemValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Item.MaxNameLength);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}

public sealed class RequestItemHandler(IAppDbContext db, ICurrentUser user, IRealtimeNotifier realtime) : ICommandHandler<RequestItemCommand, ErrorOr<ItemRequestDto>>
{
    public async ValueTask<ErrorOr<ItemRequestDto>> Handle(RequestItemCommand command, CancellationToken ct)
    {
        var key = Text.Normalize(command.Name);
        if (await db.Items.AnyAsync(i => i.NormalizedName == key && !i.IsArchived, ct))
            return Error.Conflict(description: "That item is already in the catalogue.");
        if (await db.ItemRequests.AnyAsync(r => r.NormalizedName == key && r.Status == ItemRequestStatus.Pending, ct))
            return Error.Conflict(description: "That item is already waiting for approval.");

        var request = new ItemRequest(command.Name, command.Note, user.RequireUserId(), user.Name ?? "Staff");
        db.ItemRequests.Add(request);
        await db.SaveChangesAsync(ct);
        await realtime.PublishAsync(RealtimeTopics.Catalog, RealtimeAudience.Owner, null, ct);

        return new ItemRequestDto(request.Id, request.ProposedName, request.Note, request.RequestedByName, request.Status, null, request.CreatedAt, null);
    }
}

public sealed record GetMyItemRequestsQuery : IQuery<IReadOnlyList<ItemRequestDto>>;

public sealed class GetMyItemRequestsHandler(IAppDbContext db, ICurrentUser user) : IQueryHandler<GetMyItemRequestsQuery, IReadOnlyList<ItemRequestDto>>
{
    public async ValueTask<IReadOnlyList<ItemRequestDto>> Handle(GetMyItemRequestsQuery query, CancellationToken ct)
    {
        var userId = user.RequireUserId();
        return await db.ItemRequests.AsNoTracking()
            .Where(r => r.RequestedById == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .Select(r => new ItemRequestDto(r.Id, r.ProposedName, r.Note, r.RequestedByName, r.Status, r.DecisionNote, r.CreatedAt, r.DecidedAt))
            .ToListAsync(ct);
    }
}
