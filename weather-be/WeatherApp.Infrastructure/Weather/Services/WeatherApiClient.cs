using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Polly;
using WeatherApp.Application.Exceptions;
using WeatherApp.Infrastructure.Weather.Responses;
using WeatherApp.Infrastructure.Weather.Services.Abstractions;

namespace WeatherApp.Infrastructure.Weather.Services;

public sealed class WeatherApiClient(
    HttpClient httpClient,
    IOptions<WeatherServiceConfiguration> options) : IWeatherApiClient
{
    private readonly string _apiKey = options.Value.ApiKey;

    public async Task<IReadOnlyList<GeocodingResponse>> SearchCitiesAsync(
        string query, int limit = 5, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var uri = $"geo/1.0/direct?q={Uri.EscapeDataString(query)}&limit={Math.Clamp(limit, 1, 5)}&appid={_apiKey}";

        return await SendAsync<IReadOnlyList<GeocodingResponse>>(uri, ct) ?? [];
    }

    public Task<ForecastResponse?> GetForecastAsync(
        double latitude, double longitude, CancellationToken ct = default) =>
        SendAsync<ForecastResponse>(BuildWeatherUri("data/2.5/forecast", latitude, longitude), ct);

    public Task<CurrentWeatherResponse?> GetCurrentAsync(
        double latitude, double longitude, CancellationToken ct = default) =>
        SendAsync<CurrentWeatherResponse>(BuildWeatherUri("data/2.5/weather", latitude, longitude), ct);

    private string BuildWeatherUri(string path, double latitude, double longitude) =>
        // Invariant: a comma decimal separator produces a malformed query.
        FormattableString.Invariant(
            $"{path}?lat={latitude}&lon={longitude}&units=metric&appid={_apiKey}");

    private async Task<T?> SendAsync<T>(string uri, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(uri, ct);
        }
        catch (HttpRequestException ex)
        {
            throw new WeatherApiException("The weather provider could not be reached.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new WeatherApiException("The weather provider did not respond in time.", ex);
        }
        catch (ExecutionRejectedException ex)
        {
            throw new WeatherApiException("The weather provider did not respond in time.", ex);
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.NotFound)
                return default;

            if (!response.IsSuccessStatusCode)
            {
                throw new WeatherApiException(
                    $"The weather provider returned {(int)response.StatusCode} {response.StatusCode}.");
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(ct);
            }
            catch (JsonException ex)
            {
                throw new WeatherApiException("The weather provider returned an unreadable response.", ex);
            }
        }
    }
}
