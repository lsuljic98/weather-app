using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeatherApp.Application.Weather;
using WeatherApp.Infrastructure.Weather;

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

        services.AddSingleton<IWeatherService, InMemoryWeatherService>();

        return services;
    }
}
