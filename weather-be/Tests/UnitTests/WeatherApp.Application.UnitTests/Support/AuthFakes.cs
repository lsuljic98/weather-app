using Microsoft.Extensions.Options;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Abstractions.Repositories;
using WeatherApp.Application.Auth;
using WeatherApp.Application.Enums;
using WeatherApp.Application.Exceptions;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.UnitTests.Support;

public sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Rows { get; } = [];
    public int Saves { get; private set; }

    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Rows.SingleOrDefault(u => u.Id == id));

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(Rows.SingleOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(User user, CancellationToken ct = default)
    {
        if (Rows.Any(u => string.Equals(u.Email, user.Email, StringComparison.OrdinalIgnoreCase)))
            throw new EmailTakenException();
        Rows.Add(user);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        Saves++;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Rows { get; } = [];
    public int Saves { get; private set; }

    public Task<RefreshToken?> FindByHashAsync(byte[] tokenHash, CancellationToken ct = default) =>
        Task.FromResult(Rows.SingleOrDefault(t => t.TokenHash.AsSpan().SequenceEqual(tokenHash)));

    public Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        Rows.Add(token);
        return Task.CompletedTask;
    }

    public Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default)
    {
        foreach (var token in Rows.Where(t => t.UserId == userId && !t.IsRevoked))
            token.Revoke(now);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        Saves++;
        return Task.CompletedTask;
    }
}

/// <summary>Reversible "hash" so tests can see what was stored; counts verifications for the timing-safety test.</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public const string Prefix = "hashed:";
    public int Verifications { get; private set; }
    public bool ReportRehashNeeded { get; set; }

    public string Hash(string password) => Prefix + password;

    public PasswordVerification Verify(string hashedPassword, string password)
    {
        Verifications++;
        if (hashedPassword != Prefix + password)
            return PasswordVerification.Failed;
        return ReportRehashNeeded ? PasswordVerification.SuccessRehashNeeded : PasswordVerification.Success;
    }
}

public sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
{
    public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(15);
    public List<User> IssuedFor { get; } = [];

    public AccessToken Issue(User user, DateTimeOffset now)
    {
        IssuedFor.Add(user);
        return new AccessToken($"jwt-for-{user.Id}-{IssuedFor.Count}", now.Add(Lifetime));
    }
}

public sealed class FakeClock(DateTimeOffset start) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = start;

    public override DateTimeOffset GetUtcNow() => Now;

    public void Advance(TimeSpan by) => Now += by;
}

public static class AuthTestOptions
{
    public static IOptions<AuthOptions> Default(int refreshDays = 7, int accessMinutes = 15) =>
        Options.Create(new AuthOptions
        {
            Key = new string('k', 32),
            RefreshTokenDays = refreshDays,
            AccessTokenMinutes = accessMinutes,
        });
}
