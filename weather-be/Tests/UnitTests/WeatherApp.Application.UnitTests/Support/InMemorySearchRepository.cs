using WeatherApp.Application.Abstractions.Repositories;
using WeatherApp.Application.Statistics;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.UnitTests.Support;

public sealed class InMemorySearchRepository : ISearchRepository
{
    public List<Search> Rows { get; } = [];
    public List<(int Page, int PageSize)> PageRequests { get; } = [];

    public Task AddAsync(Search search, CancellationToken ct = default)
    {
        Rows.Add(search);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Search>> GetPageAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        PageRequests.Add((page, pageSize));
        IReadOnlyList<Search> result = Rows
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<int> CountAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult(Rows.Count(s => s.UserId == userId));

    public List<int> TopCitiesTakes { get; } = [];

    public Task<IReadOnlyList<TopCityDto>> GetTopCitiesAsync(Guid userId, int take, CancellationToken ct = default)
    {
        TopCitiesTakes.Add(take);
        IReadOnlyList<TopCityDto> result = Rows
            .Where(s => s.UserId == userId)
            .GroupBy(s => (s.CityName, s.CountryCode))
            .Select(g => new TopCityDto(g.Key.CityName, g.Key.CountryCode, g.Count()))
            .OrderByDescending(c => c.Count).ThenBy(c => c.City).ThenBy(c => c.Country)
            .Take(take)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<ConditionCountDto>> GetConditionCountsAsync(Guid userId, CancellationToken ct = default)
    {
        IReadOnlyList<ConditionCountDto> result = Rows
            .Where(s => s.UserId == userId)
            .GroupBy(s => s.ConditionMain)
            .Select(g => new ConditionCountDto(g.Key, g.Count()))
            .OrderByDescending(c => c.Count).ThenBy(c => c.Condition)
            .ToList();
        return Task.FromResult(result);
    }
}
