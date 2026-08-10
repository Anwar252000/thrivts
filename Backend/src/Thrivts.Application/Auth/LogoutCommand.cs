using ErrorOr;
using Mediator;

namespace Thrivts.Application.Auth;

/// <summary>Revokes the caller's current Supabase session. Best-effort — see LogoutCommandHandler.</summary>
public sealed record LogoutCommand(string AccessToken) : ICommand<ErrorOr<Success>>;
