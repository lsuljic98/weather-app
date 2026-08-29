namespace WeatherApp.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public RefreshToken(Guid userId, byte[] tokenHash, DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    private RefreshToken() { } // EF

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public byte[] TokenHash { get; private set; } = null!; // SHA-256

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    public void Revoke(DateTimeOffset revokedAt, Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
            return;

        RevokedAt = revokedAt;
        ReplacedByTokenId = replacedByTokenId;
    }
}
