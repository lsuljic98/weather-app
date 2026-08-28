namespace WeatherApp.Domain.Entities;

/// <summary>
/// An application user. Deliberately not ASP.NET Identity — this is email plus a hash;
/// the hashing itself belongs to the Infrastructure layer.
/// </summary>
public sealed class User : BaseEntity
{
    private readonly List<RefreshToken> _refreshTokens = [];
    private readonly List<Search> _searches = [];

    public User(string email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        Email = email.Trim();
        PasswordHash = passwordHash;
    }

    private User() { } // EF

    public string Email { get; private set; } = null!;

    /// <summary>Output of <c>IPasswordHasher</c> — never the raw password.</summary>
    public string PasswordHash { get; private set; } = null!;

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens;

    public IReadOnlyCollection<Search> Searches => _searches;

    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }
}
