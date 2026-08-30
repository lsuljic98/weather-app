using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using WeatherApp.Infrastructure.UnitTests.Support;
using WeatherApp.Infrastructure.Weather.Responses;

namespace WeatherApp.Infrastructure.UnitTests.Weather.Services;

/// <summary>WeatherService caching and city resolution.</summary>
public class WeatherServiceCachingTests
{
    private static FakeWeatherApiClient ZagrebClient() => new()
    {
        Cities = [FakeWeatherApiClient.Zagreb],
        Forecast = Fixture.Load<ForecastResponse>(Fixture.Forecast),
        Current = Fixture.Load<CurrentWeatherResponse>(Fixture.Current),
    };

    [Fact]
    public async Task GetForecastAsync_WhenSameCityTwice_ShouldCallProviderOnce()
    {
        var client = ZagrebClient();
        var service = Make.Service(client);

        await service.GetForecastAsync("Zagreb");
        await service.GetForecastAsync("Zagreb");

        client.SearchCalls.ShouldBe(1);
        client.ForecastCalls.ShouldBe(1);
    }

    [Fact]
    public async Task GetForecastAsync_WhenCityDiffersOnlyByCaseOrWhitespace_ShouldGeocodeOnce()
    {
        var client = ZagrebClient();
        var service = Make.Service(client);

        await service.GetForecastAsync("Zagreb");
        await service.GetForecastAsync("ZAGREB");
        await service.GetForecastAsync("  Zagreb ");

        client.SearchCalls.ShouldBe(1);
    }

    [Fact]
    public async Task GetForecastAsync_WhenCountryCodeGiven_ShouldUseSeparateKeyAndCombinedQuery()
    {
        var client = ZagrebClient();
        var service = Make.Service(client);

        await service.GetForecastAsync("Zagreb");
        await service.GetForecastAsync("Zagreb", "HR");

        client.SearchCalls.ShouldBe(2);
        client.Queries[1].ShouldBe("Zagreb,HR");
    }

    [Fact]
    public async Task GetForecastAsync_WhenCoordinatesRoundToSameKey_ShouldShareWeatherCache()
    {
        var client = ZagrebClient();
        client.OnSearch = q => q == "A"
            ? [new GeocodingResponse("A", null, "HR", 45.8426, 15.9622)]
            : [new GeocodingResponse("B", null, "HR", 45.8449, 15.9601)];
        var service = Make.Service(client);

        var a = await service.GetForecastAsync("A");
        var b = await service.GetForecastAsync("B");

        client.ForecastCalls.ShouldBe(1);
        a.ShouldNotBeNull().City.ShouldBe("A");
        b.ShouldNotBeNull().City.ShouldBe("B");
    }

    [Fact]
    public async Task GetCurrentAsync_WhenProviderReturnsNull_ShouldNotCacheTheMiss()
    {
        var client = ZagrebClient();
        client.Current = null;
        var service = Make.Service(client);

        (await service.GetCurrentAsync("Zagreb")).ShouldBeNull();
        (await service.GetCurrentAsync("Zagreb")).ShouldBeNull();
        client.CurrentCalls.ShouldBe(2);
    }

