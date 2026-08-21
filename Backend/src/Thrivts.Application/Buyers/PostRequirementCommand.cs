using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Thrivts.Application.Common.Interfaces;
using Thrivts.Domain.Entities;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Buyers;

/// <summary>Replaces buyer.html's submitRequirement() — the price the buyer submits is in their
/// own currency; BuyerTargetPriceUsd is computed server-side from the live ExchangeRates table so
/// the client never gets to assert its own USD conversion.</summary>
public sealed record PostRequirementCommand(
    string ItemName, int CategoryId, GradeType Grade, int QuantityPcs, string? ShippingMode,
    int? DeliveryTimelineDays, string DestinationCountry, CurrencyType Currency, decimal PricePerPc, string? Notes)
    : ICommand<ErrorOr<Guid>>;

public sealed class PostRequirementCommandHandler : ICommandHandler<PostRequirementCommand, ErrorOr<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PostRequirementCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async ValueTask<ErrorOr<Guid>> Handle(PostRequirementCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized();

        decimal exchangeRate = 1m;
        if (command.Currency != CurrencyType.USD)
        {
            var rate = await _db.ExchangeRates.AsNoTracking()
                .Where(r => r.Currency == command.Currency)
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync(cancellationToken);
            if (rate is null)
                return Error.Validation(description: $"No exchange rate is configured for {command.Currency} yet — please contact Thrivts.");
            exchangeRate = rate.RateToUsd;
        }

        var buyerTargetPriceUsd = command.Currency == CurrencyType.USD ? command.PricePerPc : command.PricePerPc / exchangeRate;
        var requirementNumber = await NextRequirementNumberAsync(cancellationToken);

        var requirement = new Requirement(
            requirementNumber: requirementNumber,
            buyerId: _currentUser.UserId.Value,
            itemName: command.ItemName,
            quantityPcs: command.QuantityPcs,
            grade: command.Grade,
            buyerTargetPriceUsd: buyerTargetPriceUsd,
            destinationCountry: command.DestinationCountry,
            categoryId: command.CategoryId,
            buyerCurrency: command.Currency,
            buyerTargetPriceOriginal: command.PricePerPc,
            buyerExchangeRate: exchangeRate,
            shippingMode: command.ShippingMode,
            deliveryTimelineDays: command.DeliveryTimelineDays,
            buyerNotes: command.Notes);

        _db.Requirements.Add(requirement);
        await _db.SaveChangesAsync(cancellationToken);

        return requirement.Id;
    }

    private async Task<string> NextRequirementNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"REQ-{year}-";
        var count = await _db.Requirements.CountAsync(r => r.RequirementNumber.StartsWith(prefix), cancellationToken);
        return $"{prefix}{(count + 1):D5}";
    }
}
