using System.ComponentModel.DataAnnotations;

namespace WeatherApp.Infrastructure.Weather;

public sealed class WeatherServiceConfiguration
{
    public const string SectionName = "WeatherServiceConfiguration";

    /// <summary>Host only, no trailing path. The client appends endpoint paths.</summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>Never committed: user secrets locally, environment elsewhere.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = string.Empty;
}
