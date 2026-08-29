using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeatherApp.Application.Abstractions.Services;
using WeatherApp.Application.Auth;
using WeatherApp.Application.Searches;
using WeatherApp.Application.Statistics;

namespace WeatherApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IAuthService, AuthService>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
