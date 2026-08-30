using System.Text.Json;
using Shouldly;
using WeatherApp.Infrastructure.UnitTests.Support;
using WeatherApp.Infrastructure.Weather.Responses;

namespace WeatherApp.Infrastructure.UnitTests.Weather.Responses;

/// <summary>The response records against captured provider payloads.</summary>
public class ResponseDeserializationTests
{
    [Fact]
    public void Deserialize_WhenForecastFixture_ShouldMapEveryUsedField()
    {
        var forecast = Fixture.Load<ForecastResponse>(Fixture.Forecast);

        forecast.City.Name.ShouldBe("Zagreb");
        forecast.City.Country.ShouldBe("HR");
        forecast.City.TimezoneOffsetSeconds.ShouldBe(7200);
        forecast.Entries.Count.ShouldBe(40);
        forecast.Entries.ShouldAllBe(e => e.Main != null && e.Conditions != null && e.Wind != null);

        var first = forecast.Entries[0];
        first.Timestamp.ShouldBe(1787918400);
        first.Main.TemperatureC.ShouldBe(33.69);
        first.Main.FeelsLikeC.ShouldBe(34.44);
        first.Main.Humidity.ShouldBe(38);
        (first.Conditions[0].Main, first.Conditions[0].Description, first.Conditions[0].Icon)
            .ShouldBe(("Clouds", "scattered clouds", "03d"));
        first.Wind.Speed.ShouldBe(5.78);
        first.PrecipitationProbability.ShouldBe(0);
    }

    [Fact]
    public void LocalTime_WhenOffsetApplied_ShouldShiftFromTimestampUtc()
    {
        var first = Fixture.Load<ForecastResponse>(Fixture.Forecast).Entries[0];

        first.TimestampUtc.ShouldBe(new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero));

        var zagreb = first.LocalTime(7200);
        zagreb.ShouldBe(new DateTimeOffset(2026, 8, 28, 14, 0, 0, TimeSpan.FromHours(2)));
        zagreb.Offset.ShouldBe(TimeSpan.FromHours(2));

        var pacific = first.LocalTime(-28800);
        pacific.ShouldBe(new DateTimeOffset(2026, 8, 28, 4, 0, 0, TimeSpan.FromHours(-8)));
        pacific.Offset.ShouldBe(TimeSpan.FromHours(-8));
    }

    [Fact]
    public void Deserialize_WhenCurrentFixture_ShouldMapEveryUsedField()
    {
        var current = Fixture.Load<CurrentWeatherResponse>(Fixture.Current);

        current.Name.ShouldBe("Britanski trg");
        current.Timestamp.ShouldBe(1787923997);
        current.ObservedAtUtc.ShouldBe(new DateTimeOffset(2026, 8, 28, 13, 33, 17, TimeSpan.Zero));
        current.Main.TemperatureC.ShouldBe(35.72);
        current.Main.FeelsLikeC.ShouldBe(38.26);
        current.Main.Humidity.ShouldBe(39);
        current.Wind.Speed.ShouldBe(1.03);
        (current.Conditions[0].Main, current.Conditions[0].Description, current.Conditions[0].Icon)
            .ShouldBe(("Clouds", "few clouds", "02d"));
        current.Sys.Country.ShouldBe("HR");
    }

    [Fact]
    public void Deserialize_WhenSpringfieldFixture_ShouldMapStates()
    {
        var hits = Fixture.Load<List<GeocodingResponse>>(Fixture.Springfield);

        hits.Count.ShouldBe(5);
        hits.ShouldAllBe(h => h.State != null);
        (hits[0].Name, hits[0].State, hits[0].Country).ShouldBe(("Springfield", "Illinois", "US"));
        hits[0].Latitude.ShouldBe(39.7990175);
        hits[0].Longitude.ShouldBe(-89.6439575);
    }

    [Fact]
    public void Deserialize_WhenOsijekFixture_ShouldTolerateMissingStateAndUnmappedFields()
    {
        var hits = Fixture.Load<List<GeocodingResponse>>(Fixture.Osijek);

        hits.Count.ShouldBe(3);
        hits[0].State.ShouldBeNull();
        hits[1].State.ShouldBeNull();
        hits[2].State.ShouldBe("Federation of Bosnia and Herzegovina");
        (hits[0].Latitude, hits[0].Longitude).ShouldBe((45.5548793, 18.6953685));
    }

    [Fact]
    public void Deserialize_WhenUnknownPropertiesPresent_ShouldIgnoreThem()
    {
        const string json = """{"city":{"name":"Z","country":"HR","timezone":0,"population":1,"sunrise":1},"list":[]}""";

        var forecast = JsonSerializer.Deserialize<ForecastResponse>(json)!;

        forecast.City.Name.ShouldBe("Z");
        forecast.Entries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""[{"name":"Osijek","lat":1,"lon":2,"country":"HR"}]""")]
    [InlineData("""[{"name":"Osijek","state":null,"lat":1,"lon":2,"country":"HR"}]""")]
    public void Deserialize_WhenStateIsAbsentOrNull_ShouldMapToNull(string json)
    {
        var hits = JsonSerializer.Deserialize<List<GeocodingResponse>>(json)!;

        hits.ShouldHaveSingleItem().State.ShouldBeNull();
    }

    [Fact]
    public void Deserialize_WhenNameIsMissing_ShouldThrowJsonException()
    {
        const string json = """[{"lat":1,"lon":2,"country":"HR"}]""";

        Should.Throw<JsonException>(() => JsonSerializer.Deserialize<List<GeocodingResponse>>(json));
    }
}
