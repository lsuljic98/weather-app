using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using WeatherApp.Infrastructure.Weather;
using WeatherApp.Infrastructure.Weather.Services;

namespace WeatherApp.Infrastructure.UnitTests.Support;

public static class Make
{
    public const string ApiKey = "test-key";

    public static WeatherApiClient Client(StubHttpMessageHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://unit.test/") },
        Options.Create(new WeatherServiceConfiguration { BaseUrl = "https://unit.test", ApiKey = ApiKey }));

    public static WeatherService Service(FakeWeatherApiClient client, MemoryCacheOptions? cache = null) =>
        new(client, new MemoryCache(cache ?? new MemoryCacheOptions()));
}
