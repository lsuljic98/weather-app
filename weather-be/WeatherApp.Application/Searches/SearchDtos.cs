namespace WeatherApp.Application.Searches;

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
    string Icon);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNext => Page < TotalPages;

    public bool HasPrevious => Page > 1;
}
