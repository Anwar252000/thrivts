using ErrorOr;
using Mediator;
using Thrivts.Application.Common.Exceptions;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Application.Auth;

public sealed class RegisterBuyerCommandHandler : ICommandHandler<RegisterBuyerCommand, ErrorOr<Success>>
{
    private readonly ISupabaseAuthClient _supabaseAuth;
    private readonly IAppUrlProvider _appUrlProvider;

    public RegisterBuyerCommandHandler(ISupabaseAuthClient supabaseAuth, IAppUrlProvider appUrlProvider)
    {
        _supabaseAuth = supabaseAuth;
        _appUrlProvider = appUrlProvider;
    }

    public async ValueTask<ErrorOr<Success>> Handle(RegisterBuyerCommand command, CancellationToken cancellationToken)
    {
        // Server-configured, never client-supplied — see ForgotPasswordCommand's identical reasoning.
        var redirectTo = $"{_appUrlProvider.FrontendBaseUrl.TrimEnd('/')}/verify-email";

        var metadata = new Dictionary<string, object?>
        {
            ["role"] = "buyer",
            ["full_name"] = command.FullName,
            ["phone"] = command.Phone,
            ["whatsapp"] = command.WhatsApp,
            // Lowercase: the live language_pref enum only has 'en'/'fr' — sending the C# enum's
            // PascalCase ToString() ("En"/"Fr") is the exact capitalization the handover docs blame
            // for silently breaking the legacy signup trigger.
            ["language_pref"] = command.Language.ToString().ToLowerInvariant(),
            ["company_name"] = command.CompanyName,
            ["website"] = command.Website,
            ["country"] = command.Country,
            ["city"] = command.City,
            ["instagram"] = command.Instagram,
            ["estimated_monthly_volume_pcs"] = command.EstimatedMonthlyVolumePcs,
            ["typical_requirement_type"] = command.TypicalRequirementType,
            ["category_ids"] = command.CategoryIds,
            ["agency_ref"] = command.AgencyRef,
            ["referral_code"] = command.ReferralCode,
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
