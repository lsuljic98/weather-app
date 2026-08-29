using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using WeatherApp.Application.Auth;
using WeatherApp.Infrastructure;
using WeatherApp.Infrastructure.Abstractions;
using WeatherApp.Infrastructure.UnitTests.Support;

namespace WeatherApp.IntegrationTests.Support;

/// <summary>
/// The real host on a real Postgres (Testcontainers), with only the OpenWeather client swapped
/// out. Auth is the real JWT pipeline: tests register through the API. Shared by every test in
/// the collection; the container starts once, migrations run once.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtKey = "integration-test-signing-key-32-bytes!!";
    public const string Password = "correct horse battery staple";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("weatherapp")
        .WithUsername("weatherapp")
        .WithPassword("weatherapp")
        .Build();

    /// <summary>Scripted provider. Tests set Cities / Forecast before calling the API.</summary>
    public FakeWeatherApiClient Provider { get; } = new();

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Builds the host, which runs the startup migration path (ApplyMigrationsOnStartup below).
        _ = Services;
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("WeatherServiceConfiguration:ApiKey", "integration-test-key");
        builder.UseSetting("Auth:Key", JwtKey);
        builder.UseSetting("Auth:SecureCookie", "false"); // the test server is plain HTTP
        builder.UseSetting("ApplyMigrationsOnStartup", "true");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWeatherApiClient>();
            services.AddSingleton<IWeatherApiClient>(Provider);
        });
    }

    public static string FreshEmail() => $"{Guid.CreateVersion7():N}@example.com";

    /// <summary>Registers a new user through the API and returns a client carrying their bearer token (and refresh cookie).</summary>
    public async Task<(HttpClient Client, UserDto User)> RegisterAsync(string? email = null)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email ?? FreshEmail(), Password));
        response.EnsureSuccessStatusCode();

        var tokens = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return (client, tokens.User);
    }

    public WeatherDbContext NewDbContext() =>
        new(new DbContextOptionsBuilder<WeatherDbContext>().UseNpgsql(ConnectionString).Options);
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
