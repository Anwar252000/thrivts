using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Auth;

/// <summary>
/// Proxies to Supabase Auth's /recover endpoint — see ISupabaseAuthClient. Always resolves to
/// Success even for an unregistered email (GoTrue itself never reveals account existence here);
/// only a genuine send failure (bad config, GoTrue down) surfaces as an error.
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : ICommand<ErrorOr<Success>>;

public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, ErrorOr<Success>>
{
    private readonly ISupabaseAuthClient _authClient;
    private readonly IAppUrlProvider _appUrlProvider;

    public ForgotPasswordCommandHandler(ISupabaseAuthClient authClient, IAppUrlProvider appUrlProvider)
    {
        _authClient = authClient;
        _appUrlProvider = appUrlProvider;
    }

    public async ValueTask<ErrorOr<Success>> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        // Server-configured, never client-supplied — a client-controlled redirect_to would be an
        // open-redirect hole in the recovery email link.
        var redirectTo = $"{_appUrlProvider.FrontendBaseUrl.TrimEnd('/')}/reset-password";

        try
        {
            await _authClient.RequestPasswordResetAsync(command.Email, redirectTo, cancellationToken);
            return Result.Success;
        }
        catch (SupabaseAuthException ex)
        {
            return Error.Failure(description: ex.Message);
        }
    }
}
