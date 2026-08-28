namespace WeatherApp.Application.Weather;

public sealed record CityDto(
    string Name,
    string? State,
    string Country,
    double Latitude,
    double Longitude);