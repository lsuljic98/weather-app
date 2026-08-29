using WeatherApp.Application.Searches;
using WeatherApp.Application.Statistics;

namespace WeatherApp.Application.Abstractions;

/// <summary>Per-user aggregates over the search history, computed in the database.</summary>
public interface IStatisticsService
{
    /// <summary>The user's most searched cities, most frequent first.</summary>
    Task<IReadOnlyList<TopCityDto>> GetTopCitiesAsync(Guid userId, int take, CancellationToken ct = default);

    /// <summary>The user's latest searches with the conditions snapshotted at the time, newest first.</summary>
    Task<IReadOnlyList<SearchRecordDto>> GetRecentAsync(Guid userId, int take, CancellationToken ct = default);

    /// <summary>How the user's searches are distributed across condition groups, largest first.</summary>
    Task<IReadOnlyList<ConditionCountDto>> GetConditionDistributionAsync(Guid userId, CancellationToken ct = default);
}