    [Fact]
    public async Task GetForecastAsync_WhenCityIsUnknown_ShouldReturnNullWithoutWeatherCall()
    {
        var client = ZagrebClient();
        client.Cities = [];
        var service = Make.Service(client);

        (await service.GetForecastAsync("Nowhere")).ShouldBeNull();
        client.ForecastCalls.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetForecastAsync_WhenCityIsBlank_ShouldReturnNullWithoutAnyCall(string city)
    {
        var client = ZagrebClient();

        (await Make.Service(client).GetForecastAsync(city)).ShouldBeNull();
        client.SearchCalls.ShouldBe(0);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenCityResolved_ShouldUseGeocodedIdentityAndProviderReadings()
    {
        var dto = (await Make.Service(ZagrebClient()).GetCurrentAsync("Zagreb")).ShouldNotBeNull();

        dto.City.ShouldBe("Zagreb"); // not the fixture's station name "Britanski trg"
        dto.Country.ShouldBe("HR");
        (dto.Latitude, dto.Longitude).ShouldBe((45.8426, 15.9622));
        dto.ObservedAt.ShouldBe(new DateTimeOffset(2026, 8, 28, 13, 33, 17, TimeSpan.Zero));
        dto.TemperatureC.ShouldBe(35.72);
        dto.FeelsLikeC.ShouldBe(38.26);
        dto.Humidity.ShouldBe(39);
        dto.WindSpeed.ShouldBe(1.03);
        (dto.Condition, dto.Description, dto.Icon).ShouldBe(("Clouds", "few clouds", "02d"));
    }

    [Fact]
    public async Task GetCurrentAsync_WhenCoordinatesGiven_ShouldSkipGeocodingAndUseProviderPlaceName()
    {
        var client = ZagrebClient();

        var dto = (await Make.Service(client).GetCurrentAsync(45.8426, 15.9622)).ShouldNotBeNull();

        client.SearchCalls.ShouldBe(0);
        client.CurrentCalls.ShouldBe(1);
        dto.City.ShouldBe("Britanski trg"); // the fixture's station name, no geocoder to override it
        dto.Country.ShouldBe("HR");
        (dto.Latitude, dto.Longitude).ShouldBe((45.8426, 15.9622));
        dto.TemperatureC.ShouldBe(35.72);
        (dto.Condition, dto.Description, dto.Icon).ShouldBe(("Clouds", "few clouds", "02d"));
    }

    [Fact]
    public async Task GetCurrentAsync_WhenCoordinatesMatchAGeocodedCity_ShouldShareTheWeatherCache()
    {
        var client = ZagrebClient();
        var service = Make.Service(client);

        await service.GetCurrentAsync("Zagreb");
        await service.GetCurrentAsync(45.8449, 15.9601); // rounds to the same ~1km key

        client.CurrentCalls.ShouldBe(1);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenCoordinatesGivenAndProviderOmitsCountry_ShouldFallBackToEmptyString()
    {
        var client = ZagrebClient();
        var reading = Fixture.Load<CurrentWeatherResponse>(Fixture.Current);
        client.Current = reading with { Sys = new CurrentWeatherSys(Country: null) };

        var dto = (await Make.Service(client).GetCurrentAsync(45.8426, 15.9622)).ShouldNotBeNull();

        dto.Country.ShouldBe(string.Empty);
        dto.City.ShouldBe("Britanski trg");
    }

    [Fact]
    public async Task GetCurrentAsync_WhenCoordinatesGivenAndProviderReturnsNull_ShouldReturnNullAndNotCacheTheMiss()
    {
        var client = ZagrebClient();
        client.Current = null;
        var service = Make.Service(client);

        (await service.GetCurrentAsync(0.0, 0.0)).ShouldBeNull();
        (await service.GetCurrentAsync(0.0, 0.0)).ShouldBeNull();

        client.SearchCalls.ShouldBe(0);
        client.CurrentCalls.ShouldBe(2);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenCoordinatesRoundToDifferentKeys_ShouldCallProviderForEach()
    {
        var client = ZagrebClient();
        var service = Make.Service(client);

        await service.GetCurrentAsync(45.8426, 15.9622);
        await service.GetCurrentAsync(45.8526, 15.9622); // 0.01° ≈ 1km north, a different key

        client.CurrentCalls.ShouldBe(2);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenCoordinatesGivenWithToken_ShouldPassItToClient()
    {
        var client = ZagrebClient();
        using var cts = new CancellationTokenSource();

        await Make.Service(client).GetCurrentAsync(45.8426, 15.9622, cts.Token);

        client.LastToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task GetForecastAsync_WhenCacheHasSizeLimit_ShouldStillCacheEntries()
    {
        var service = Make.Service(ZagrebClient(), new MemoryCacheOptions { SizeLimit = 1000 });

        var dto = await service.GetForecastAsync("Zagreb");

        dto.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchCitiesAsync_WhenCalled_ShouldPassLimitThroughAndMapOptionalState()
    {
        var client = new FakeWeatherApiClient { Cities = Fixture.Load<List<GeocodingResponse>>(Fixture.Osijek) };

        var cities = await Make.Service(client).SearchCitiesAsync("Osijek", 3);

        client.LastLimit.ShouldBe(3);
        cities[0].State.ShouldBeNull();
        cities[2].Country.ShouldBe("BA");
    }

    [Fact]
    public async Task GetForecastAsync_WhenTokenProvided_ShouldPassItToClient()
    {
        var client = ZagrebClient();
        using var cts = new CancellationTokenSource();

        await Make.Service(client).GetForecastAsync("Zagreb", ct: cts.Token);

        client.LastToken.ShouldBe(cts.Token);
    }
}
