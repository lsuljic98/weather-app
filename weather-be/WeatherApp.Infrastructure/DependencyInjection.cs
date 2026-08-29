using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Abstractions.Repositories;
using WeatherApp.Application.Abstractions.Services;
using WeatherApp.Application.Auth;
using WeatherApp.Infrastructure.Abstractions;
using WeatherApp.Infrastructure.Auth;
using WeatherApp.Infrastructure.Repositories;
using WeatherApp.Infrastructure.Weather;
using WeatherApp.Infrastructure.Weather.Services;

namespace WeatherApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured. Set ConnectionStrings:Default in " +
                "appsettings.json, or ConnectionStrings__Default in the environment.");

        services.AddDbContext<WeatherDbContext>(options => options.UseNpgsql(connectionString));

        services.AddOptions<WeatherServiceConfiguration>()
            .Bind(configuration.GetSection(WeatherServiceConfiguration.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<AuthOptions>()
            .Bind(configuration.GetSection(AuthOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddMemoryCache(options => options.SizeLimit = 1_000);

        services.AddHttpClient<IWeatherApiClient, WeatherApiClient>((provider, http) =>
            {
                var settings = provider.GetRequiredService<IOptions<WeatherServiceConfiguration>>().Value;

                // BaseAddress needs the trailing slash, or a relative path replaces the last segment.
                http.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/");

                // The resilience pipeline below controls every timeout
                http.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
            });

        services.AddScoped<IWeatherService, WeatherService>();
        services.AddScoped<ISearchRepository, SearchRepository>();

        return services;
    }
}
