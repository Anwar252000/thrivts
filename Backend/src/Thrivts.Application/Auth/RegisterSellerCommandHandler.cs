using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Auth;

public sealed class RegisterSellerCommandHandler : ICommandHandler<RegisterSellerCommand, ErrorOr<Success>>
{
    private readonly ISupabaseAuthClient _supabaseAuth;
    private readonly IAppUrlProvider _appUrlProvider;

    public RegisterSellerCommandHandler(ISupabaseAuthClient supabaseAuth, IAppUrlProvider appUrlProvider)
    {
        _supabaseAuth = supabaseAuth;
        _appUrlProvider = appUrlProvider;
    }

    public async ValueTask<ErrorOr<Success>> Handle(RegisterSellerCommand command, CancellationToken cancellationToken)
    {
        // Server-configured, never client-supplied — see ForgotPasswordCommand's identical reasoning.
        // The ?role=seller query param survives Supabase's redirect (it appends the token hash
        // after it) so VerifyEmail.tsx knows which complete-registration endpoint to call.
        var redirectTo = $"{_appUrlProvider.FrontendBaseUrl.TrimEnd('/')}/verify-email?role=seller";

        var metadata = new Dictionary<string, object?>
        {
            ["role"] = "seller",
            ["full_name"] = command.FullName,
            ["company_name"] = command.CompanyName,
            ["country"] = command.Country,
            ["city"] = command.City,
            ["phone"] = command.Phone,
            ["whatsapp"] = command.WhatsApp ?? command.Phone,
            ["years_in_business"] = command.YearsInBusiness,
            ["monthly_volume_capacity_pcs"] = command.MonthlyVolumeCapacityPcs,
            ["categories_supplied"] = command.CategoriesSupplied,
            ["language_pref"] = command.Language.ToString(),
        };

        try
        {
            await _supabaseAuth.SignUpAsync(command.Email, command.Password, metadata, redirectTo, cancellationToken);
            return Result.Success;
        }
        catch (SupabaseAuthException ex)
        {
            return Error.Validation(description: ex.Message);
        }
    }
}
