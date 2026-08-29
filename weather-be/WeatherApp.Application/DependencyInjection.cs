using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Auth;
using WeatherApp.Application.Searches;

namespace WeatherApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IAuthService, AuthService>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
