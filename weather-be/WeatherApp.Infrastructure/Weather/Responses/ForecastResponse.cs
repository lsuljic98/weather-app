using System.Text.Json.Serialization;

namespace WeatherApp.Infrastructure.Weather.Responses;

public sealed record ForecastResponse(
    [property: JsonPropertyName("city")] ForecastCity City,
    [property: JsonPropertyName("list")] IReadOnlyList<ForecastEntry> Entries);

public sealed record ForecastCity(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("country")] string Country,
    // Seconds from UTC. Entries must be bucketed into days with this, not UTC.
    [property: JsonPropertyName("timezone")] int TimezoneOffsetSeconds);

public sealed record ForecastEntry(
    [property: JsonPropertyName("dt")] long Timestamp,
    [property: JsonPropertyName("main")] ForecastMeasurements Main,
    [property: JsonPropertyName("weather")] IReadOnlyList<ForecastCondition> Conditions,
    [property: JsonPropertyName("wind")] ForecastWind Wind,
    // Per 3-hour block, 0-1. Aggregate with max, never a sum.
    [property: JsonPropertyName("pop")] double PrecipitationProbability)
{
    [JsonIgnore]
    public DateTimeOffset TimestampUtc => DateTimeOffset.FromUnixTimeSeconds(Timestamp);

    public DateTimeOffset LocalTime(int timezoneOffsetSeconds) =>
        TimestampUtc.ToOffset(TimeSpan.FromSeconds(timezoneOffsetSeconds));
}

public sealed record ForecastMeasurements(
    [property: JsonPropertyName("temp")] double TemperatureC,
    [property: JsonPropertyName("feels_like")] double FeelsLikeC,
    [property: JsonPropertyName("humidity")] int Humidity);

public sealed record ForecastCondition(
    [property: JsonPropertyName("main")] string Main,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("icon")] string Icon);

public sealed record ForecastWind(
    [property: JsonPropertyName("speed")] double Speed);
