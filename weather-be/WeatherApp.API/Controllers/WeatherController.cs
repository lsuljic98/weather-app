using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Weather;

namespace WeatherApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class WeatherController(
    IWeatherService weatherService,
    ISearchService searchService,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("cities")]
    [ProducesResponseType<IReadOnlyList<CityDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CityDto>>> SearchCities(
        [FromQuery] string q,
        CancellationToken ct)
        => Ok(await weatherService.SearchCitiesAsync(q, ct: ct));

    [HttpGet("current")]
    [ProducesResponseType<CurrentWeatherDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentWeatherDto>> GetCurrent(
        [FromQuery] string city,
        [FromQuery] string? countryCode,
        CancellationToken ct)
    {
        var current = await weatherService.GetCurrentAsync(city, countryCode, ct);
        return current is null ? NotFound() : Ok(current);
    }

    /// <summary>
    /// Fetching a forecast is a search, so this GET also records it in the caller's history.
    /// Deliberate: it keeps the FE from having to report the search separately.
    /// </summary>
    [HttpGet("forecast")]
    [ProducesResponseType<ForecastDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ForecastDto>> GetForecast(
        [FromQuery] string city,
        [FromQuery] string? countryCode,
        CancellationToken ct)
    {
        var forecast = await searchService.SearchForecastAsync(currentUser.UserId, city, countryCode, ct);
        return forecast is null ? NotFound() : Ok(forecast);
    }
}
