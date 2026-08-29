using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Exceptions;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher hasher,
    IAccessTokenIssuer tokens,
    IOptions<AuthOptions> options,
    TimeProvider clock) : IAuthService
{
    private readonly AuthOptions _options = options.Value;

    // Verified against when the email is unknown, so a miss costs the same time as a wrong password.
    private readonly Lazy<string> _dummyHash = new(() => hasher.Hash(Guid.NewGuid().ToString("N")));

    public async Task<AuthResult> RegisterAsync(string email, string password, CancellationToken ct = default)
    {
        email = Normalise(email);

        if (await users.FindByEmailAsync(email, ct) is not null)
            throw new EmailTakenException();

        var user = new User(email, hasher.Hash(password));
        await users.AddAsync(user, ct);

        return await IssueAsync(user, ct);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(Normalise(email), ct);

        var verification = hasher.Verify(user?.PasswordHash ?? _dummyHash.Value, password);
        if (user is null || verification is PasswordVerification.Failed)
            throw new InvalidCredentialsException();

        if (verification is PasswordVerification.SuccessRehashNeeded)
        {
            user.SetPasswordHash(hasher.Hash(password));
            await users.SaveChangesAsync(ct);
        }

        return await IssueAsync(user, ct);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var token = await refreshTokens.FindByHashAsync(HashToken(refreshToken), ct)
            ?? throw new InvalidRefreshTokenException();

        if (token.IsRevoked)
        {
            // A revoked token being presented again means it leaked: cut the whole chain.
            await refreshTokens.RevokeAllAsync(token.UserId, now, ct);
            await refreshTokens.SaveChangesAsync(ct);
            throw new InvalidRefreshTokenException();
        }

        if (token.IsExpired(now))
            throw new InvalidRefreshTokenException();

        var user = await users.FindByIdAsync(token.UserId, ct) ?? throw new InvalidRefreshTokenException();

        return await IssueAsync(user, ct, replacing: token);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var token = await refreshTokens.FindByHashAsync(HashToken(refreshToken), ct);
        if (token is null || token.IsRevoked)
            return;

        token.Revoke(clock.GetUtcNow());
        await refreshTokens.SaveChangesAsync(ct);
    }

    public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId, ct);
        return user is null ? null : ToDto(user);
    }

    /// <summary>Issues a token pair; when rotating, revokes the old refresh token in the same save.</summary>
    private async Task<AuthResult> IssueAsync(User user, CancellationToken ct, RefreshToken? replacing = null)
    {
        var now = clock.GetUtcNow();
        var access = tokens.Issue(user, now);

        var raw = GenerateToken();
        var refresh = new RefreshToken(user.Id, HashToken(raw), now.Add(_options.RefreshTokenLifetime));

        replacing?.Revoke(now, replacedByTokenId: refresh.Id);
        await refreshTokens.AddAsync(refresh, ct);
        await refreshTokens.SaveChangesAsync(ct);

        return new AuthResult(
            ToDto(user),
            access.Token,
            ExpiresIn: (int)(access.ExpiresAt - now).TotalSeconds,
            raw,
            refresh.ExpiresAt);
    }

    private static string GenerateToken() =>
        Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static byte[] HashToken(string raw) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(raw));

    private static string Normalise(string email) => email.Trim();

    private static UserDto ToDto(User user) => new(user.Id, user.Email);
}
