using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Dtos;

public sealed record SearchRecordDto(
    Guid Id,
    string City,
    string Country,
    double Latitude,
    double Longitude,
    DateTimeOffset SearchedAt,
    double TemperatureC,
    int Humidity,
    double WindSpeed,
    string Condition,
    string Description,
    string Icon)
{
    public static SearchRecordDto From(Search s) => new(
        s.Id,
        s.CityName,
        s.CountryCode,
        s.Latitude,
        s.Longitude,
        s.CreatedAt,
        s.TemperatureC,
        s.Humidity,
        s.WindSpeed,
        s.ConditionMain,
        s.Description,
        s.Icon);
}
