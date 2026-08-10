using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Auth;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, ErrorOr<SupabaseSession>>
{
    private readonly ISupabaseAuthClient _authClient;

    public RefreshTokenCommandHandler(ISupabaseAuthClient authClient)
    {
        _authClient = authClient;
    }

    public async ValueTask<ErrorOr<SupabaseSession>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await _authClient.RefreshTokenAsync(command.RefreshToken, cancellationToken);
        }
        catch (SupabaseAuthException ex)
        {
            return Error.Unauthorized(description: ex.Message);
        }
    }
}
