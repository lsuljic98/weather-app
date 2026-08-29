using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
using WeatherApp.Application.Searches;
using WeatherApp.Application.Statistics;
using WeatherApp.Infrastructure.UnitTests.Support;
using WeatherApp.Infrastructure.Weather.Responses;
using WeatherApp.IntegrationTests.Support;

namespace WeatherApp.IntegrationTests.Statistics;

/// <summary>
/// The three statistics endpoints aggregate the caller's search rows in Postgres through the
/// real controllers, EF GROUP BY translation and indexes.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class StatisticsEndpointTests(ApiFactory factory)
{
    private const string TopCities = "/api/statistics/top-cities";
    private const string Recent = "/api/statistics/recent";
    private const string Conditions = "/api/statistics/conditions";

    private static readonly GeocodingResponse Split = new("Split", null, "HR", 43.5081, 16.4402);
    private static readonly GeocodingResponse Rijeka = new("Rijeka", null, "HR", 45.3271, 14.4422);

    /// <summary>Routes geocoding by the query so one client can search several cities.</summary>
    private void ScriptCities()
    {
        factory.Provider.OnSearch = q => q switch
        {
            "Zagreb" => [FakeWeatherApiClient.Zagreb],
            "Split" => [Split],
            "Rijeka" => [Rijeka],
            _ => [],
        };
        factory.Provider.Forecast = Fixture.Load<ForecastResponse>(Fixture.Forecast);
    }

    private static async Task SearchAsync(HttpClient client, string city, int times = 1)
    {
        for (var i = 0; i < times; i++)
            (await client.GetAsync($"/api/weather/forecast?city={city}")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TopCities_ShouldRankCallersCitiesByCountAndHonourTake()
    {
        ScriptCities();
        using var alice = (await factory.RegisterAsync()).Client;
        using var bob = (await factory.RegisterAsync()).Client;
        await SearchAsync(alice, "Zagreb", times: 3);
        await SearchAsync(alice, "Split", times: 2);
        await SearchAsync(alice, "Rijeka");
        await SearchAsync(bob, "Rijeka", times: 5);

        var top3 = (await alice.GetFromJsonAsync<List<TopCityDto>>(TopCities)).ShouldNotBeNull();
        var top1 = (await alice.GetFromJsonAsync<List<TopCityDto>>($"{TopCities}?take=1")).ShouldNotBeNull();

        top3.ShouldBe([
            new TopCityDto("Zagreb", "HR", 3),
            new TopCityDto("Split", "HR", 2),
            new TopCityDto("Rijeka", "HR", 1)]);
        top1.ShouldBe([new TopCityDto("Zagreb", "HR", 3)]);
    }

    [Fact]
    public async Task Recent_ShouldReturnCallersLatestSearchesNewestFirstWithConditions()
    {
        ScriptCities();
        using var alice = (await factory.RegisterAsync()).Client;
        using var bob = (await factory.RegisterAsync()).Client;
        await SearchAsync(alice, "Rijeka");
        await SearchAsync(alice, "Split");
        await SearchAsync(bob, "Zagreb");
        await SearchAsync(alice, "Zagreb");
        await SearchAsync(alice, "Split");

        var recent = (await alice.GetFromJsonAsync<List<SearchRecordDto>>(Recent)).ShouldNotBeNull();
        var recent2 = (await alice.GetFromJsonAsync<List<SearchRecordDto>>($"{Recent}?take=2")).ShouldNotBeNull();

        recent.Select(r => r.City).ShouldBe(["Split", "Zagreb", "Split"]);
        recent.ShouldAllBe(r => r.Condition != "" && r.Icon != "");
        recent.Select(r => r.SearchedAt).ShouldBeInOrder(SortDirection.Descending);
        recent2.Select(r => r.City).ShouldBe(["Split", "Zagreb"]);
    }

    [Fact]
    public async Task Conditions_ShouldCountCallersSearchesPerCondition()
    {
        ScriptCities();
        using var alice = (await factory.RegisterAsync()).Client;
        using var bob = (await factory.RegisterAsync()).Client;
        await SearchAsync(alice, "Zagreb", times: 4);
        await SearchAsync(bob, "Zagreb", times: 2);

        var dist = (await alice.GetFromJsonAsync<List<ConditionCountDto>>(Conditions)).ShouldNotBeNull();

        // The fixture forecast is one city with one snapshot condition, so all rows land in one bucket.
        var bucket = dist.ShouldHaveSingleItem();
        bucket.Condition.ShouldNotBeNullOrWhiteSpace();
        bucket.Count.ShouldBe(4);
    }

    [Fact]
    public async Task AllEndpoints_WhenUserHasNoSearches_ShouldReturnEmptyArrays()
    {
        using var client = (await factory.RegisterAsync()).Client;

        (await client.GetFromJsonAsync<List<TopCityDto>>(TopCities)).ShouldNotBeNull().ShouldBeEmpty();
        (await client.GetFromJsonAsync<List<SearchRecordDto>>(Recent)).ShouldNotBeNull().ShouldBeEmpty();
        (await client.GetFromJsonAsync<List<ConditionCountDto>>(Conditions)).ShouldNotBeNull().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(TopCities)]
    [InlineData(Recent)]
    [InlineData(Conditions)]
    public async Task AllEndpoints_WhenAnonymous_ShouldReturn401(string url)
    {
        using var client = factory.CreateClient();

        (await client.GetAsync(url)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(TopCities, "take=0")]
    [InlineData(TopCities, "take=51")]
    [InlineData(TopCities, "take=abc")]
    [InlineData(Recent, "take=0")]
    [InlineData(Recent, "take=51")]
    public async Task TakeOutOfRange_ShouldReturn400ValidationProblem(string url, string query)
    {
        using var client = (await factory.RegisterAsync()).Client;

        var response = await client.GetAsync($"{url}?{query}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").EnumerateObject().ShouldNotBeEmpty();
    }
}
