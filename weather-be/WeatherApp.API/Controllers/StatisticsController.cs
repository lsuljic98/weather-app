using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Searches;
using WeatherApp.Application.Statistics;

namespace WeatherApp.API.Controllers;

/// <summary>Per-user aggregates over the search history for the analytics page. One endpoint per card.</summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class StatisticsController(IStatisticsService statistics, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>The caller's most searched cities, most frequent first.</summary>
    /// <param name="take">How many cities to return (1–50, default 3).</param>
    [HttpGet("top-cities")]
    [ProducesResponseType<IReadOnlyList<TopCityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<TopCityDto>>> GetTopCities(
        [FromQuery, Range(1, StatisticsService.MaxTake)] int take = StatisticsService.DefaultTake,
        CancellationToken ct = default)
        => Ok(await statistics.GetTopCitiesAsync(currentUser.UserId, take, ct));

    /// <summary>The caller's latest searches with the conditions at search time, newest first.</summary>
    /// <param name="take">How many searches to return (1–50, default 3).</param>
    [HttpGet("recent")]
    [ProducesResponseType<IReadOnlyList<SearchRecordDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<SearchRecordDto>>> GetRecent(
        [FromQuery, Range(1, StatisticsService.MaxTake)] int take = StatisticsService.DefaultTake,
        CancellationToken ct = default)
        => Ok(await statistics.GetRecentAsync(currentUser.UserId, take, ct));

    /// <summary>How the caller's searches are distributed across condition groups (Clear, Rain, ...), largest first.</summary>
    [HttpGet("conditions")]
    [ProducesResponseType<IReadOnlyList<ConditionCountDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ConditionCountDto>>> GetConditions(CancellationToken ct = default)
        => Ok(await statistics.GetConditionDistributionAsync(currentUser.UserId, ct));
}
