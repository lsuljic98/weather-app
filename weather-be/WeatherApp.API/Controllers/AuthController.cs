using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WeatherApp.API.Auth;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Abstractions.Services;
using WeatherApp.Application.Auth;
using WeatherApp.Application.Exceptions;

namespace WeatherApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController(
    IAuthService auth,
    ICurrentUser currentUser,
    IOptions<AuthOptions> options) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TokenResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await auth.RegisterAsync(request.Email, request.Password, ct);
        RefreshTokenCookie.Set(Response, result, options.Value);
        return Created("/api/auth/me", TokenResponse.From(result));
    }

    [HttpPost("login")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await auth.LoginAsync(request.Email, request.Password, ct);
        RefreshTokenCookie.Set(Response, result, options.Value);
        return Ok(TokenResponse.From(result));
    }

    /// <summary>Trades the refresh cookie for a new access token and a new refresh cookie.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType<TokenResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Refresh(CancellationToken ct)
    {
        var raw = RefreshTokenCookie.Read(Request) ?? throw new UnauthenticatedException();

        try
        {
            var result = await auth.RefreshAsync(raw, ct);
            RefreshTokenCookie.Set(Response, result, options.Value);
            return Ok(TokenResponse.From(result));
        }
        catch
        {
            // A dead cookie is only noise on every later request.
            RefreshTokenCookie.Clear(Response, options.Value);
            throw;
        }
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await auth.LogoutAsync(RefreshTokenCookie.Read(Request), ct);
        RefreshTokenCookie.Clear(Response, options.Value);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
    {
        var user = await auth.GetUserAsync(currentUser.UserId, ct);
        return user is null ? Unauthorized() : Ok(user);
    }
}
