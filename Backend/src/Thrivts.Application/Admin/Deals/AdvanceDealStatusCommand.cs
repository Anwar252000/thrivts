using ErrorOr;
using Mediator;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Deals;

/// <summary>
/// Replaces the old advance_deal_status RPC. [Authorize(Policy = "AdminOnly")] at the controller
/// is the first gate; the handler re-checks the role too (defense in depth).
/// </summary>
public sealed record AdvanceDealStatusCommand(Guid DealId, DealStatus NewStatus) : ICommand<ErrorOr<Success>>;
