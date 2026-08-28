namespace WeatherApp.Domain.Entities;

/// <summary>
/// One forecast search performed by a user. The current-conditions snapshot is
/// denormalised onto the row on purpose: history and statistics are then single-table
/// reads that never call OpenWeather again. <see cref="BaseEntity.CreatedAt"/> is the
/// search time and maps to <c>searched_at</c>.
/// </summary>
public sealed class Search : BaseEntity
{
    public Search(
        Guid userId,
        string cityName,
        string countryCode,
        double latitude,
        double longitude,
        string conditionMain,
        string description,
        string icon,
        double temperatureC,
        int humidity,
        double windSpeed)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(countryCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionMain);
        ArgumentOutOfRangeException.ThrowIfLessThan(latitude, -90);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(latitude, 90);
        ArgumentOutOfRangeException.ThrowIfLessThan(longitude, -180);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(longitude, 180);
        ArgumentOutOfRangeException.ThrowIfNegative(humidity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(humidity, 100);

        UserId = userId;
        CityName = cityName;
        CountryCode = countryCode;
        Latitude = latitude;
        Longitude = longitude;
        ConditionMain = conditionMain;
        Description = description;
        Icon = icon;
        TemperatureC = temperatureC;
        Humidity = humidity;
        WindSpeed = windSpeed;
    }

    private Search() { } // EF needs this

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    /// <summary>Canonical name as resolved by geocoding, not the raw text the user typed.</summary>
    public string CityName { get; private set; } = null!;

    /// <summary>ISO 3166-1 alpha-2, e.g. <c>HR</c>.</summary>
    public string CountryCode { get; private set; } = null!;

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    /// <summary>OpenWeather condition group: <c>Clear</c>, <c>Rain</c>, <c>Clouds</c>, … Groups the distribution chart.</summary>
    public string ConditionMain { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    /// <summary>OpenWeather icon code, e.g. <c>04d</c>.</summary>
    public string Icon { get; private set; } = null!;

    public double TemperatureC { get; private set; }

    /// <summary>Percent, 0–100.</summary>
    public int Humidity { get; private set; }

    /// <summary>Metres per second, as returned by OpenWeather with <c>units=metric</c>.</summary>
    public double WindSpeed { get; private set; }
}
