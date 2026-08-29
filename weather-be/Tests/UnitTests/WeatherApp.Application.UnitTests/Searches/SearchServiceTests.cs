using Shouldly;
using WeatherApp.Application.Searches;
using WeatherApp.Application.UnitTests.Support;
using WeatherApp.Domain.Entities;
using static WeatherApp.Application.UnitTests.Support.Forecasts;

namespace WeatherApp.Application.UnitTests.Searches;

/// <summary>SearchService: a forecast fetch writes exactly one history row, and history pages read back.</summary>
public class SearchServiceTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();

    private readonly FakeWeatherService _weather = new();
    private readonly InMemorySearchRepository _repo = new();
    private readonly SearchService _sut;

    public SearchServiceTests() => _sut = new SearchService(_weather, _repo);

    [Fact]
    public async Task SearchForecastAsync_WhenCityResolves_ShouldReturnForecastAndRecordOneRow()
    {
        _weather.Forecast = Zagreb(Point(Start, temp: 24.5, humidity: 61, wind: 3.2, condition: "Rain", description: "light rain", icon: "10d"));

        var result = await _sut.SearchForecastAsync(UserId, "Zagreb", "HR");

        result.ShouldBeSameAs(_weather.Forecast);
        _weather.LastRequest.ShouldBe(("Zagreb", "HR"));

        var row = _repo.Rows.ShouldHaveSingleItem();
        row.UserId.ShouldBe(UserId);
        row.CityName.ShouldBe("Zagreb");
        row.CountryCode.ShouldBe("HR");
        row.Latitude.ShouldBe(45.8426);
        row.Longitude.ShouldBe(15.9622);
        row.TemperatureC.ShouldBe(24.5);
        row.Humidity.ShouldBe(61);
        row.WindSpeed.ShouldBe(3.2);
        row.ConditionMain.ShouldBe("Rain");
        row.Description.ShouldBe("light rain");
        row.Icon.ShouldBe("10d");
    }

    [Fact]
    public async Task SearchForecastAsync_WhenPointsAreUnordered_ShouldSnapshotEarliestReading()
    {
        _weather.Forecast = Zagreb(
            Point(Start.AddHours(6), temp: 30, condition: "Clouds"),
            Point(Start, temp: 21, condition: "Clear"),
            Point(Start.AddHours(3), temp: 27, condition: "Rain"));

        await _sut.SearchForecastAsync(UserId, "Zagreb");

        var row = _repo.Rows.ShouldHaveSingleItem();
        row.TemperatureC.ShouldBe(21);
        row.ConditionMain.ShouldBe("Clear");
    }

    [Fact]
    public async Task SearchForecastAsync_WhenCityUnknown_ShouldReturnNullAndRecordNothing()
    {
        _weather.Forecast = null;

        var result = await _sut.SearchForecastAsync(UserId, "Nowhere");

        result.ShouldBeNull();
        _repo.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchForecastAsync_WhenReadingHasNoCondition_ShouldStillRecordWithUnknownCondition()
    {
        _weather.Forecast = Zagreb(Point(Start, condition: "", description: "", icon: ""));

        await _sut.SearchForecastAsync(UserId, "Zagreb");

        var row = _repo.Rows.ShouldHaveSingleItem();
        row.ConditionMain.ShouldBe("Unknown");
        row.Description.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task SearchForecastAsync_WhenForecastHasNoPoints_ShouldStillRecordTheSearch()
    {
        _weather.Forecast = Zagreb();

        await _sut.SearchForecastAsync(UserId, "Zagreb");

        var row = _repo.Rows.ShouldHaveSingleItem();
        row.CityName.ShouldBe("Zagreb");
        row.ConditionMain.ShouldBe("Unknown");
    }

    [Fact]
    public async Task SearchForecastAsync_WhenCalledTwice_ShouldRecordTwoRows()
    {
        _weather.Forecast = Zagreb(Point(Start));

        await _sut.SearchForecastAsync(UserId, "Zagreb");
        await _sut.SearchForecastAsync(UserId, "Zagreb");

        _repo.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenUserHasRows_ShouldMapAndPage()
    {
        var other = Guid.CreateVersion7();
        for (var i = 0; i < 5; i++)
            _repo.Rows.Add(Row(UserId, $"City{i}"));
        _repo.Rows.Add(Row(other, "Elsewhere"));

        var page = await _sut.GetHistoryAsync(UserId, page: 2, pageSize: 2);

        page.TotalCount.ShouldBe(5);
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(2);
        page.TotalPages.ShouldBe(3);
        page.HasNext.ShouldBeTrue();
        page.HasPrevious.ShouldBeTrue();
        page.Items.Count.ShouldBe(2);
        page.Items.ShouldAllBe(i => i.City != "Elsewhere");
        _repo.PageRequests.ShouldHaveSingleItem().ShouldBe((2, 2));
    }

    [Fact]
    public async Task GetHistoryAsync_WhenMapping_ShouldCopyEveryField()
    {
        var row = Row(UserId, "Zagreb");
        _repo.Rows.Add(row);

        var dto = (await _sut.GetHistoryAsync(UserId, 1, 10)).Items.ShouldHaveSingleItem();

        dto.Id.ShouldBe(row.Id);
        dto.City.ShouldBe("Zagreb");
        dto.Country.ShouldBe("HR");
        dto.Latitude.ShouldBe(row.Latitude);
        dto.Longitude.ShouldBe(row.Longitude);
        dto.SearchedAt.ShouldBe(row.CreatedAt);
        dto.TemperatureC.ShouldBe(row.TemperatureC);
        dto.Humidity.ShouldBe(row.Humidity);
        dto.WindSpeed.ShouldBe(row.WindSpeed);
        dto.Condition.ShouldBe(row.ConditionMain);
        dto.Description.ShouldBe(row.Description);
        dto.Icon.ShouldBe(row.Icon);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenUserHasNoRows_ShouldReturnEmptyPageWithoutQueryingRows()
    {
        var page = await _sut.GetHistoryAsync(UserId, 1, 20);

        page.Items.ShouldBeEmpty();
        page.TotalCount.ShouldBe(0);
        page.TotalPages.ShouldBe(0);
        page.HasNext.ShouldBeFalse();
        page.HasPrevious.ShouldBeFalse();
        _repo.PageRequests.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(-3, 500, 1, SearchService.MaxPageSize)]
    [InlineData(2, 25, 2, 25)]
    public async Task GetHistoryAsync_WhenPagingOutOfRange_ShouldClamp(int page, int pageSize, int expectedPage, int expectedSize)
    {
        _repo.Rows.Add(Row(UserId, "Zagreb"));

        var result = await _sut.GetHistoryAsync(UserId, page, pageSize);

        result.Page.ShouldBe(expectedPage);
        result.PageSize.ShouldBe(expectedSize);
        _repo.PageRequests.ShouldHaveSingleItem().ShouldBe((expectedPage, expectedSize));
    }

    private static Search Row(Guid userId, string city) =>
        new(userId, city, "HR", 45.8, 15.9, "Clear", "clear sky", "01d", 22.5, 40, 2.5);
}
