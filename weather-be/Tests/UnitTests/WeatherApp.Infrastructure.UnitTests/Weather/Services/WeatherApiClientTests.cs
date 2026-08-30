using System.Globalization;
using System.Net;
using System.Text.Json;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Shouldly;
using WeatherApp.Application.Exceptions;
using WeatherApp.Infrastructure.UnitTests.Support;

namespace WeatherApp.Infrastructure.UnitTests.Weather.Services;

/// <summary>WeatherApiClient error mapping and URI building.</summary>
public class WeatherApiClientTests
{
    private const string EmptyForecast = """{"city":{"name":"Z","country":"HR","timezone":0},"list":[]}""";
    private const string NotFoundBody = """{"cod":"404","message":"city not found"}""";

    private static StubHttpMessageHandler Responding(HttpStatusCode status, string body = "{}") =>
        new(_ => StubHttpMessageHandler.Json(status, body));

    private static StubHttpMessageHandler Throwing(Exception exception) => new(_ => throw exception);

    [Fact]
    public async Task GetForecastAsync_WhenResponseIsOk_ShouldDeserialiseForecast()
    {
        var client = Make.Client(Responding(HttpStatusCode.OK, Fixture.Text(Fixture.Forecast)));

        var forecast = await client.GetForecastAsync(45.8426, 15.9622);

        forecast.ShouldNotBeNull().Entries.Count.ShouldBe(40);
    }

