using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Abstractions;

/// <summary>Stores refresh tokens and tracks their revocation.</summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(byte[] tokenHash, CancellationToken ct = default);

    Task AddAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>Revokes every active token of the user; used when a replayed token is detected.</summary>
    Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
