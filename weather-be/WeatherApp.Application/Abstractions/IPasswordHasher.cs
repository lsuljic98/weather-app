namespace WeatherApp.Application.Abstractions;

public enum PasswordVerification
{
    Failed,
    Success,
    /// <summary>Correct password, but the hash uses an older format or work factor and should be replaced.</summary>
    SuccessRehashNeeded,
}

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerification Verify(string hashedPassword, string password);
}
