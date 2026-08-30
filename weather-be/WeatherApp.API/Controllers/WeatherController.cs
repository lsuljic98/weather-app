using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Abstractions.Services;
using WeatherApp.Application.Dtos;

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

    /// <summary>Current conditions at a coordinate — the "where am I" widget. Not recorded as a search.</summary>
    [HttpGet("current/coordinates")]
    [ProducesResponseType<CurrentWeatherDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentWeatherDto>> GetCurrentAtCoordinates(
        [FromQuery, BindRequired, Range(-90.0, 90.0)] double lat,
        [FromQuery, BindRequired, Range(-180.0, 180.0)] double lon,
        CancellationToken ct)
    {
        var current = await weatherService.GetCurrentAsync(lat, lon, ct);
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
