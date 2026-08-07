namespace Thrivts.Domain.Enums;

/// <summary>
/// The four admin actions on a Profile's lifecycle (THRIVTS_FLOW_MATRIX.md §1.3/§1.4) —
/// approve_seller / reject_seller / block_seller / unblock_seller and the equivalent
/// admin_block_buyer / admin_unblock_buyer. Not a 1:1 mirror of ApprovalStatus because
/// Block/Unblock also toggle IsActive together (see Profile.Block/Unblock).
/// </summary>
public enum ProfileApprovalAction
{
    Approve,
    Reject,
    Block,
    Unblock
}
