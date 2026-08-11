using ErrorOr;
using Mediator;
using Thrivts.Domain.Enums;

namespace Thrivts.Application.Admin.Agencies;

/// <summary>
/// Mirrors SetBuyerApprovalStatusCommand/SetSellerApprovalStatusCommand — agencies share the same
/// profiles.approval_status lifecycle (THRIVTS_FLOW_MATRIX.md §1.4) but had no admin endpoint to
/// drive it, leaving an agency stuck at Pending forever with no way to approve/reject/block it.
/// </summary>
public sealed record SetAgencyApprovalStatusCommand(Guid AgencyId, ProfileApprovalAction Action, string? Reason = null)
    : ICommand<ErrorOr<Success>>;
