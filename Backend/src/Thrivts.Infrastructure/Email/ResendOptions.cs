namespace Thrivts.Infrastructure.Email;

public class ResendOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = default!;
    public string FromAddress { get; set; } = "noreply@thrivts.com";
}
