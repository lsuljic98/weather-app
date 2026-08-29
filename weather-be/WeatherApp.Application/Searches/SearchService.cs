using WeatherApp.Application.Abstractions.Repositories;
using WeatherApp.Application.Abstractions.Services;
using WeatherApp.Application.Constants;
using WeatherApp.Application.Dtos;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Searches;

public sealed class SearchService(IWeatherService weather, ISearchRepository searches) : ISearchService
{
    // Used when the provider sends a reading without a condition
    private const string UnknownCondition = "Unknown";

    public async Task<ForecastDto?> SearchForecastAsync(
        Guid userId, string city, string? countryCode = null, CancellationToken ct = default)
    {
        var forecast = await weather.GetForecastAsync(city, countryCode, ct);
        if (forecast is null)
            return null;

        await searches.AddAsync(Snapshot(userId, forecast), ct);

        return forecast;
    }

    public async Task<PagedResult<SearchRecordDto>> GetHistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, SearchHistoryLimits.MaxPageSize);

        var total = await searches.CountAsync(userId, ct);
        var rows = total == 0
            ? []
            : await searches.GetPageAsync(userId, page, pageSize, ct);

        return new PagedResult<SearchRecordDto>([.. rows.Select(SearchRecordDto.From)], page, pageSize, total);
    }

    /// <summary>
    /// The conditions at search time, taken from the earliest reading in the forecast: it is at
    /// most three hours away and costs no extra provider call.
    /// </summary>
    private static Search Snapshot(Guid userId, ForecastDto forecast)
    {
        var now = forecast.Points.MinBy(p => p.LocalTime);

        return new Search(
            userId,
            forecast.City,
            forecast.Country,
            forecast.Latitude,
            forecast.Longitude,
            conditionMain: Or(now?.Condition, UnknownCondition),
            description: Or(now?.Description, string.Empty),
            icon: Or(now?.Icon, string.Empty),
            temperatureC: now?.TemperatureC ?? 0,
            humidity: now?.Humidity ?? 0,
            windSpeed: now?.WindSpeed ?? 0);
    }

    private static string Or(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
