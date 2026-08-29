using WeatherApp.Application.Abstractions.Services;
using WeatherApp.Application.Dtos;

namespace WeatherApp.Application.UnitTests.Support;

/// <summary>Scripted IWeatherService; only the forecast path matters to the search service.</summary>
public sealed class FakeWeatherService : IWeatherService
{
    public ForecastDto? Forecast { get; set; }
    public int ForecastCalls { get; private set; }
    public (string City, string? CountryCode)? LastRequest { get; private set; }

    public Task<IReadOnlyList<CityDto>> SearchCitiesAsync(string query, int limit = 5, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CityDto>>([]);

    public Task<CurrentWeatherDto?> GetCurrentAsync(string city, string? countryCode = null, CancellationToken ct = default) =>
        Task.FromResult<CurrentWeatherDto?>(null);

    public Task<ForecastDto?> GetForecastAsync(string city, string? countryCode = null, CancellationToken ct = default)
    {
        ForecastCalls++;
        LastRequest = (city, countryCode);
        return Task.FromResult(Forecast);
    }
}
