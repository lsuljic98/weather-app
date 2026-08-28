using System.Text.Json.Serialization;

namespace WeatherApp.Infrastructure.Weather.Responses;

public sealed record CurrentWeatherResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("dt")] long Timestamp,
    [property: JsonPropertyName("main")] ForecastMeasurements Main,
    [property: JsonPropertyName("weather")] IReadOnlyList<ForecastCondition> Conditions,
    [property: JsonPropertyName("wind")] ForecastWind Wind,
    [property: JsonPropertyName("sys")] CurrentWeatherSys Sys)
{
    [JsonIgnore]
    public DateTimeOffset ObservedAtUtc => DateTimeOffset.FromUnixTimeSeconds(Timestamp);
}

public sealed record CurrentWeatherSys(
    [property: JsonPropertyName("country")] string? Country);
