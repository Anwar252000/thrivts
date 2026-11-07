using System.ComponentModel.DataAnnotations;

namespace Thrivts.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string RoleClaim = "role";
    public const string AppClaim = "app";

    [Required]
    public string Issuer { get; init; } = "thrivts-api";

    [Required]
    public string Audience { get; init; } = "thrivts-apps";

    /// <summary>HMAC-SHA256 key, at least 32 bytes. Supplied via user-secrets / environment, never
    /// committed.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; init; } = default!;

    [Range(1, 120)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 14;
}
