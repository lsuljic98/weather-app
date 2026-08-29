using WeatherApp.Application.Abstractions.Repositories;
using WeatherApp.Application.Abstractions.Services;
using WeatherApp.Application.Constants;
using WeatherApp.Application.Dtos;

namespace WeatherApp.Application.Statistics;

public sealed class StatisticsService(ISearchRepository searches) : IStatisticsService
{
    public Task<IReadOnlyList<TopCityDto>> GetTopCitiesAsync(Guid userId, int take, CancellationToken ct = default) =>
        searches.GetTopCitiesAsync(userId, Math.Clamp(take, 1, StatisticsLimits.MaxTake), ct);

    public async Task<IReadOnlyList<SearchRecordDto>> GetRecentAsync(Guid userId, int take, CancellationToken ct = default)
    {
        var rows = await searches.GetPageAsync(userId, page: 1, pageSize: Math.Clamp(take, 1, StatisticsLimits.MaxTake), ct);
        return [.. rows.Select(SearchRecordDto.From)];
    }

    public Task<IReadOnlyList<ConditionCountDto>> GetConditionDistributionAsync(Guid userId, CancellationToken ct = default) =>
        searches.GetConditionCountsAsync(userId, ct);
}
