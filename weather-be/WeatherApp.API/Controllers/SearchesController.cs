using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Abstractions.Services;
using WeatherApp.Application.Constants;
using WeatherApp.Application.Dtos;

namespace WeatherApp.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class SearchesController(ISearchService searchService, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>The caller's search history, newest first. Always read from the database.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<SearchRecordDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<SearchRecordDto>>> GetHistory(
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, SearchHistoryLimits.MaxPageSize)] int pageSize = SearchHistoryLimits.DefaultPageSize,
        CancellationToken ct = default)
        => Ok(await searchService.GetHistoryAsync(currentUser.UserId, page, pageSize, ct));
}
