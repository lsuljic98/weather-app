using WeatherApp.Application.Dtos;

namespace WeatherApp.Application.Abstractions.Services;

/// <summary>Weather lookups by city name and optional country code.</summary>
public interface IWeatherService
{
    /// <summary>Cities matching the query, closest match first. Empty if none.</summary>
    Task<IReadOnlyList<CityDto>> SearchCitiesAsync(string query, int limit = 5, CancellationToken ct = default);

    /// <summary>The conditions right now. Null if the city is unknown.</summary>
    Task<CurrentWeatherDto?> GetCurrentAsync(string city, string? countryCode = null, CancellationToken ct = default);

    /// <summary>The conditions right now at a coordinate. Null if the provider has no data for it.</summary>
    Task<CurrentWeatherDto?> GetCurrentAsync(double latitude, double longitude, CancellationToken ct = default);

    /// <summary>Five days as daily summaries plus the three-hour readings behind them. Null if the city is unknown.</summary>
    Task<ForecastDto?> GetForecastAsync(string city, string? countryCode = null, CancellationToken ct = default);
}
