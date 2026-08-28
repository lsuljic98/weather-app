using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Infrastructure.Weather;

/// <summary>
/// Settings for the external weather API. Bound from the
/// <c>WeatherServiceConfiguration</c> configuration section.
/// </summary>
public sealed class WeatherServiceConfiguration
{
    public const string SectionName = "WeatherServiceConfiguration";

    /// <summary>
    /// Root address of the upstream weather API, without a trailing path — for example
    /// <c>https://api.openweathermap.org</c>. Endpoint paths are appended by the client.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Subscription key for the upstream weather API. Never committed: supplied from user
    /// secrets in development and from the environment (<c>WeatherServiceConfiguration__ApiKey</c>)
    /// everywhere else.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = string.Empty;
}
