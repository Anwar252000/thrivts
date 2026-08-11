using FluentValidation;

namespace Thrivts.Application.Admin.Settings;

public class UpdatePlatformFeeConfigCommandValidator : AbstractValidator<UpdatePlatformFeeConfigCommand>
{
    public UpdatePlatformFeeConfigCommandValidator()
    {
        RuleFor(x => x.FeePerPcUsd).GreaterThan(0);
        RuleFor(x => x.PkrReference).NotEmpty();
    }
}
