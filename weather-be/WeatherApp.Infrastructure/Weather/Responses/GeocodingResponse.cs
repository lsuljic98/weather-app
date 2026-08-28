using System.Text.Json.Serialization;

namespace WeatherApp.Infrastructure.Weather.Responses;

public sealed record GeocodingResponse(
    [property: JsonPropertyName("name"), JsonRequired] string Name,
    [property: JsonPropertyName("state")] string? State, // Absent for most countries outside the US.
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("lat")] double Latitude,
    [property: JsonPropertyName("lon")] double Longitude);
