using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Auth;

/// <summary>Proxies to Supabase Auth's password grant — see ISupabaseAuthClient.</summary>
public sealed record LoginCommand(string Email, string Password) : ICommand<ErrorOr<SupabaseSession>>;
