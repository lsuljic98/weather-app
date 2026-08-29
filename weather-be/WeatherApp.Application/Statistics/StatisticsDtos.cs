namespace WeatherApp.Application.Statistics;

/// <summary>How many times the user searched one city.</summary>
public sealed record TopCityDto(string City, string Country, int Count);

/// <summary>How many of the user's searches hit one condition group (Clear, Rain, ...).</summary>
public sealed record ConditionCountDto(string Condition, int Count);
