using System.Text.RegularExpressions;
using FluentValidation;

namespace Thrivts.Application.Auth;

public class RegisterSellerCommandValidator : AbstractValidator<RegisterSellerCommand>
{
    // Mirrors seller.html's isValidPhone(): ^\+?[0-9\s\-()]{7,20}$ plus a 7-15 digit count —
    // never enforced server-side before, so "abc" passed signup as a phone number.
    private static readonly Regex PhoneFormat = new(@"^\+?[0-9\s\-()]{7,20}$", RegexOptions.Compiled);

    public RegisterSellerCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FullName).NotEmpty();
        RuleFor(x => x.CompanyName).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
        // Both required on the live seller.html signup form (suCity/suPhone both carry `required`).
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Phone).NotEmpty().Must(BeAValidPhone).WithMessage("Please enter a valid phone number.");
        RuleFor(x => x.WhatsApp).Must(BeAValidPhone).WithMessage("Please enter a valid WhatsApp number.")
            .When(x => !string.IsNullOrWhiteSpace(x.WhatsApp));
    }

    private static bool BeAValidPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return true; // required-ness is a separate rule; this only judges format when present

        if (!PhoneFormat.IsMatch(phone))
            return false;

        var digitCount = phone.Count(char.IsDigit);
        return digitCount is >= 7 and <= 15;
    }
}
