using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using WeatherApp.Application.Dtos;
using WeatherApp.Infrastructure.UnitTests.Support;
using WeatherApp.Infrastructure.Weather.Responses;
using WeatherApp.IntegrationTests.Support;

namespace WeatherApp.IntegrationTests.Weather;

/// <summary>
/// GET /api/weather/current/coordinates through the real host: auth, model validation, the
/// provider-miss 404, and the promise that a "where am I" lookup is never written to history.
/// Coordinates are unique per test because the in-memory weather cache is shared across the collection.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CurrentWeatherByCoordinatesTests(ApiFactory factory)
{
    private static string Url(double lat, double lon) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/weather/current/coordinates?lat={lat}&lon={lon}");

    [Fact]
    public async Task Current_WhenAuthenticated_ShouldReturnProviderReadingWithoutGeocodingOrRecordingASearch()
    {
        factory.Provider.Cities = [];
        factory.Provider.Current = Fixture.Load<CurrentWeatherResponse>(Fixture.Current);
        var (client, user) = await factory.RegisterAsync();
        using var _ = client;
        var searchesBefore = factory.Provider.SearchCalls;

        var response = await client.GetAsync(Url(45.8100, 15.9800));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = (await response.Content.ReadFromJsonAsync<CurrentWeatherDto>()).ShouldNotBeNull();
        dto.City.ShouldBe("Britanski trg");
        dto.Country.ShouldBe("HR");
        (dto.Latitude, dto.Longitude).ShouldBe((45.8100, 15.9800));
        dto.TemperatureC.ShouldBe(35.72);
        factory.Provider.SearchCalls.ShouldBe(searchesBefore);

        await using var db = factory.NewDbContext();
        (await db.Searches.AnyAsync(s => s.UserId == user.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Current_WhenProviderHasNoReading_ShouldReturn404()
    {
        factory.Provider.Current = null;
        var (client, _) = await factory.RegisterAsync();
        using var __ = client;

        var response = await client.GetAsync(Url(-47.2500, -126.7100)); // open ocean, unused by other tests

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Current_WhenAnonymous_ShouldReturn401()
    {
        using var client = factory.CreateClient();

        (await client.GetAsync(Url(45.81, 15.98))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("lat=91&lon=0", "lat")]
    [InlineData("lat=-90.5&lon=0", "lat")] // fractional overshoot: an int Range would round it back in-bounds
    [InlineData("lat=90.4&lon=0", "lat")]
    [InlineData("lat=0&lon=180.1", "lon")]
    [InlineData("lat=0&lon=-181", "lon")]
    [InlineData("lat=abc&lon=0", "lat")]
    [InlineData("lon=0", "lat")]
    [InlineData("lat=0", "lon")]
    public async Task Current_WhenCoordinatesInvalid_ShouldReturn400NamingTheField(string query, string field)
    {
        var (client, _) = await factory.RegisterAsync();
        using var __ = client;

        var response = await client.GetAsync($"/api/weather/current/coordinates?{query}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").EnumerateObject().Select(e => e.Name).ShouldContain(field);
    }
}
