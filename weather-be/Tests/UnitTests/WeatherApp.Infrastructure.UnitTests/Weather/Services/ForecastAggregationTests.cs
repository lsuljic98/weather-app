using Shouldly;
using WeatherApp.Application.Dtos;
using WeatherApp.Infrastructure.UnitTests.Support;
using WeatherApp.Infrastructure.Weather.Responses;

namespace WeatherApp.Infrastructure.UnitTests.Weather.Services;

/// <summary>Suite A: WeatherService.GetForecastAsync → Days / Points.</summary>
public class ForecastAggregationTests
{
    private const double Tolerance = 0.005;

    private static readonly DateOnly[] FixtureDates =
    [
        new(2026, 8, 28), new(2026, 8, 29), new(2026, 8, 30), new(2026, 8, 31), new(2026, 9, 1),
    ];

    private static async Task<ForecastDto> FromFixtureAsync(GeocodingResponse? geocode = null)
    {
        var client = new FakeWeatherApiClient
        {
            Cities = [geocode ?? FakeWeatherApiClient.Zagreb],
            Forecast = Fixture.Load<ForecastResponse>(Fixture.Forecast),
        };
        return (await Make.Service(client).GetForecastAsync("Zagreb"))!;
    }

    private static async Task<ForecastDto> FromAsync(ForecastResponse forecast)
    {
        var client = new FakeWeatherApiClient { Cities = [FakeWeatherApiClient.Zagreb], Forecast = forecast };
        return (await Make.Service(client).GetForecastAsync("Zagreb"))!;
    }

    [Fact]
    public async Task GetForecastAsync_WhenPayloadSpansSixLocalDays_ShouldReturnFirstFiveAscending()
    {
        var dto = await FromFixtureAsync();

        dto.Days.Select(d => d.Date).ShouldBe(FixtureDates);
        dto.Days.ShouldNotContain(d => d.Date == new DateOnly(2026, 9, 2));
    }

    [Fact]
    public async Task GetForecastAsync_WhenFirstDayIsPartial_ShouldReportReadingCounts()
    {
        var dto = await FromFixtureAsync();

        dto.Days.Select(d => d.ReadingCount).ShouldBe(new[] { 4, 8, 8, 8, 8 });
    }

    [Theory]
    [InlineData(0, 27.93, 34.47)]
    [InlineData(1, 22.65, 30.30)]
    [InlineData(2, 18.48, 31.44)]
    [InlineData(3, 20.18, 32.86)]
    [InlineData(4, 20.65, 31.11)]
    public async Task GetForecastAsync_WhenAggregatingDay_ShouldTakeMinAndMaxFromTemp(int index, double min, double max)
    {
        var day = (await FromFixtureAsync()).Days[index];

        day.MinTemperatureC.ShouldBe(min, Tolerance);
        day.MaxTemperatureC.ShouldBe(max, Tolerance);
    }

    [Fact]
    public async Task GetForecastAsync_WhenAggregatingDay_ShouldAverageHumidityRounded()
    {
        var dto = await FromFixtureAsync();

        dto.Days.Select(d => d.Humidity).ShouldBe(new[] { 41, 54, 46, 44, 45 });
    }

    [Fact]
    public async Task GetForecastAsync_WhenAggregatingDay_ShouldTakeMaxWind()
    {
        var dto = await FromFixtureAsync();

        dto.Days.Select(d => Math.Round(d.WindSpeed, 2)).ShouldBe(new[] { 5.78, 3.87, 1.92, 1.86, 3.23 });
    }

    [Fact]
    public async Task GetForecastAsync_WhenAggregatingDay_ShouldTakeMaxPrecipitationProbability()
    {
        var dto = await FromFixtureAsync();

        dto.Days.Select(d => d.PrecipitationProbability).ShouldBe(new[] { 0, 1.0, 0, 0, 0 });
    }

    [Fact]
    public async Task GetForecastAsync_WhenAggregatingDay_ShouldTakeConditionNearestLocalNoon()
    {
        var dto = await FromFixtureAsync();

        Condition(dto.Days[0]).ShouldBe(("Clouds", "scattered clouds", "03d"));
        Condition(dto.Days[1]).ShouldBe(("Rain", "light rain", "10d"));
        foreach (var day in dto.Days.Skip(2))
            Condition(day).ShouldBe(("Clear", "clear sky", "01d"));
    }

    [Fact]
    public async Task GetForecastAsync_WhenCityIsGeocoded_ShouldUseGeocodedIdentity()
    {
        var geocoded = new GeocodingResponse("Geocoded Name", null, "XX", 1.5, 2.5);

        var dto = await FromFixtureAsync(geocoded);

        dto.City.ShouldBe("Geocoded Name");
        dto.Country.ShouldBe("XX");
        dto.Latitude.ShouldBe(1.5);
        dto.Longitude.ShouldBe(2.5);
    }

    [Fact]
    public async Task GetForecastAsync_WhenMappingPoints_ShouldUseLocalTimeOfKeptReadings()
    {
        var dto = await FromFixtureAsync();

        dto.Points.Count.ShouldBe(36);
        dto.Points[0].LocalTime.ShouldBe(new DateTimeOffset(2026, 8, 28, 14, 0, 0, TimeSpan.FromHours(2)));
        dto.Points[0].LocalTime.Offset.ShouldBe(TimeSpan.FromHours(2));
        dto.Points[0].TemperatureC.ShouldBe(33.69, Tolerance);
    }

