using WeatherApp.Infrastructure.Weather.Responses;

namespace WeatherApp.Infrastructure.Abstractions;

/// <summary>
/// A service that communicates with the OpenWeather API.
/// </summary>
public interface IWeatherApiClient
{
    /// <summary>Places matching the query, closest match first. Empty if none.</summary>
    Task<IReadOnlyList<GeocodingResponse>> SearchCitiesAsync(
        string query, int limit = 5, CancellationToken ct = default);

    /// <summary>40 readings 3 hours apart covering the next 5 days. Null if the place is unknown.</summary>
    Task<ForecastResponse?> GetForecastAsync(
        double latitude, double longitude, CancellationToken ct = default);

    /// <summary>The conditions right now. Null if the place is unknown.</summary>
    Task<CurrentWeatherResponse?> GetCurrentAsync(
        double latitude, double longitude, CancellationToken ct = default);
}
