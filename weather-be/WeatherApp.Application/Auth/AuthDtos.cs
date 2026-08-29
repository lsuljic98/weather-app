using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Application.Auth;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(320)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record UserDto(Guid Id, string Email);

/// <summary>What a successful register / login / refresh hands back. The refresh token goes into a cookie, never the body.</summary>
public sealed record AuthResult(
    UserDto User,
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

/// <summary>The body of the token endpoints: everything in <see cref="AuthResult"/> except the refresh token.</summary>
public sealed record TokenResponse(UserDto User, string AccessToken, int ExpiresIn)
{
    public static TokenResponse From(AuthResult result) => new(result.User, result.AccessToken, result.ExpiresIn);
}
