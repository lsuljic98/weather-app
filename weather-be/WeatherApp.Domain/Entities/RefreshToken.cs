namespace WeatherApp.Domain.Entities;

/// <summary>
/// One link in a user's refresh-token rotation chain. Only the SHA-256 of the token is
/// held; the raw value exists solely in the response and the client's cookie.
/// </summary>
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

    public byte[] TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>The token issued in place of this one. A hit on an already-replaced token means reuse.</summary>
    public Guid? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    /// <summary>Revokes this token, optionally recording the token that superseded it.</summary>
    public void Revoke(DateTimeOffset revokedAt, Guid? replacedByTokenId = null)
    {
        if (IsRevoked)
            return;

        RevokedAt = revokedAt;
        ReplacedByTokenId = replacedByTokenId;
    }
}
