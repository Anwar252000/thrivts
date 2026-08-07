namespace Thrivts.Domain.Enums;

/// <summary>
/// Mirrors profiles.approval_status — shared by every role (buyer, seller, agency, influencer),
/// not per-role. Confirmed by THRIVTS_FLOW_MATRIX.md §1.4: approve_seller/reject_seller/
/// block_seller/unblock_seller AND admin_block_buyer/admin_unblock_buyer all read back through
/// `select approval_status, is_active from profiles`.
/// </summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Suspended
}
