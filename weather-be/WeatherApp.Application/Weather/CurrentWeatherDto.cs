namespace WeatherApp.Application.Weather;

public sealed record CurrentWeatherDto(
    string City,
    string Country,
    double Latitude,
    double Longitude,
    double TemperatureC,
    double FeelsLikeC,
    int Humidity,
    double WindSpeed,
    string Condition,
    string Description,
    string Icon,
    DateTimeOffset ObservedAt);