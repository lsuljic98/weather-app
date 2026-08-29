namespace WeatherApp.Application.Weather;

public sealed record ForecastDto(
    string City,
    string Country,
    double Latitude,
    double Longitude,
    IReadOnlyList<ForecastDayDto> Days,
    IReadOnlyList<ForecastPointDto> Points);

public sealed record ForecastPointDto(
    DateTimeOffset LocalTime,
    double TemperatureC,
    int Humidity,
    double WindSpeed,
    double PrecipitationProbability,
    string Condition,
    string Description,
    string Icon);

public sealed record ForecastDayDto(
    DateOnly Date,
    double MinTemperatureC,
    double MaxTemperatureC,
    int Humidity,
    double WindSpeed,
    double PrecipitationProbability,
    string Condition,
    string Description,
    string Icon,
    // Below 8 on a partial day.
    int ReadingCount);
