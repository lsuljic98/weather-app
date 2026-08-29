using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Abstractions;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(WeatherDbContext context) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(byte[] tokenHash, CancellationToken ct = default) =>
        context.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        context.RefreshTokens.Add(token);
        return Task.CompletedTask;
    }

    public async Task RevokeAllAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default)
    {
        var active = await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in active)
            token.Revoke(now);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
