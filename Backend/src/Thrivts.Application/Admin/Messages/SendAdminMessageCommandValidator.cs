using FluentValidation;

namespace Thrivts.Application.Admin.Messages;

public class SendAdminMessageCommandValidator : AbstractValidator<SendAdminMessageCommand>
{
    public SendAdminMessageCommandValidator()
    {
        RuleFor(x => x.ThreadId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
    }
}
