namespace Thrivts.Domain.Enums;

/// <summary>
/// Mirrors notifications.audience as written by push_notification / notify_admins in the live schema.
/// </summary>
public enum NotificationAudience
{
    Buyer,
    Seller,
    Admin
}
