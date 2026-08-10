using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Auth;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand, ErrorOr<Success>>
{
    private readonly ISupabaseAuthClient _authClient;

    public LogoutCommandHandler(ISupabaseAuthClient authClient)
    {
        _authClient = authClient;
    }

    public async ValueTask<ErrorOr<Success>> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await _authClient.SignOutAsync(command.AccessToken, cancellationToken);
        }
        catch (SupabaseAuthException)
        {
            // Best-effort: the client discards its token regardless of whether Supabase's own
            // session record was already gone (e.g. it already expired). Never block a logout.
        }

        return Result.Success;
    }
}
