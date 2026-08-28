using System.Text.Json;

namespace WeatherApp.Infrastructure.UnitTests.Support;

/// <summary>Loads the captured provider payloads copied to the output directory.</summary>
public static class Fixture
{
    public const string Forecast = "forecast-zagreb.json";
    public const string Current = "current-zagreb.json";
    public const string Springfield = "geocode-springfield.json";
    public const string Osijek = "geocode-osijek.json";

    public static string Text(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));

    public static T Load<T>(string name) => JsonSerializer.Deserialize<T>(Text(name))!;
}
