using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using WeatherApp.Application.Auth;
using WeatherApp.Domain.Entities;
using WeatherApp.Infrastructure.Auth;

namespace WeatherApp.Infrastructure.UnitTests.Auth;

public class JwtAccessTokenIssuerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);

    private static readonly AuthOptions Options = new()
    {
        Key = "0123456789abcdef0123456789abcdef",
        Issuer = "weather-api",
        Audience = "weather-web",
        AccessTokenMinutes = 15,
    };

    private static JwtAccessTokenIssuer Issuer(AuthOptions? options = null) =>
        new(Microsoft.Extensions.Options.Options.Create(options ?? Options));

    [Fact]
    public async Task Issue_ShouldProduceHs256TokenWithExpectedClaimsAndExpiry()
    {
        var user = new User("leon@example.com", "hash");

        var access = Issuer().Issue(user, Now);

        access.ExpiresAt.ShouldBe(Now.AddMinutes(15));

        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(access.Token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = JwtAccessTokenIssuer.SigningKey(Options),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = (_, expires, _, _) => expires == Now.AddMinutes(15).UtcDateTime,
        });

        result.IsValid.ShouldBeTrue(result.Exception?.ToString());
        var jwt = (JsonWebToken)result.SecurityToken;
        jwt.Subject.ShouldBe(user.Id.ToString());
        jwt.GetClaim("email").Value.ShouldBe("leon@example.com");
        Guid.TryParse(jwt.Id, out _).ShouldBeTrue();
        jwt.IssuedAt.ShouldBe(Now.UtcDateTime);
        jwt.ValidFrom.ShouldBe(Now.UtcDateTime);
        jwt.ValidTo.ShouldBe(Now.AddMinutes(15).UtcDateTime);
    }

    [Fact]
    public void Issue_ShouldGiveEveryTokenItsOwnJti()
    {
        var user = new User("leon@example.com", "hash");
        var issuer = Issuer();

        var a = new JsonWebToken(issuer.Issue(user, Now).Token);
        var b = new JsonWebToken(issuer.Issue(user, Now).Token);

        a.Id.ShouldNotBe(b.Id);
    }

    [Fact]
    public async Task Issue_TokenSignedWithOtherKey_ShouldNotValidate()
    {
        var other = new AuthOptions { Key = "ffffffffffffffffffffffffffffffff" };
        var access = Issuer(other).Issue(new User("leon@example.com", "hash"), Now);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(access.Token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = JwtAccessTokenIssuer.SigningKey(Options),
            ValidateLifetime = false,
        });

        result.IsValid.ShouldBeFalse();
    }
}
