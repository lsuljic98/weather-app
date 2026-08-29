using Shouldly;
using WeatherApp.Application.Constants;
using WeatherApp.Application.Statistics;
using WeatherApp.Application.UnitTests.Support;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.UnitTests.Statistics;

/// <summary>StatisticsService: scopes every aggregate to the user, clamps take, maps snapshots to DTOs.</summary>
public class StatisticsServiceTests
{
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid OtherUser = Guid.CreateVersion7();

    private readonly InMemorySearchRepository _repo = new();
    private readonly StatisticsService _sut;

    public StatisticsServiceTests() => _sut = new StatisticsService(_repo);

    [Fact]
    public async Task GetTopCitiesAsync_ShouldReturnMostFrequentFirstForThatUserOnly()
    {
        Add(UserId, "Zagreb", "HR"); Add(UserId, "Zagreb", "HR"); Add(UserId, "Zagreb", "HR");
        Add(UserId, "Split", "HR"); Add(UserId, "Split", "HR");
        Add(UserId, "Rijeka", "HR");
        Add(OtherUser, "Rijeka", "HR"); Add(OtherUser, "Rijeka", "HR"); Add(OtherUser, "Rijeka", "HR"); Add(OtherUser, "Rijeka", "HR");

        var top = await _sut.GetTopCitiesAsync(UserId, take: 2);

        top.ShouldBe([new TopCityDto("Zagreb", "HR", 3), new TopCityDto("Split", "HR", 2)]);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    [InlineData(500, StatisticsLimits.MaxTake)]
    public async Task GetTopCitiesAsync_ShouldClampTake(int requested, int expected)
    {
        await _sut.GetTopCitiesAsync(UserId, requested);

        _repo.TopCitiesTakes.ShouldBe([expected]);
    }

    [Fact]
    public async Task GetRecentAsync_ShouldReturnNewestFirstWithSnapshotConditions()
    {
        Add(UserId, "Oldest", condition: "Clear");
        Add(UserId, "Middle", condition: "Rain");
        Add(OtherUser, "Bobs");
        Add(UserId, "Newest", condition: "Snow", temp: -2);

        var recent = await _sut.GetRecentAsync(UserId, take: 3);

        recent.Select(r => r.City).ShouldBe(["Newest", "Middle", "Oldest"]);
        recent[0].Condition.ShouldBe("Snow");
        recent[0].TemperatureC.ShouldBe(-2);
        _repo.PageRequests.ShouldBe([(1, 3)]);
    }

    [Fact]
    public async Task GetRecentAsync_ShouldRequestFirstPageOfClampedSize()
    {
        await _sut.GetRecentAsync(UserId, take: 999);

        _repo.PageRequests.ShouldBe([(1, StatisticsLimits.MaxTake)]);
    }

    [Fact]
    public async Task GetConditionDistributionAsync_ShouldCountPerConditionLargestFirst()
    {
        Add(UserId, "A", condition: "Rain"); Add(UserId, "B", condition: "Rain");
        Add(UserId, "C", condition: "Clear");
        Add(UserId, "D", condition: "Clouds"); Add(UserId, "E", condition: "Clouds"); Add(UserId, "F", condition: "Clouds");
        Add(OtherUser, "G", condition: "Snow");

        var dist = await _sut.GetConditionDistributionAsync(UserId);

        dist.ShouldBe([
            new ConditionCountDto("Clouds", 3),
            new ConditionCountDto("Rain", 2),
            new ConditionCountDto("Clear", 1)]);
    }

    [Fact]
    public async Task AllAggregates_WhenUserHasNoSearches_ShouldReturnEmpty()
    {
        Add(OtherUser, "X");

        (await _sut.GetTopCitiesAsync(UserId, 3)).ShouldBeEmpty();
        (await _sut.GetRecentAsync(UserId, 3)).ShouldBeEmpty();
        (await _sut.GetConditionDistributionAsync(UserId)).ShouldBeEmpty();
    }

    private void Add(Guid userId, string city, string country = "HR", string condition = "Clear", double temp = 20) =>
        _repo.Rows.Add(new Search(userId, city, country, 45.8, 15.9, condition, "sky", "01d", temp, 40, 2.5));
}
