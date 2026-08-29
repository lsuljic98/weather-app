using Shouldly;
using WeatherApp.Application.Auth;
using WeatherApp.Application.Exceptions;
using WeatherApp.Application.UnitTests.Support;

namespace WeatherApp.Application.UnitTests.Auth;

/// <summary>AuthService: registration, login, refresh rotation with replay detection, logout.</summary>
public class AuthServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 8, 29, 10, 0, 0, TimeSpan.Zero);

    private readonly InMemoryUserRepository _users = new();
    private readonly InMemoryRefreshTokenRepository _tokens = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeAccessTokenIssuer _issuer = new();
    private readonly FakeClock _clock = new(Start);
    private readonly AuthService _sut;

    public AuthServiceTests() =>
        _sut = new AuthService(_users, _tokens, _hasher, _issuer, AuthTestOptions.Default(), _clock);

    // ---- register -------------------------------------------------------------------------

    [Fact]
    public async Task RegisterAsync_ShouldStoreHashedPasswordAndSignIn()
    {
        var result = await _sut.RegisterAsync(" leon@example.com ", "correct horse");

        var user = _users.Rows.ShouldHaveSingleItem();
        user.Email.ShouldBe("leon@example.com");
        user.PasswordHash.ShouldBe(FakePasswordHasher.Prefix + "correct horse");
        user.PasswordHash.ShouldNotContain("correct horse\0"); // sanity: never the raw value alone

        result.User.ShouldBe(new UserDto(user.Id, "leon@example.com"));
        result.AccessToken.ShouldStartWith("jwt-for-");
        result.ExpiresIn.ShouldBe(15 * 60);
        result.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        result.RefreshTokenExpiresAt.ShouldBe(Start.AddDays(7));
    }

    [Fact]
    public async Task RegisterAsync_ShouldStoreOnlyTheHashOfTheRefreshToken()
    {
        var result = await _sut.RegisterAsync("leon@example.com", "correct horse");

        var stored = _tokens.Rows.ShouldHaveSingleItem();
        stored.TokenHash.ShouldBe(AuthService.HashToken(result.RefreshToken));
        stored.TokenHash.Length.ShouldBe(32);
        stored.ExpiresAt.ShouldBe(Start.AddDays(7));
        stored.IsRevoked.ShouldBeFalse();
        Convert.ToBase64String(stored.TokenHash).ShouldNotBe(result.RefreshToken);
    }

    [Theory]
    [InlineData("leon@example.com")]
    [InlineData("LEON@example.com")]
    public async Task RegisterAsync_WhenEmailTaken_ShouldThrowEmailTaken(string again)
    {
        await _sut.RegisterAsync("leon@example.com", "correct horse");

        await Should.ThrowAsync<EmailTakenException>(() => _sut.RegisterAsync(again, "other password"));
        _users.Rows.Count.ShouldBe(1);
    }

    // ---- login ----------------------------------------------------------------------------

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ShouldIssueNewTokenPair()
    {
        var registered = await _sut.RegisterAsync("leon@example.com", "correct horse");

        var login = await _sut.LoginAsync("leon@example.com", "correct horse");

        login.User.ShouldBe(registered.User);
        login.RefreshToken.ShouldNotBe(registered.RefreshToken);
        _tokens.Rows.Count.ShouldBe(2);
        _tokens.Rows.ShouldAllBe(t => !t.IsRevoked); // logging in elsewhere does not kill the first session
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ShouldThrowInvalidCredentials()
    {
        await _sut.RegisterAsync("leon@example.com", "correct horse");

        await Should.ThrowAsync<InvalidCredentialsException>(() => _sut.LoginAsync("leon@example.com", "wrong"));
        _tokens.Rows.Count.ShouldBe(1);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ShouldStillVerifyAPasswordThenThrow()
    {
        await Should.ThrowAsync<InvalidCredentialsException>(() => _sut.LoginAsync("nobody@example.com", "whatever"));

        // Same work as a real verification, so timing does not reveal which emails exist.
        _hasher.Verifications.ShouldBe(1);
    }

    [Fact]
    public async Task LoginAsync_WhenRehashNeeded_ShouldRehashAndSave()
    {
        await _sut.RegisterAsync("leon@example.com", "correct horse");
        var savesBefore = _users.Saves;
        _hasher.ReportRehashNeeded = true;

        await _sut.LoginAsync("leon@example.com", "correct horse");

        _users.Saves.ShouldBe(savesBefore + 1);
        _users.Rows.Single().PasswordHash.ShouldBe(FakePasswordHasher.Prefix + "correct horse");
    }

    // ---- refresh --------------------------------------------------------------------------

    [Fact]
    public async Task RefreshAsync_ShouldRotate_RevokingOldTokenAndLinkingReplacement()
    {
        var first = await _sut.RegisterAsync("leon@example.com", "correct horse");
        _clock.Advance(TimeSpan.FromHours(1));

        var second = await _sut.RefreshAsync(first.RefreshToken);

        second.RefreshToken.ShouldNotBe(first.RefreshToken);
        second.AccessToken.ShouldNotBe(first.AccessToken);
        second.RefreshTokenExpiresAt.ShouldBe(Start.AddHours(1).AddDays(7));

        var old = _tokens.Rows.Single(t => t.TokenHash.AsSpan().SequenceEqual(AuthService.HashToken(first.RefreshToken)));
        var fresh = _tokens.Rows.Single(t => t.TokenHash.AsSpan().SequenceEqual(AuthService.HashToken(second.RefreshToken)));
        old.IsRevoked.ShouldBeTrue();
        old.RevokedAt.ShouldBe(Start.AddHours(1));
        old.ReplacedByTokenId.ShouldBe(fresh.Id);
        fresh.IsActive(_clock.Now).ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenUnknown_ShouldThrow()
    {
        await Should.ThrowAsync<InvalidRefreshTokenException>(() => _sut.RefreshAsync("not-a-token"));
    }

    [Fact]
    public async Task RefreshAsync_WhenTokenExpired_ShouldThrow()
    {
        var result = await _sut.RegisterAsync("leon@example.com", "correct horse");
        _clock.Advance(TimeSpan.FromDays(7));

        await Should.ThrowAsync<InvalidRefreshTokenException>(() => _sut.RefreshAsync(result.RefreshToken));
    }

    [Fact]
    public async Task RefreshAsync_WhenRevokedTokenIsReplayed_ShouldRevokeEntireChain()
    {
        var first = await _sut.RegisterAsync("leon@example.com", "correct horse");
        var second = await _sut.RefreshAsync(first.RefreshToken);
        var third = await _sut.RefreshAsync(second.RefreshToken);

        // The first token is presented again: a stolen copy, or the client and thief racing.
        await Should.ThrowAsync<InvalidRefreshTokenException>(() => _sut.RefreshAsync(first.RefreshToken));

        _tokens.Rows.ShouldAllBe(t => t.IsRevoked);
        await Should.ThrowAsync<InvalidRefreshTokenException>(() => _sut.RefreshAsync(third.RefreshToken));
    }

    // ---- logout ---------------------------------------------------------------------------

    [Fact]
    public async Task LogoutAsync_ShouldRevokeTheTokenSoItCannotRefresh()
    {
        var result = await _sut.RegisterAsync("leon@example.com", "correct horse");

        await _sut.LogoutAsync(result.RefreshToken);

        _tokens.Rows.Single().IsRevoked.ShouldBeTrue();
        await Should.ThrowAsync<InvalidRefreshTokenException>(() => _sut.RefreshAsync(result.RefreshToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public async Task LogoutAsync_WhenNoOrUnknownToken_ShouldSucceedSilently(string? token)
    {
        var savesBefore = _tokens.Saves;

        await Should.NotThrowAsync(() => _sut.LogoutAsync(token));

        _tokens.Saves.ShouldBe(savesBefore);
    }

    // ---- me -------------------------------------------------------------------------------

    [Fact]
    public async Task GetUserAsync_ShouldReturnDtoOrNull()
    {
        var result = await _sut.RegisterAsync("leon@example.com", "correct horse");

        (await _sut.GetUserAsync(result.User.Id)).ShouldBe(result.User);
        (await _sut.GetUserAsync(Guid.CreateVersion7())).ShouldBeNull();
    }
}
