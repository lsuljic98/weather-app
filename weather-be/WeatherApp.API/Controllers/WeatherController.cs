using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WeatherApp.Application.Weather;

namespace WeatherApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class WeatherController(IWeatherService weatherService) : ControllerBase
{
    /// <summary>Current conditions for a city.</summary>
    [HttpGet("current/{city}")]
    [ProducesResponseType<CurrentWeatherDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentWeatherDto>> GetCurrent(string city, CancellationToken ct)
    {
        var current = await weatherService.GetCurrentAsync(city, ct);
        return current is null ? NotFound() : Ok(current);
    }

    /// <summary>Daily forecast for a city.</summary>
    [HttpGet("forecast/{city}")]
    [ProducesResponseType<ForecastDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ForecastDto>> GetForecast(
        string city,
        [FromQuery] [Range(1, 14)] int days,
        CancellationToken ct)
    {
        var forecast = await weatherService.GetForecastAsync(city, days, ct);
        return forecast is null ? NotFound() : Ok(forecast);
    }
}
