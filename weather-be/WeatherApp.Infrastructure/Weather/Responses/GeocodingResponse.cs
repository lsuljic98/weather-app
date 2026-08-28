using System.Text.Json.Serialization;

namespace WeatherApp.Infrastructure.Weather.Responses;

public sealed record GeocodingResponse(
    [property: JsonPropertyName("name")] string Name,
    // Absent for most countries outside the US.
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("lat")] double Latitude,
    [property: JsonPropertyName("lon")] double Longitude);
