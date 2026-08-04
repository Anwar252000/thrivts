using Thrivts.Domain.Enums;

namespace Thrivts.Application.Common.Interfaces;

/// <summary>
/// Resolves the caller from the validated Supabase JWT (the `sub` claim = auth.uid()).
/// Every query/command handler must scope data through this — never trust a client-supplied id.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}
