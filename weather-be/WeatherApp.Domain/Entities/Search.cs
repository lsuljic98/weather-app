namespace WeatherApp.Domain.Entities;

/// <summary>
/// One forecast search. The current-conditions snapshot is denormalised onto the row so
/// history and statistics are single-table reads. CreatedAt is the search time.
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

    /// <summary>Canonical name from geocoding, not the raw text typed.</summary>
    public string CityName { get; private set; } = null!;

    /// <summary>ISO 3166-1 alpha-2.</summary>
    public string CountryCode { get; private set; } = null!;

    public double Latitude { get; private set; }

    public double Longitude { get; private set; }

    /// <summary>Condition group (Clear, Rain, Clouds, ...); groups the distribution chart.</summary>
    public string ConditionMain { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public string Icon { get; private set; } = null!;

    public double TemperatureC { get; private set; }

    public int Humidity { get; private set; }

    /// <summary>Metres per second.</summary>
    public double WindSpeed { get; private set; }
}
