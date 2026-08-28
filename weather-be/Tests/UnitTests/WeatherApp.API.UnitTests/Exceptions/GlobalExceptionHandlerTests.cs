using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;
using WeatherApp.API.Exceptions;
using WeatherApp.Application.Exceptions;

namespace WeatherApp.API.UnitTests.Exceptions;

/// <summary>Suite D: GlobalExceptionHandler through the real ProblemDetails writer.</summary>
public class GlobalExceptionHandlerTests
{
    private sealed record Outcome(bool Handled, HttpContext Context, JsonElement Body, FakeLogger<GlobalExceptionHandler> Logger);

    private static async Task<Outcome> RunAsync(Exception exception, IProblemDetailsService? problemDetails = null)
    {
        await using var provider = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/weather/current";
        context.Response.Body = new MemoryStream();
        var logger = new FakeLogger<GlobalExceptionHandler>();
        var handler = new GlobalExceptionHandler(problemDetails ?? provider.GetRequiredService<IProblemDetailsService>(), logger);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var document = text.Length == 0 ? null : JsonDocument.Parse(text);
        var body = document?.RootElement.Clone() ?? default;
        return new Outcome(handled, context, body, logger);
    }

    public static TheoryData<Exception, int, string, LogLevel> Mappings => new()
    {
        { new WeatherApiException("boom"), 502, "Weather provider unavailable", LogLevel.Error },
        { new ArgumentException("latitude must be between -90 and 90"), 400, "Invalid request", LogLevel.Warning },
        { new ArgumentOutOfRangeException("latitude"), 400, "Invalid request", LogLevel.Warning },
        { new OperationCanceledException(), 499, "Request cancelled", LogLevel.Warning },
        { new TaskCanceledException(), 499, "Request cancelled", LogLevel.Warning },
        { new InvalidOperationException("connection string Host=db;Password=secret"), 500, "An unexpected error occurred", LogLevel.Error },
    };

    [Theory]
    [MemberData(nameof(Mappings))]
    public async Task TryHandleAsync_WhenExceptionIsMapped_ShouldSetStatusTitleAndLogLevel(Exception exception, int status, string title, LogLevel level)
    {
        var outcome = await RunAsync(exception);

        outcome.Handled.ShouldBeTrue();
        outcome.Context.Response.StatusCode.ShouldBe(status);
        outcome.Body.GetProperty("status").GetInt32().ShouldBe(status);
        outcome.Body.GetProperty("title").GetString().ShouldBe(title);
        outcome.Context.Response.ContentType.ShouldNotBeNull().ShouldStartWith("application/problem+json");
        outcome.Body.TryGetProperty("traceId", out _).ShouldBeTrue();

        var entry = outcome.Logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        entry.Level.ShouldBe(level);
        entry.Exception.ShouldBeSameAs(exception);
    }

    [Theory]
    [MemberData(nameof(Mappings))]
    public async Task TryHandleAsync_WhenWritingBody_ShouldNotEchoExceptionMessage(Exception exception, int status, string title, LogLevel level)
    {
        _ = (status, title, level);
        var outcome = await RunAsync(exception);

        var body = outcome.Body.GetRawText();
        if (!string.IsNullOrEmpty(exception.Message))
            body.ShouldNotContain(exception.Message);
        body.ShouldNotContain("latitude");
        body.ShouldNotContain("Password");
    }

    [Fact]
    public async Task TryHandleAsync_WhenCalled_ShouldSetInstanceToMethodAndPath()
    {
        var outcome = await RunAsync(new WeatherApiException("boom"));

        outcome.Body.GetProperty("instance").GetString().ShouldBe("GET /api/weather/current");
    }

    [Fact]
    public async Task TryHandleAsync_WhenUpstreamFails_ShouldUseFixedDetailText()
    {
        var outcome = await RunAsync(new WeatherApiException("boom"));

        outcome.Body.GetProperty("detail").GetString()
            .ShouldBe("The upstream weather service could not be reached. Please try again shortly.");
    }

    [Fact]
    public async Task TryHandleAsync_WhenWriterDeclines_ShouldReturnFalse()
    {
        var outcome = await RunAsync(new WeatherApiException("boom"), new DecliningProblemDetailsService());

        outcome.Handled.ShouldBeFalse();
        outcome.Context.Response.StatusCode.ShouldBe(502); // status is set before writing
    }

    [Fact]
    public async Task TryHandleAsync_WhenExceptionWrapsAnother_ShouldMapOuterType()
    {
        var outcome = await RunAsync(new WeatherApiException("boom", new ArgumentException("inner")));

        outcome.Context.Response.StatusCode.ShouldBe(502);
    }

    private sealed class DecliningProblemDetailsService : IProblemDetailsService
    {
        public ValueTask WriteAsync(ProblemDetailsContext context) => ValueTask.CompletedTask;

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context) => ValueTask.FromResult(false);
    }
}
