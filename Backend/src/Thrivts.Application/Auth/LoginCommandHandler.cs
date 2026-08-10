using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Auth;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, ErrorOr<SupabaseSession>>
{
    private readonly ISupabaseAuthClient _authClient;

    public LoginCommandHandler(ISupabaseAuthClient authClient)
    {
        _authClient = authClient;
    }

    public async ValueTask<ErrorOr<SupabaseSession>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await _authClient.SignInWithPasswordAsync(command.Email, command.Password, cancellationToken);
        }
        catch (SupabaseAuthException ex)
        {
            return Error.Unauthorized(description: ex.Message);
        }
    }
}
