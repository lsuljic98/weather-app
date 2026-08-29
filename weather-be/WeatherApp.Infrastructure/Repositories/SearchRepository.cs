using Microsoft.EntityFrameworkCore;
using WeatherApp.Application.Abstractions.Repositories;
using WeatherApp.Application.Statistics;
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

        return await context.Searches.AsNoTracking()
            .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ThenByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public Task<int> CountAsync(Guid userId, CancellationToken ct = default) =>
        context.Searches.CountAsync(s => s.UserId == userId, ct);

    // Both aggregates below translate to a single GROUP BY served by the
    // ix_searches_user_id_* composite indexes; no rows are pulled into memory.

    public async Task<IReadOnlyList<TopCityDto>> GetTopCitiesAsync(
        Guid userId, int take, CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(take, 1);

        var rows = await context.Searches .AsNoTracking()
            .Where(s => s.UserId == userId)
                .GroupBy(s => new { s.CityName, s.CountryCode })
                .Select(g => new { g.Key.CityName, g.Key.CountryCode, Count = g.Count() })
                .OrderByDescending(c => c.Count)
                    .ThenBy(c => c.CityName)
                    .ThenBy(c => c.CountryCode)
            .Take(take)
            .ToListAsync(ct);

        return [.. rows.Select(r => new TopCityDto(r.CityName, r.CountryCode, r.Count))];
    }

    public async Task<IReadOnlyList<ConditionCountDto>> GetConditionCountsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await context.Searches.AsNoTracking()
            .Where(s => s.UserId == userId)
                .GroupBy(s => s.ConditionMain)
                .Select(g => new { Condition = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.Condition)
            .ToListAsync(ct);

        return [.. rows.Select(r => new ConditionCountDto(r.Condition, r.Count))];
    }
}
