namespace WeatherApp.Application.Enums;

/// <summary>The outcome of checking a password against a stored hash.</summary>
public enum PasswordVerification
{
    Failed,
    Success,
    SuccessRehashNeeded,
}
