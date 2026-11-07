using Thrivts.Domain.Common;
using Thrivts.Domain.Exceptions;

namespace Thrivts.Domain.Identity;

public enum AppKind
{
    Staff,
    Owner,
}

public enum UserRole
{
    Owner,
    Finance,
    Sales,
    GeneralManager,
    Supervisor,
    InventoryManager,
    GateGuard,
}

public static class UserRoles
{
    public static AppKind AppOf(this UserRole role) => role switch
    {
        UserRole.Owner or UserRole.Finance or UserRole.Sales => AppKind.Owner,
        _ => AppKind.Staff,
    };

    /// <summary>Owner-app logins guard prices and money, so they need a longer PIN.</summary>
    public static int PinLengthFor(UserRole role) => role.AppOf() == AppKind.Owner ? 6 : 4;
}

public class AppUser : AuditableEntity, IVersioned
{
    public const int MaxDisplayNameLength = 60;

    public string DisplayName { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public AppKind App { get; private set; }
    public string Color { get; private set; } = default!;
    public string PinHash { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public int FailedPinAttempts { get; private set; }
    public DateTimeOffset? LockoutUntil { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public int SortOrder { get; private set; }
    public uint Version { get; private set; }

    private AppUser()
    {
    }

    public AppUser(string displayName, string title, UserRole role, string color, string pinHash, int sortOrder)
    {
        Rename(displayName, title);
        Role = role;
        App = role.AppOf();
        Color = color;
        PinHash = pinHash;
        SortOrder = sortOrder;
    }

    public bool IsLockedOut(DateTimeOffset now) => LockoutUntil is { } until && until > now;

    /// <summary>Progressive lockout: 5 misses → 30s, 10 → 5 min, 15+ → 30 min. A 4-digit PIN has
    /// only 10,000 combinations, so the server-side lockout is what actually protects it.</summary>
    public void RecordFailedPin(DateTimeOffset now)
    {
        FailedPinAttempts++;
        var lockFor = FailedPinAttempts switch
        {
            >= 15 => TimeSpan.FromMinutes(30),
            >= 10 when FailedPinAttempts % 5 == 0 => TimeSpan.FromMinutes(5),
            >= 5 when FailedPinAttempts % 5 == 0 => TimeSpan.FromSeconds(30),
            _ => TimeSpan.Zero,
        };

        if (lockFor > TimeSpan.Zero)
            LockoutUntil = now + lockFor;
    }

    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        FailedPinAttempts = 0;
        LockoutUntil = null;
        LastLoginAt = now;
    }

    public void ChangePin(string pinHash)
    {
        PinHash = pinHash;
        FailedPinAttempts = 0;
        LockoutUntil = null;
    }

    public void Rename(string displayName, string title)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("A user needs a name.");

        DisplayName = Text.Clean(displayName);
        Title = Text.Clean(title);
    }

    public void SetColor(string color) => Color = color;

    public void Deactivate() => IsActive = false;

    public void Activate()
    {
        IsActive = true;
        FailedPinAttempts = 0;
        LockoutUntil = null;
    }
}
