using WeatherApp.Application.Statistics;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Abstractions.Repositories;

/// <summary>Stores a user's forecast searches; reads them back as history and as aggregates.</summary>
public interface ISearchRepository
{
    Task AddAsync(Search search, CancellationToken ct = default);

    /// <summary>One page of a user's searches, newest first. Page numbering starts at 1.</summary>
    Task<IReadOnlyList<Search>> GetPageAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);

    Task<int> CountAsync(Guid userId, CancellationToken ct = default);

    /// <summary>The user's most searched (city, country) pairs, most frequent first; ties break on city name.</summary>
    Task<IReadOnlyList<TopCityDto>> GetTopCitiesAsync(Guid userId, int take, CancellationToken ct = default);

    /// <summary>The user's search count per condition group, largest first.</summary>
    Task<IReadOnlyList<ConditionCountDto>> GetConditionCountsAsync(Guid userId, CancellationToken ct = default);
}
