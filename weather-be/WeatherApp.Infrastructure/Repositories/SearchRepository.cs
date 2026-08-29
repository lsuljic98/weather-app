using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Abstractions;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Repositories;

public sealed class SearchRepository(WeatherDbContext context) : ISearchRepository
{
    public async Task AddAsync(Search search, CancellationToken ct = default)
    {
        context.Searches.Add(search);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Search>> GetPageAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        return await context.Searches
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            // Id is a UUIDv7 tiebreak for rows written in the same instant.
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountAsync(Guid userId, CancellationToken ct = default) =>
        context.Searches.CountAsync(s => s.UserId == userId, ct);
}
