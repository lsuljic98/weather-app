namespace WeatherApp.Application.Exceptions;

public sealed class EmailTakenException() : Exception("Registration rejected: email is already taken.");

public sealed class InvalidCredentialsException() : Exception("Login rejected: unknown email or wrong password.");

public sealed class InvalidRefreshTokenException() : Exception("Refresh rejected: token unknown, expired, revoked, or replayed.");
