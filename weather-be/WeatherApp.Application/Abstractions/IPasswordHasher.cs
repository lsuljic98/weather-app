using WeatherApp.Application.Enums;

namespace WeatherApp.Application.Abstractions;

/// <summary>Hashes passwords for storage and verifies them on login.</summary>
public interface IPasswordHasher
{
    /// <summary>A salted hash of the password, safe to store.</summary>
    string Hash(string password);

    /// <summary>Checks the password against a stored hash. A malformed hash counts as a failure.</summary>
    PasswordVerification Verify(string hashedPassword, string password);
}
