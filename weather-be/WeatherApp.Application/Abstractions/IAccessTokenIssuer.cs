using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Abstractions;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

/// <summary>Signs short-lived access tokens.</summary>
public interface IAccessTokenIssuer
{
    /// <summary>A signed token for the user, valid from <paramref name="now"/> until its expiry.</summary>
    AccessToken Issue(User user, DateTimeOffset now);
}
