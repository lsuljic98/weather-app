using WeatherApp.Application.Dtos;

namespace WeatherApp.Application.Abstractions.Services;

/// <summary>
/// A forecast search: fetching the forecast and recording that the user searched for it are
/// one operation, so the history can never miss a search the client forgot to report.
/// </summary>
public interface ISearchService
{
    /// <summary>The city's forecast, recorded as a search of the user. Null, and nothing recorded, if the city is unknown.</summary>
    Task<ForecastDto?> SearchForecastAsync(
        Guid userId, string city, string? countryCode = null, CancellationToken ct = default);

    /// <summary>One page of the user's past searches, newest first. Always read from the database, never a cache.</summary>
    Task<PagedResult<SearchRecordDto>> GetHistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default);
}
