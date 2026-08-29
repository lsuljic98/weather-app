namespace WeatherApp.Application.Exceptions;

/// <summary>Raised when a user-scoped operation runs without an authenticated caller.</summary>
public sealed class UnauthenticatedException(string message = "Authentication is required.")
    : Exception(message);
