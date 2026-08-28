using WeatherApp.Infrastructure.Weather.Responses;
using WeatherApp.Infrastructure.Weather.Services.Abstractions;

namespace WeatherApp.Infrastructure.UnitTests.Support;

/// <summary>Scripted IWeatherApiClient with call counters and captured arguments.</summary>
public sealed class FakeWeatherApiClient : IWeatherApiClient
{
    public static readonly GeocodingResponse Zagreb = new("Zagreb", null, "HR", 45.8426, 15.9622);

    public Func<string, IReadOnlyList<GeocodingResponse>> OnSearch { get; set; } = _ => [];
    public Func<double, double, ForecastResponse?> OnForecast { get; set; } = (_, _) => null;
    public Func<double, double, CurrentWeatherResponse?> OnCurrent { get; set; } = (_, _) => null;

    public int SearchCalls { get; private set; }
    public int ForecastCalls { get; private set; }
    public int CurrentCalls { get; private set; }
    public List<string> Queries { get; } = [];
    public int? LastLimit { get; private set; }
    public CancellationToken LastToken { get; private set; }

    public IReadOnlyList<GeocodingResponse> Cities
    {
        set => OnSearch = _ => value;
    }

    public ForecastResponse? Forecast
    {
        set => OnForecast = (_, _) => value;
    }

    public CurrentWeatherResponse? Current
    {
        set => OnCurrent = (_, _) => value;
    }

    public Task<IReadOnlyList<GeocodingResponse>> SearchCitiesAsync(string query, int limit = 5, CancellationToken ct = default)
    {
        SearchCalls++;
        Queries.Add(query);
        LastLimit = limit;
        LastToken = ct;
        return Task.FromResult(OnSearch(query));
    }

    public Task<ForecastResponse?> GetForecastAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        ForecastCalls++;
        LastToken = ct;
        return Task.FromResult(OnForecast(latitude, longitude));
    }

    public Task<CurrentWeatherResponse?> GetCurrentAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        CurrentCalls++;
        LastToken = ct;
        return Task.FromResult(OnCurrent(latitude, longitude));
    }
}
