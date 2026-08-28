namespace WeatherApp.Application.Exceptions;

/// <summary>Upstream provider failed. "City not found" is described with null instead.</summary>
public sealed class WeatherApiException(string message, Exception? innerException = null)
    : Exception(message, innerException);
