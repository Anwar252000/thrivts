using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Auth;

/// <summary>
/// Proxies to Supabase Auth's PUT /user — see ISupabaseAuthClient. AccessToken here is the
/// short-lived recovery token GoTrue put in the emailed link's URL fragment, not a normal login
/// session token.
/// </summary>
public sealed record ResetPasswordCommand(string AccessToken, string NewPassword) : ICommand<ErrorOr<Success>>;

public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, ErrorOr<Success>>
{
    private readonly ISupabaseAuthClient _authClient;

    public ResetPasswordCommandHandler(ISupabaseAuthClient authClient)
    {
        _authClient = authClient;
    }

    public async ValueTask<ErrorOr<Success>> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await _authClient.ResetPasswordAsync(command.AccessToken, command.NewPassword, cancellationToken);
            return Result.Success;
        }
        catch (SupabaseAuthException ex)
        {
            return Error.Unauthorized(description: ex.Message);
        }
    }
}
