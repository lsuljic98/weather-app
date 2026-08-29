using WeatherApp.Application.Dtos;
using WeatherApp.Application.Statistics;

namespace WeatherApp.Application.Abstractions.Services;

/// <summary>Per-user aggregates over the search history, computed in the database.</summary>
public interface IStatisticsService
{
    /// <summary>The user's most searched cities, most frequent first.</summary>
    Task<IReadOnlyList<TopCityDto>> GetTopCitiesAsync(Guid userId, int take, CancellationToken ct = default);

    /// <summary>The user's latest searches with their snapshotted conditions, newest first.</summary>
    Task<IReadOnlyList<SearchRecordDto>> GetRecentAsync(Guid userId, int take, CancellationToken ct = default);

    /// <summary>The user's search count per condition group, largest first.</summary>
    Task<IReadOnlyList<ConditionCountDto>> GetConditionDistributionAsync(Guid userId, CancellationToken ct = default);
}
