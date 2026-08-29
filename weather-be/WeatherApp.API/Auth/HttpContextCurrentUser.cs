using System.Security.Claims;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Exceptions;

namespace WeatherApp.API.Auth;

/// <summary>
/// Reads the user id from the authenticated principal's <c>sub</c> (or NameIdentifier) claim.
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private readonly Lazy<Guid?> _userId = new(() => Resolve(accessor.HttpContext?.User));

    public bool IsAuthenticated => _userId.Value.HasValue;

    public Guid UserId => _userId.Value ?? throw new UnauthenticatedException();

    private static Guid? Resolve(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var raw = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
