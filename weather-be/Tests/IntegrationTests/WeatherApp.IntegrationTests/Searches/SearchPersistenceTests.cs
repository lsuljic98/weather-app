using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using WeatherApp.Application.Searches;
using WeatherApp.Application.Weather;
using WeatherApp.Infrastructure.UnitTests.Support;
using WeatherApp.Infrastructure.Weather.Responses;
using WeatherApp.IntegrationTests.Support;

namespace WeatherApp.IntegrationTests.Searches;

/// <summary>
/// The forecast endpoint writes a search row to Postgres and the history endpoint reads it
/// back, through the real controllers, DI, EF model and migrations.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SearchPersistenceTests(ApiFactory factory)
{
    private const string Forecast = "/api/weather/forecast?city=Zagreb";
    private const string History = "/api/searches";

    private void ScriptZagreb()
    {
        factory.Provider.Cities = [FakeWeatherApiClient.Zagreb];
        factory.Provider.Forecast = Fixture.Load<ForecastResponse>(Fixture.Forecast);
    }

    [Fact]
    public async Task Forecast_WhenAuthenticated_ShouldReturnForecastAndInsertSnapshotRow()
    {
        ScriptZagreb();
        var (client, user) = await factory.RegisterAsync();
        using var _ = client;

        var response = await client.GetAsync(Forecast);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var forecast = (await response.Content.ReadFromJsonAsync<ForecastDto>()).ShouldNotBeNull();
        forecast.City.ShouldBe("Zagreb");

        await using var db = factory.NewDbContext();
        var row = await db.Searches.SingleAsync(s => s.UserId == user.Id);
        var earliest = forecast.Points.MinBy(p => p.LocalTime).ShouldNotBeNull();
        row.CityName.ShouldBe("Zagreb");
        row.CountryCode.ShouldBe("HR");
        row.Latitude.ShouldBe(FakeWeatherApiClient.Zagreb.Latitude);
        row.Longitude.ShouldBe(FakeWeatherApiClient.Zagreb.Longitude);
        row.TemperatureC.ShouldBe(earliest.TemperatureC);
        row.Humidity.ShouldBe(earliest.Humidity);
        row.WindSpeed.ShouldBe(earliest.WindSpeed);
        row.ConditionMain.ShouldBe(earliest.Condition);
        row.Description.ShouldBe(earliest.Description);
        row.Icon.ShouldBe(earliest.Icon);
        row.CreatedAt.ShouldBe(DateTimeOffset.UtcNow, tolerance: TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Forecast_WhenSearchedRepeatedly_ShouldInsertOneRowPerSearch()
    {
        ScriptZagreb();
        var (client, user) = await factory.RegisterAsync();
        using var _ = client;

        for (var i = 0; i < 3; i++)
            (await client.GetAsync(Forecast)).StatusCode.ShouldBe(HttpStatusCode.OK);

        await using var db = factory.NewDbContext();
        (await db.Searches.CountAsync(s => s.UserId == user.Id)).ShouldBe(3);
    }

    [Fact]
    public async Task Forecast_WhenAnonymous_ShouldReturn401AndInsertNothing()
    {
        ScriptZagreb();
        using var client = factory.CreateClient();
        await using var db = factory.NewDbContext();
        var before = await db.Searches.CountAsync();

        var response = await client.GetAsync(Forecast);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await db.Searches.CountAsync()).ShouldBe(before);
    }

    [Fact]
    public async Task Forecast_WhenCityUnknown_ShouldReturn404AndInsertNothing()
    {
        factory.Provider.Cities = [];
        var (client, user) = await factory.RegisterAsync();
        using var _ = client;

        var response = await client.GetAsync("/api/weather/forecast?city=Nowhere-" + Guid.NewGuid());

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        await using var db = factory.NewDbContext();
        (await db.Searches.AnyAsync(s => s.UserId == user.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task History_ShouldReturnOnlyCallersRowsNewestFirst()
    {
        ScriptZagreb();
        using var aliceClient = (await factory.RegisterAsync()).Client;
        using var bobClient = (await factory.RegisterAsync()).Client;

        await aliceClient.GetAsync(Forecast);
        await bobClient.GetAsync(Forecast);
        await aliceClient.GetAsync(Forecast);

        var page = (await aliceClient.GetFromJsonAsync<PagedResult<SearchRecordDto>>(History)).ShouldNotBeNull();

        page.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(2);
        page.Items.ShouldAllBe(i => i.City == "Zagreb" && i.Country == "HR");
        page.Items[0].SearchedAt.ShouldBeGreaterThanOrEqualTo(page.Items[1].SearchedAt);
        page.Items.Select(i => i.Id).ShouldBeUnique();

        var bobPage = (await bobClient.GetFromJsonAsync<PagedResult<SearchRecordDto>>(History)).ShouldNotBeNull();
        bobPage.TotalCount.ShouldBe(1);
        bobPage.Items.ShouldHaveSingleItem().Id.ShouldNotBeOneOf(page.Items.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task History_ShouldPageWithSkipAndTake()
    {
        ScriptZagreb();
        var (client, user) = await factory.RegisterAsync();
        using var _ = client;
        for (var i = 0; i < 5; i++)
            await client.GetAsync(Forecast);

        var first = (await client.GetFromJsonAsync<PagedResult<SearchRecordDto>>($"{History}?page=1&pageSize=2")).ShouldNotBeNull();
        var third = (await client.GetFromJsonAsync<PagedResult<SearchRecordDto>>($"{History}?page=3&pageSize=2")).ShouldNotBeNull();

        first.TotalCount.ShouldBe(5);
        first.TotalPages.ShouldBe(3);
        first.HasNext.ShouldBeTrue();
        first.HasPrevious.ShouldBeFalse();
        first.Items.Count.ShouldBe(2);

        third.Items.Count.ShouldBe(1);
        third.HasNext.ShouldBeFalse();
        third.HasPrevious.ShouldBeTrue();
        third.Items[0].Id.ShouldNotBeOneOf(first.Items.Select(i => i.Id).ToArray());
    }

    [Fact]
    public async Task History_WhenUserHasNoSearches_ShouldReturnEmptyPage()
    {
        var (client, user) = await factory.RegisterAsync();
        using var _ = client;

        var page = (await client.GetFromJsonAsync<PagedResult<SearchRecordDto>>(History)).ShouldNotBeNull();

        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(0);
        page.TotalPages.ShouldBe(0);
    }

    [Fact]
    public async Task History_WhenAnonymous_ShouldReturn401()
    {
        using var client = factory.CreateClient();

        (await client.GetAsync(History)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=abc")]
    public async Task History_WhenPagingInvalid_ShouldReturn400ValidationProblem(string query)
    {
        var (client, user) = await factory.RegisterAsync();
        using var _ = client;

        var response = await client.GetAsync($"{History}?{query}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").EnumerateObject().ShouldNotBeEmpty();
    }
}
