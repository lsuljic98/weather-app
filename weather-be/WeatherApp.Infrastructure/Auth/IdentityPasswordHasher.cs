using Microsoft.AspNetCore.Identity;
using WeatherApp.Application.Abstractions;
using WeatherApp.Application.Enums;

namespace WeatherApp.Infrastructure.Auth;

/// <summary>
/// ASP.NET Core Identity hasher behind the application's interface.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly object Placeholder = new();
    private readonly PasswordHasher<object> _innerHasher = new();

    public string Hash(string password) => _innerHasher.HashPassword(Placeholder, password);

    public PasswordVerification Verify(string hashedPassword, string password)
    {
        PasswordVerificationResult result;
        try
        {
            result = _innerHasher.VerifyHashedPassword(Placeholder, hashedPassword, password);
        }
        catch (FormatException)
        {
            return PasswordVerification.Failed;
        }

        return result switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
            _ => PasswordVerification.Failed,
        };
    }
}