    [Fact]
    public async Task GetForecastAsync_WhenResponseIsNotFound_ShouldReturnNull()
    {
        var client = Make.Client(Responding(HttpStatusCode.NotFound, NotFoundBody));

        (await client.GetForecastAsync(1, 2)).ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentAsync_WhenResponseIsNotFound_ShouldReturnNull()
    {
        var client = Make.Client(Responding(HttpStatusCode.NotFound, NotFoundBody));

        (await client.GetCurrentAsync(1, 2)).ShouldBeNull();
    }

    [Fact]
    public async Task SearchCitiesAsync_WhenResponseIsNotFound_ShouldReturnEmptyList()
    {
        var client = Make.Client(Responding(HttpStatusCode.NotFound));

        var cities = await client.SearchCitiesAsync("Nowhere");

        cities.ShouldNotBeNull().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCurrentAsync_WhenResponseIsUnauthorized_ShouldThrowWithoutUpstreamBody()
    {
        var body = """{"cod":401,"message":"Invalid API key. Please see https://openweathermap.org/faq#error401 for more info."}""";
        var client = Make.Client(Responding(HttpStatusCode.Unauthorized, body));

        var ex = await Should.ThrowAsync<WeatherApiException>(() => client.GetCurrentAsync(1, 2));

        ex.Message.ShouldContain("401");
        ex.Message.ShouldContain("Unauthorized");
        ex.Message.ShouldNotContain("Invalid API key");
        ex.InnerException.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task GetForecastAsync_WhenResponseIsFailureStatus_ShouldThrowWeatherApiException(HttpStatusCode status)
    {
        var client = Make.Client(Responding(status));

        await Should.ThrowAsync<WeatherApiException>(() => client.GetForecastAsync(1, 2));
    }

    [Fact]
    public async Task GetForecastAsync_WhenBodyIsMalformedJson_ShouldThrowWithJsonExceptionInner()
    {
        var client = Make.Client(Responding(HttpStatusCode.OK, """{"city":"""));

        var ex = await Should.ThrowAsync<WeatherApiException>(() => client.GetForecastAsync(1, 2));

        ex.InnerException.ShouldBeAssignableTo<JsonException>();
    }

    [Fact]
    public async Task GetForecastAsync_WhenBodyIsLiteralNull_ShouldReturnNull()
    {
        var client = Make.Client(Responding(HttpStatusCode.OK, "null"));

        (await client.GetForecastAsync(1, 2)).ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentAsync_WhenHttpRequestExceptionThrown_ShouldThrowUnreachable()
    {
        var inner = new HttpRequestException("dns");
        var client = Make.Client(Throwing(inner));

        var ex = await Should.ThrowAsync<WeatherApiException>(() => client.GetCurrentAsync(1, 2));

        ex.Message.ShouldContain("could not be reached");
        ex.InnerException.ShouldBeSameAs(inner);
    }

    [Fact]
    public async Task GetCurrentAsync_WhenTaskCanceledWithoutCallerCancellation_ShouldThrowTimeout()
    {
        var client = Make.Client(Throwing(new TaskCanceledException("client timeout")));

        var ex = await Should.ThrowAsync<WeatherApiException>(() => client.GetCurrentAsync(1, 2));

        ex.Message.ShouldContain("did not respond in time");
    }

    [Fact]
    public async Task GetForecastAsync_WhenCallerTokenIsCancelled_ShouldPropagateAndSendNothing()
    {
        var handler = Responding(HttpStatusCode.OK, EmptyForecast);
        var client = Make.Client(handler);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => client.GetForecastAsync(1, 2, cts.Token));

        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetCurrentAsync_WhenResilienceTimeoutThrown_ShouldThrowWeatherApiException()
    {
        var client = Make.Client(Throwing(new TimeoutRejectedException("slow")));

        var ex = await Should.ThrowAsync<WeatherApiException>(() => client.GetCurrentAsync(1, 2));

        ex.InnerException.ShouldBeOfType<TimeoutRejectedException>();
    }

    [Fact]
    public async Task GetCurrentAsync_WhenCircuitIsOpen_ShouldThrowWeatherApiException()
    {
        var client = Make.Client(Throwing(new BrokenCircuitException("open")));

        await Should.ThrowAsync<WeatherApiException>(() => client.GetCurrentAsync(1, 2));
    }

    [Fact]
    public async Task SearchCitiesAsync_WhenCalled_ShouldBuildExactUri()
    {
        var handler = Responding(HttpStatusCode.OK, "[]");

        await Make.Client(handler).SearchCitiesAsync("Velika Gorica", 5);

        handler.Requests.ShouldHaveSingleItem().AbsoluteUri
            .ShouldBe($"https://unit.test/geo/1.0/direct?q=Velika%20Gorica&limit=5&appid={Make.ApiKey}");
    }

    [Theory]
    [InlineData(0, "limit=1")]
    [InlineData(10, "limit=5")]
    public async Task SearchCitiesAsync_WhenLimitIsOutOfRange_ShouldClampIt(int limit, string expected)
    {
        var handler = Responding(HttpStatusCode.OK, "[]");

        await Make.Client(handler).SearchCitiesAsync("Zagreb", limit);

        handler.Requests.ShouldHaveSingleItem().Query.ShouldContain(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SearchCitiesAsync_WhenQueryIsBlank_ShouldReturnEmptyWithoutRequest(string query)
    {
        var handler = Responding(HttpStatusCode.OK, "[]");

        var cities = await Make.Client(handler).SearchCitiesAsync(query);

        cities.ShouldBeEmpty();
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetForecastAsync_WhenCalled_ShouldBuildExactUri()
    {
        var handler = Responding(HttpStatusCode.OK, EmptyForecast);

        await Make.Client(handler).GetForecastAsync(45.8426, 15.9622);

        handler.Requests.ShouldHaveSingleItem().AbsoluteUri
            .ShouldBe($"https://unit.test/data/2.5/forecast?lat=45.8426&lon=15.9622&units=metric&appid={Make.ApiKey}");
    }

    [Fact]
    public async Task GetCurrentAsync_WhenCalled_ShouldBuildExactUri()
    {
        var handler = Responding(HttpStatusCode.OK, EmptyForecast);

        await Make.Client(handler).GetCurrentAsync(45.8426, 15.9622);

        handler.Requests.ShouldHaveSingleItem().AbsoluteUri
            .ShouldBe($"https://unit.test/data/2.5/weather?lat=45.8426&lon=15.9622&units=metric&appid={Make.ApiKey}");
    }

    [Fact]
    public async Task GetForecastAsync_WhenCultureUsesCommaDecimal_ShouldUseInvariantCoordinates()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("hr-HR");
        try
        {
            45.8426.ToString().ShouldBe("45,8426"); // the culture is in effect
            var handler = Responding(HttpStatusCode.OK, EmptyForecast);

            await Make.Client(handler).GetForecastAsync(45.8426, 15.9622);

            handler.Requests.ShouldHaveSingleItem().Query.ShouldContain("lat=45.8426&lon=15.9622");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public async Task SearchCitiesAsync_WhenQueryHasDiacritics_ShouldPercentEncodeIt()
    {
        var handler = Responding(HttpStatusCode.OK, "[]");

        await Make.Client(handler).SearchCitiesAsync("Đakovo");

        handler.Requests.ShouldHaveSingleItem().Query.ShouldContain("q=%C4%90akovo");
    }

    [Fact]
    public async Task GetForecastAsync_WhenLongitudeIsNegative_ShouldKeepTheSign()
    {
        var handler = Responding(HttpStatusCode.OK, EmptyForecast);

        await Make.Client(handler).GetForecastAsync(39.7990175, -89.6439575);

        handler.Requests.ShouldHaveSingleItem().Query.ShouldContain("lon=-89.6439575");
    }
}
