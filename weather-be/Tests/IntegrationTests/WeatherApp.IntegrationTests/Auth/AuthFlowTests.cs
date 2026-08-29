using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using WeatherApp.Application.Auth;
using WeatherApp.IntegrationTests.Support;

namespace WeatherApp.IntegrationTests.Auth;

/// <summary>Register → me → refresh → logout through the real JWT pipeline, cookies and Postgres.</summary>
[Collection(ApiCollection.Name)]
public sealed class AuthFlowTests(ApiFactory factory)
{
    private const string Register = "/api/auth/register";
    private const string Login = "/api/auth/login";
    private const string Refresh = "/api/auth/refresh";
    private const string Logout = "/api/auth/logout";
    private const string Me = "/api/auth/me";

    [Fact]
    public async Task Register_ShouldReturn201WithAccessTokenAndHttpOnlyRefreshCookie()
    {
        using var client = factory.CreateClient();
        var email = ApiFactory.FreshEmail();

        var response = await client.PostAsJsonAsync(Register, new RegisterRequest(email, ApiFactory.Password));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var body = (await response.Content.ReadFromJsonAsync<TokenResponse>()).ShouldNotBeNull();
        body.User.Email.ShouldBe(email);
        body.AccessToken.Split('.').Length.ShouldBe(3);
        body.ExpiresIn.ShouldBe(15 * 60);
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("refreshToken", Case.Insensitive);

        var cookie = response.Headers.GetValues("Set-Cookie").ShouldHaveSingleItem();
        cookie.ShouldStartWith("rt=");
        cookie.ShouldContain("httponly", Case.Insensitive);
        cookie.ShouldContain("path=/api/auth", Case.Insensitive);
        cookie.ShouldContain("samesite=lax", Case.Insensitive);

        await using var db = factory.NewDbContext();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        user.PasswordHash.ShouldNotContain(ApiFactory.Password);
        (await db.RefreshTokens.CountAsync(t => t.UserId == user.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task Register_WhenEmailDiffersOnlyByCase_ShouldReturn409()
    {
        var email = ApiFactory.FreshEmail();
        await factory.RegisterAsync(email);
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Register, new RegisterRequest(email.ToUpperInvariant(), ApiFactory.Password));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Theory]
    [InlineData("not-an-email", "longenough1")]
    [InlineData("ok@example.com", "short")]
    [InlineData("", "")]
    public async Task Register_WhenInvalid_ShouldReturn400ValidationProblem(string email, string password)
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(Register, new { email, password });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").EnumerateObject().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Me_WithBearerToken_ShouldReturnTheUser()
    {
        var (client, user) = await factory.RegisterAsync();
        using var _ = client;

        var me = await client.GetFromJsonAsync<UserDto>(Me);

        me.ShouldBe(user);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Bearer not.a.jwt")]
    public async Task Me_WithoutValidToken_ShouldReturn401ProblemDetails(string? authorization)
    {
        using var client = factory.CreateClient();
        if (authorization is not null)
            client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);

        var response = await client.GetAsync(Me);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ShouldReturn200_WithWrongPassword401()
    {
        var email = ApiFactory.FreshEmail();
        await factory.RegisterAsync(email);
        using var client = factory.CreateClient();

        var ok = await client.PostAsJsonAsync(Login, new LoginRequest(email, ApiFactory.Password));
        var bad = await client.PostAsJsonAsync(Login, new LoginRequest(email, "wrong password"));
        var unknown = await client.PostAsJsonAsync(Login, new LoginRequest(ApiFactory.FreshEmail(), ApiFactory.Password));

        ok.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ok.Content.ReadFromJsonAsync<TokenResponse>())!.User.Email.ShouldBe(email);
        bad.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        unknown.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        // Same title and detail whether the email exists or not.
        var badBody = await bad.Content.ReadFromJsonAsync<JsonElement>();
        var unknownBody = await unknown.Content.ReadFromJsonAsync<JsonElement>();
        badBody.GetProperty("title").GetString().ShouldBe(unknownBody.GetProperty("title").GetString());
        badBody.GetProperty("detail").GetString().ShouldBe(unknownBody.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Refresh_ShouldRotateCookieAndRejectTheOldOne()
    {
        var (client, user) = await factory.RegisterAsync();
        using var _ = client;
        client.DefaultRequestHeaders.Authorization = null; // refresh needs only the cookie

        var first = await client.PostAsync(Refresh, null);
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tokens = (await first.Content.ReadFromJsonAsync<TokenResponse>()).ShouldNotBeNull();
        first.Headers.GetValues("Set-Cookie").ShouldHaveSingleItem().ShouldStartWith("rt=");

        // The new access token works.
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        (await client.GetFromJsonAsync<UserDto>(Me)).ShouldBe(user);

        await using var db = factory.NewDbContext();
        var rows = await db.RefreshTokens.Where(t => t.UserId == user.Id).OrderBy(t => t.CreatedAt).ToListAsync();
        rows.Count.ShouldBe(2);
        rows[0].IsRevoked.ShouldBeTrue();
        rows[0].ReplacedByTokenId.ShouldBe(rows[1].Id);
        rows[1].IsRevoked.ShouldBeFalse();
    }

    [Fact]
    public async Task Refresh_WithoutCookie_ShouldReturn401()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync(Refresh, null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Refresh_WhenOldCookieReplayed_ShouldReturn401AndRevokeChain()
    {
        using var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync(Register, new RegisterRequest(ApiFactory.FreshEmail(), ApiFactory.Password));
        var stolen = CookieValue(register);
        var user = (await register.Content.ReadFromJsonAsync<TokenResponse>())!.User;

        // The legitimate client rotates once; the stolen copy is now revoked.
        (await client.PostAsync(Refresh, null)).StatusCode.ShouldBe(HttpStatusCode.OK);

        using var thief = factory.CreateClient();
        thief.DefaultRequestHeaders.Add("Cookie", $"rt={stolen}");
        var replay = await thief.PostAsync(Refresh, null);

        replay.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // ...and the legitimate client's current token is dead as well: forced re-login.
        (await client.PostAsync(Refresh, null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await using var db = factory.NewDbContext();
        (await db.RefreshTokens.Where(t => t.UserId == user.Id).AllAsync(t => t.RevokedAt != null)).ShouldBeTrue();
    }

    [Fact]
    public async Task Logout_ShouldRevokeTokenClearCookieAndBlockRefresh()
    {
        var (client, user) = await factory.RegisterAsync();
        using var _ = client;

        var logout = await client.PostAsync(Logout, null);

        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var cleared = logout.Headers.GetValues("Set-Cookie").ShouldHaveSingleItem();
        cleared.ShouldStartWith("rt=;");
        cleared.ShouldContain("expires=", Case.Insensitive);

        await using var db = factory.NewDbContext();
        (await db.RefreshTokens.SingleAsync(t => t.UserId == user.Id)).IsRevoked.ShouldBeTrue();

        (await client.PostAsync(Refresh, null)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutSession_ShouldStillReturn204()
    {
        using var client = factory.CreateClient();

        (await client.PostAsync(Logout, null)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static string CookieValue(HttpResponseMessage response)
    {
        var header = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("rt="));
        return header[3..header.IndexOf(';')];
    }
}
