using WeatherApp.Application.Auth;

namespace WeatherApp.Application.Abstractions;

public interface IAuthService
{
    /// <summary>Creates the user and signs them in. Throws <see cref="Exceptions.EmailTakenException"/> on a duplicate email.</summary>
    Task<AuthResult> RegisterAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Throws <see cref="Exceptions.InvalidCredentialsException"/> for a wrong email or password (same error for both).</summary>
    Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default);

    /// <summary>Rotates the refresh token. Throws <see cref="Exceptions.InvalidRefreshTokenException"/> if it is unknown, expired, or already used.</summary>
    Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Revokes the refresh token. Unknown tokens are ignored: logout must always succeed.</summary>
    Task LogoutAsync(string? refreshToken, CancellationToken ct = default);

    Task<UserDto?> GetUserAsync(Guid userId, CancellationToken ct = default);
}
