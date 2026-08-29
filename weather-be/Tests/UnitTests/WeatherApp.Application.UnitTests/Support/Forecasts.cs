using WeatherApp.Application.Dtos;

namespace WeatherApp.Application.UnitTests.Support;

public static class Forecasts
{
    public static readonly DateTimeOffset Start = new(2026, 8, 28, 14, 0, 0, TimeSpan.FromHours(2));

    public static ForecastPointDto Point(
        DateTimeOffset at, double temp = 20, int humidity = 50, double wind = 1,
        string condition = "Clear", string description = "clear sky", string icon = "01d") =>
        new(at, temp, humidity, wind, 0, condition, description, icon);

    public static ForecastDto Zagreb(params ForecastPointDto[] points) =>
        new("Zagreb", "HR", 45.8426, 15.9622, Days: [], Points: points);
}
