using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Auth;

/// <summary>Proxies to Supabase Auth's refresh_token grant — see ISupabaseAuthClient.</summary>
public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<ErrorOr<SupabaseSession>>;
