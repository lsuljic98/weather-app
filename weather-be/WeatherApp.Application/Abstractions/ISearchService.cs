using WeatherApp.Application.Searches;
using WeatherApp.Application.Weather;

namespace WeatherApp.Application.Abstractions;

/// <summary>
/// A forecast search: fetching the forecast and recording that the user searched for it are
/// one operation, so the history can never miss a search the client forgot to report.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// The forecast for the city, recorded against <paramref name="userId"/>. Null (and nothing
    /// recorded) if the city cannot be matched to a place.
    /// </summary>
    Task<ForecastDto?> SearchForecastAsync(
        Guid userId, string city, string? countryCode = null, CancellationToken ct = default);

    /// <summary>The user's past searches, newest first, read from the database rather than any cache.</summary>
    Task<PagedResult<SearchRecordDto>> GetHistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);
}
