namespace Thrivts.Domain.Enums;

/// <summary>Mirrors the live schema's notification_channel enum exactly.
/// "Whatsapp" (single capital) is deliberate — it snake_cases to "whatsapp", matching the DB
/// label; "WhatsApp" would snake_case to "whats_app" and fail to round-trip.</summary>
public enum NotificationChannel
{
    Email,
    Whatsapp,
    InApp
}