    [Fact]
    public async Task GetForecastAsync_WhenSixthDayIsDropped_ShouldDropItsPointsToo()
    {
        var dto = await FromFixtureAsync();
        var kept = dto.Days.Select(d => d.Date).ToHashSet();

        dto.Points.ShouldAllBe(p => kept.Contains(DateOnly.FromDateTime(p.LocalTime.Date)));
        dto.Points.ShouldNotContain(p => p.LocalTime.Day == 2);
    }

    [Theory]
    [InlineData(7200, 29)]
    [InlineData(0, 28)]
    public async Task GetForecastAsync_WhenOffsetIsPositive_ShouldBucketLateEveningIntoNextLocalDay(int offset, int expectedDay)
    {
        var forecast = new ForecastBuilder().WithOffset(offset)
            .Add(ForecastBuilder.Utc(2026, 8, 28, 22))
            .Build();

        var dto = await FromAsync(forecast);

        dto.Days.ShouldHaveSingleItem().Date.ShouldBe(new DateOnly(2026, 8, expectedDay));
    }

    [Fact]
    public async Task GetForecastAsync_WhenOffsetIsNegative_ShouldBucketEarlyMorningIntoPreviousLocalDay()
    {
        var forecast = new ForecastBuilder().WithOffset(-28800)
            .Add(ForecastBuilder.Utc(2026, 8, 29, 2))
            .Build();

        var dto = await FromAsync(forecast);

        dto.Days.ShouldHaveSingleItem().Date.ShouldBe(new DateOnly(2026, 8, 28));
    }

    [Fact]
    public async Task GetForecastAsync_WhenFewerThanFiveDays_ShouldReturnThemWithoutPadding()
    {
        var forecast = new ForecastBuilder()
            .Add(ForecastBuilder.Utc(2026, 8, 28, 0))
            .Add(ForecastBuilder.Utc(2026, 8, 29, 0))
            .Add(ForecastBuilder.Utc(2026, 8, 30, 0))
            .Build();

        var dto = await FromAsync(forecast);

        dto.Days.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetForecastAsync_WhenExactlyFiveCompleteDays_ShouldKeepEverything()
    {
        var forecast = new ForecastBuilder().AddSeries(ForecastBuilder.Utc(2026, 8, 28, 0), 40).Build();

        var dto = await FromAsync(forecast);

        dto.Days.Count.ShouldBe(5);
        dto.Days.ShouldAllBe(d => d.ReadingCount == 8);
        dto.Days[^1].Date.ShouldBe(new DateOnly(2026, 9, 1));
        dto.Points.Count.ShouldBe(40);
    }

    [Fact]
    public async Task GetForecastAsync_WhenTwoReadingsTieForNoon_ShouldUseTheEarlier()
    {
        var forecast = new ForecastBuilder()
            .Add(ForecastBuilder.Utc(2026, 8, 28, 9), condition: "Rain", description: "light rain", icon: "10d")
            .Add(ForecastBuilder.Utc(2026, 8, 28, 15), condition: "Clear", description: "clear sky", icon: "01d")
            .Build();

        var dto = await FromAsync(forecast);

        dto.Days.ShouldHaveSingleItem().Condition.ShouldBe("Rain");
    }

    [Fact]
    public async Task GetForecastAsync_WhenWeatherArrayIsEmpty_ShouldReturnEmptyConditionFields()
    {
        var forecast = new ForecastBuilder().Add(ForecastBuilder.Utc(2026, 8, 28, 12), condition: null).Build();

        var dto = await FromAsync(forecast);

        Condition(dto.Days.ShouldHaveSingleItem()).ShouldBe(("", "", ""));
        var point = dto.Points.ShouldHaveSingleItem();
        point.Condition.ShouldBe("");
        point.Icon.ShouldBe("");
    }

    [Fact]
    public async Task GetForecastAsync_WhenHumidityAverageIsHalf_ShouldRoundAwayFromZero()
    {
        var forecast = new ForecastBuilder()
            .Add(ForecastBuilder.Utc(2026, 8, 28, 9), humidity: 60)
            .Add(ForecastBuilder.Utc(2026, 8, 28, 12), humidity: 65)
            .Build();

        var dto = await FromAsync(forecast);

        dto.Days.ShouldHaveSingleItem().Humidity.ShouldBe(63);
    }

    [Fact]
    public async Task GetForecastAsync_WhenEntriesAreUnordered_ShouldReturnAscendingDays()
    {
        var forecast = new ForecastBuilder().AddSeries(ForecastBuilder.Utc(2026, 8, 28, 0), 40).Reverse().Build();

        var dto = await FromAsync(forecast);

        dto.Days.Select(d => d.Date).ShouldBeInOrder();
        dto.Days.ShouldAllBe(d => d.ReadingCount == 8);
    }

    private static (string, string, string) Condition(ForecastDayDto day) => (day.Condition, day.Description, day.Icon);
}
