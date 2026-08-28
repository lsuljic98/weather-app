namespace WeatherApp.Application.Weather;

public sealed record CurrentWeatherDto(string City, int TemperatureC, int TemperatureF, string Summary);

public sealed record ForecastDayDto(DateOnly Date, int TemperatureC, int TemperatureF, string Summary);

public sealed record ForecastDto(string City, IReadOnlyList<ForecastDayDto> Days);
