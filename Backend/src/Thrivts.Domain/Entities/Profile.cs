using Thrivts.Domain.Common;
using Thrivts.Domain.Enums;

namespace Thrivts.Domain.Entities;

/// <summary>
/// One row per Supabase auth user. Id == auth.uid() (the JWT `sub` claim) — never a separate
/// surrogate key, so a profile can always be looked up directly from the validated token.
/// </summary>
public class Profile : BaseEntity, IAggregateRoot
{
    public UserRole Role { get; private set; }
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public bool IsApproved { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Profile()
    {
        // EF Core
    }

    public Profile(Guid authUserId, UserRole role, string email, string fullName)
    {
        Id = authUserId;
        Role = role;
        Email = email;
        FullName = fullName;
    }
}
