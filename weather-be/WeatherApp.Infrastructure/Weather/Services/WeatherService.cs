using Microsoft.Extensions.Caching.Memory;
using WeatherApp.Application.Abstractions.Services;
using WeatherApp.Application.Dtos;
using WeatherApp.Infrastructure.Abstractions;
using WeatherApp.Infrastructure.Weather.Responses;

namespace WeatherApp.Infrastructure.Weather.Services;

public sealed class WeatherService(IWeatherApiClient client, IMemoryCache cache) : IWeatherService
{
    // Cache timeouts (TTLs)
    private static readonly TimeSpan GeocodeTtl = TimeSpan.FromHours(12);
    private static readonly TimeSpan WeatherTtl = TimeSpan.FromMinutes(10);

    public async Task<IReadOnlyList<CityDto>> SearchCitiesAsync(
        string query, int limit = 5, CancellationToken ct = default)
    {
        var matches = await client.SearchCitiesAsync(query, limit, ct);

        return [.. matches.Select(m => new CityDto(m.Name, m.State, m.Country, m.Latitude, m.Longitude))];
    }

    public async Task<CurrentWeatherDto?> GetCurrentAsync(
        string city, string? countryCode = null, CancellationToken ct = default)
    {
        var location = await ResolveAsync(city, countryCode, ct);
        if (location is null)
            return null;

        var current = await GetCurrentCachedAsync(location.Latitude, location.Longitude, ct);

        return current is null
            ? null
            : ToDto(current, location.Name, location.Country, location.Latitude, location.Longitude);
    }

    public async Task<CurrentWeatherDto?> GetCurrentAsync(
        double latitude, double longitude, CancellationToken ct = default)
    {
        var current = await GetCurrentCachedAsync(latitude, longitude, ct);

        // No geocoding step here, so the place name comes from the weather reading itself.
        return current is null
            ? null
            : ToDto(current, current.Name, current.Sys.Country ?? string.Empty, latitude, longitude);
    }

    private Task<CurrentWeatherResponse?> GetCurrentCachedAsync(double latitude, double longitude, CancellationToken ct) =>
        GetOrCreateAsync(
            $"current:{Key(latitude, longitude)}", WeatherTtl,
            token => client.GetCurrentAsync(latitude, longitude, token), ct);

    private static CurrentWeatherDto ToDto(
        CurrentWeatherResponse current, string city, string country, double latitude, double longitude)
    {
        var condition = current.Conditions.FirstOrDefault();

        return new CurrentWeatherDto(
            City: city,
            Country: country,
            Latitude: latitude,
            Longitude: longitude,
            TemperatureC: current.Main.TemperatureC,
            FeelsLikeC: current.Main.FeelsLikeC,
            Humidity: current.Main.Humidity,
            WindSpeed: current.Wind.Speed,
            Condition: condition?.Main ?? string.Empty,
            Description: condition?.Description ?? string.Empty,
            Icon: condition?.Icon ?? string.Empty,
            ObservedAt: current.ObservedAtUtc);
    }

    public async Task<ForecastDto?> GetForecastAsync(
        string city, string? countryCode = null, CancellationToken ct = default)
    {
        var location = await ResolveAsync(city, countryCode, ct);
        if (location is null)
            return null;

        var forecast = await GetOrCreateAsync(
            $"forecast:{Key(location.Latitude, location.Longitude)}", WeatherTtl,
            token => client.GetForecastAsync(location.Latitude, location.Longitude, token), ct);

        if (forecast is null)
            return null;

        var offset = forecast.City.TimezoneOffsetSeconds;

        var days = forecast.Entries
            .GroupBy(e => DateOnly.FromDateTime(e.LocalTime(offset).Date))
            .OrderBy(group => group.Key)
                .Take(5) // Because of the trailing last day
            .Select(group => Summarise(group.Key, [.. group], offset))
            .ToArray();

        var keptDates = days.Select(d => d.Date).ToHashSet();

        // Only readings from the kept days, so the chart never shows a day the grid does not have.
        var points = forecast.Entries
            .Where(e => keptDates.Contains(DateOnly.FromDateTime(e.LocalTime(offset).Date)))
            .Select(e => new ForecastPointDto(
                LocalTime: e.LocalTime(offset),
                TemperatureC: e.Main.TemperatureC,
                Humidity: e.Main.Humidity,
                WindSpeed: e.Wind.Speed,
                PrecipitationProbability: e.PrecipitationProbability,
                Condition: e.Conditions.FirstOrDefault()?.Main ?? string.Empty,
                Description: e.Conditions.FirstOrDefault()?.Description ?? string.Empty,
                Icon: e.Conditions.FirstOrDefault()?.Icon ?? string.Empty))
            .ToArray();

        return new ForecastDto(
            City: location.Name,
            Country: location.Country,
            Latitude: location.Latitude,
            Longitude: location.Longitude,
            Days: days,
            Points: points);
    }

    private static ForecastDayDto Summarise(DateOnly date, ForecastEntry[] entries, int offset)
    {
        var midday = entries.MinBy(e => Math.Abs((e.LocalTime(offset).TimeOfDay - TimeSpan.FromHours(12)).Ticks))!;
        var condition = midday.Conditions.FirstOrDefault();

        return new ForecastDayDto(
            Date: date,
            MinTemperatureC: entries.Min(e => e.Main.TemperatureC),
            MaxTemperatureC: entries.Max(e => e.Main.TemperatureC),
            Humidity: (int)Math.Round(entries.Average(e => e.Main.Humidity), MidpointRounding.AwayFromZero),
            WindSpeed: entries.Max(e => e.Wind.Speed),
            PrecipitationProbability: entries.Max(e => e.PrecipitationProbability),
            Condition: condition?.Main ?? string.Empty,
            Description: condition?.Description ?? string.Empty,
            Icon: condition?.Icon ?? string.Empty,
            ReadingCount: entries.Length);
    }

    /// <summary>
    /// Resolves a city name to a geocoding response.
    /// </summary>
    /// <param name="city">City name</param>
    /// <param name="countryCode">Optional country code</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    private async Task<GeocodingResponse?> ResolveAsync(string city, string? countryCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(city))
            return null;

        var query = string.IsNullOrWhiteSpace(countryCode) ? city.Trim() : $"{city.Trim()},{countryCode.Trim()}";

        return await GetOrCreateAsync<GeocodingResponse>(
            $"geocode:{query.ToLowerInvariant()}", GeocodeTtl,
            async token => (await client.SearchCitiesAsync(query, 1, token)).FirstOrDefault(), ct);
    }

    // Rounded to ~1km so nearby coordinates share one API call.
    private static string Key(double latitude, double longitude) =>
        FormattableString.Invariant($"{latitude:F2}:{longitude:F2}");

    /// <summary>
    /// Caches the result of a factory function. Used to avoid redundant API calls.
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="ttl">Timeout of cached values</param>
    /// <param name="factory">Function that is used to compute cached values if a cache miss occurs</param>
    /// <param name="ct">Cancellation token</param>
    /// <typeparam name="T">Type of the cached values</typeparam>
    /// <returns></returns>
    private async Task<T?> GetOrCreateAsync<T>(
        string key, TimeSpan ttl, Func<CancellationToken, Task<T?>> factory, CancellationToken ct)
        where T : class
    {
        // Fetch if cached
        if (cache.TryGetValue(key, out T? cached))
            return cached;

        // Actually compute the value
        var value = await factory(ct);

        if (value is not null)
        {
            cache.Set(key, value, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl,
                Size = 1,
            });
        }

        return value;
    }
}
