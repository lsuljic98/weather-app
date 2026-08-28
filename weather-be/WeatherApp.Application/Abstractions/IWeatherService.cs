using WeatherApp.Application.Weather;

namespace WeatherApp.Application.Abstractions;

/// <summary>
/// Weather service that works with city names and country codes.
/// </summary>
public interface IWeatherService
{
    /// <summary>Cities matching what the caller provided. Empty if nothing matches.</summary>
    Task<IReadOnlyList<CityDto>> SearchCitiesAsync(string query, int limit = 5, CancellationToken ct = default);

    /// <summary>Conditions right now. Null if the city name cannot be matched to a place.</summary>
    Task<CurrentWeatherDto?> GetCurrentAsync(string city, string? countryCode = null, CancellationToken ct = default);

    /// <summary>
    /// Today and the next four days, given both as daily summaries and as the raw three-hour
    /// readings behind them. Null if the city name cannot be matched to a place.
    /// </summary>
    Task<ForecastDto?> GetForecastAsync(string city, string? countryCode = null, CancellationToken ct = default);
}
