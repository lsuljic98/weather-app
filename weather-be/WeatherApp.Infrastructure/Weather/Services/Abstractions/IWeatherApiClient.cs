using WeatherApp.Infrastructure.Weather.Responses;

namespace WeatherApp.Infrastructure.Weather.Services.Abstractions;

/// <summary>
/// One method per call we make to the weather provider, returning its data as it comes.
/// Nothing here caches, combines or reshapes anything. A place the provider does not know
/// is not an error and comes back as null or an empty list; anything else that goes wrong
/// throws WeatherApiException.
/// </summary>
public interface IWeatherApiClient
{
    /// <summary>Looks up a place by name and returns the candidates, closest match first.</summary>
    Task<IReadOnlyList<GeocodingResponse>> SearchCitiesAsync(
        string query, int limit = 5, CancellationToken ct = default);

    /// <summary>Forty readings, three hours apart, covering the next five days.</summary>
    Task<ForecastResponse?> GetForecastAsync(
        double latitude, double longitude, CancellationToken ct = default);

    /// <summary>A single reading for how the weather is at this moment.</summary>
    Task<CurrentWeatherResponse?> GetCurrentAsync(
        double latitude, double longitude, CancellationToken ct = default);
}
