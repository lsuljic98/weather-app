using WeatherApp.Application.Weather;

namespace WeatherApp.Application.Abstractions;

public interface IWeatherService
{
    /// <summary>Current conditions for a city, or <c>null</c> if the city is unknown.</summary>
    Task<CurrentWeatherDto?> GetCurrentAsync(string city, CancellationToken ct = default);

    /// <summary>Multi-day forecast for a city, or <c>null</c> if the city is unknown.</summary>
    Task<ForecastDto?> GetForecastAsync(string city, int days, CancellationToken ct = default);
}
