namespace WeatherApp.Application.Abstractions;

/// <summary>
/// The caller behind the current request. Every user-scoped query takes the id from here
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The caller's user id. Throws <see cref="Exceptions.UnauthenticatedException"/> when there is none.</summary>
    Guid UserId { get; }
}
