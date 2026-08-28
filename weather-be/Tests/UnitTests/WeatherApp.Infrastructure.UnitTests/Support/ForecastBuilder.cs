using WeatherApp.Infrastructure.Weather.Responses;

namespace WeatherApp.Infrastructure.UnitTests.Support;

/// <summary>Builds synthetic ForecastResponse payloads for cases the fixture cannot express.</summary>
public sealed class ForecastBuilder
{
    private readonly List<ForecastEntry> _entries = [];
    private int _offsetSeconds;

    public ForecastBuilder WithOffset(int seconds)
    {
        _offsetSeconds = seconds;
        return this;
    }

    public ForecastBuilder Add(
        DateTimeOffset utc,
        double temp = 20,
        int humidity = 50,
        double wind = 1,
        double pop = 0,
        string? condition = "Clear",
        string description = "clear sky",
        string icon = "01d")
    {
        IReadOnlyList<ForecastCondition> conditions =
            condition is null ? [] : [new ForecastCondition(condition, description, icon)];

        _entries.Add(new ForecastEntry(
            utc.ToUnixTimeSeconds(),
            new ForecastMeasurements(temp, temp, humidity),
            conditions,
            new ForecastWind(wind),
            pop));

        return this;
    }

    /// <summary>Adds <paramref name="count"/> readings starting at <paramref name="startUtc"/>, 3 hours apart.</summary>
    public ForecastBuilder AddSeries(DateTimeOffset startUtc, int count)
    {
        for (var i = 0; i < count; i++)
            Add(startUtc.AddHours(3 * i));
        return this;
    }

    public ForecastBuilder Reverse()
    {
        _entries.Reverse();
        return this;
    }

    public ForecastResponse Build() =>
        new(new ForecastCity("Fixture City", "XX", _offsetSeconds), _entries.ToArray());

    public static DateTimeOffset Utc(int year, int month, int day, int hour) =>
        new(year, month, day, hour, 0, 0, TimeSpan.Zero);
}
