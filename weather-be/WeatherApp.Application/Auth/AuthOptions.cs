using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Application.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>HS256 signing key. At least 32 bytes; comes from user-secrets or the environment, never appsettings.json.</summary>
    [Required, MinLength(32)]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = "weather-api";

    [Required]
    public string Audience { get; init; } = "weather-web";

    [Range(1, 24 * 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 365)]
    public int RefreshTokenDays { get; init; } = 7;

    /// <summary>Send the refresh cookie with the Secure flag. Off for plain-HTTP local Docker.</summary>
    public bool SecureCookie { get; init; } = true;

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenMinutes);

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenDays);
}
