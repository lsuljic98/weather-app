using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Weather;

namespace WeatherApp.Infrastructure.Weather;

/// <summary>
/// Placeholder weather source: generates stable pseudo-random data so the API and the
/// frontend can be developed before the OpenWeather client is wired up. Replace with a
/// typed <c>HttpClient</c> implementation of <see cref="IWeatherService"/>.
/// </summary>
public sealed class InMemoryWeatherService : IWeatherService
{
    private static readonly string[] Summaries =
    [
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    ];

    private static readonly HashSet<string> KnownCities =
        new(StringComparer.OrdinalIgnoreCase) { "Zagreb", "Split", "Rijeka", "Osijek", "London", "Berlin" };

    public Task<CurrentWeatherDto?> GetCurrentAsync(string city, CancellationToken ct = default)
    {
        if (!KnownCities.TryGetValue(city, out var canonical))
            return Task.FromResult<CurrentWeatherDto?>(null);

        var forecast = Generate(canonical, DateOnly.FromDateTime(DateTime.UtcNow));
        return Task.FromResult<CurrentWeatherDto?>(
            new CurrentWeatherDto(canonical, forecast.TemperatureC, forecast.TemperatureF, forecast.Summary));
    }

    public Task<ForecastDto?> GetForecastAsync(string city, int days, CancellationToken ct = default)
    {
        if (!KnownCities.TryGetValue(city, out var canonical))
            return Task.FromResult<ForecastDto?>(null);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var forecasts = Enumerable.Range(1, days)
            .Select(offset => Generate(canonical, today.AddDays(offset)))
            .Select(f => new ForecastDayDto(f.Date, f.TemperatureC, f.TemperatureF, f.Summary))
            .ToArray();

        return Task.FromResult<ForecastDto?>(new ForecastDto(canonical, forecasts));
    }

    /// <summary>Deterministic per city and date, so repeated calls agree with each other.</summary>
    private static GeneratedDay Generate(string city, DateOnly date)
    {
        var seed = Hash($"{city.ToLowerInvariant()}|{date:O}");
        var temperatureC = (int)(seed % 55) - 20;
        // Summaries run cold to hot, so pick the band the temperature falls in.
        var summary = Summaries[(temperatureC + 20) * Summaries.Length / 55];
        return new GeneratedDay(date, temperatureC, summary);
    }

    // FNV-1a: unlike string.GetHashCode(), stable across processes.
    private static uint Hash(string value)
    {
        var hash = 2166136261u;
        foreach (var c in value)
        {
            hash ^= c;
            hash *= 16777619u;
        }

        return hash;
    }

    /// <summary>Internal shape of one generated day; forecasts are never persisted, so this is not a domain entity.</summary>
    private sealed record GeneratedDay(DateOnly Date, int TemperatureC, string Summary)
    {
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }
}
