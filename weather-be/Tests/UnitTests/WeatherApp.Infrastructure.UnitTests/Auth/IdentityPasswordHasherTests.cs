using Shouldly;
using WeatherApp.Application.Abstractions;
using WeatherApp.Infrastructure.Auth;

namespace WeatherApp.Infrastructure.UnitTests.Auth;

public class IdentityPasswordHasherTests
{
    private readonly IdentityPasswordHasher _sut = new();

    [Fact]
    public void Hash_ShouldRoundTripAndNotContainThePassword()
    {
        var hash = _sut.Hash("correct horse battery staple");

        hash.ShouldNotContain("correct horse");
        _sut.Verify(hash, "correct horse battery staple").ShouldBe(PasswordVerification.Success);
        _sut.Verify(hash, "correct horse battery stapl").ShouldBe(PasswordVerification.Failed);
    }

    [Fact]
    public void Hash_ShouldSaltSoEqualPasswordsDiffer()
    {
        _sut.Hash("same").ShouldNotBe(_sut.Hash("same"));
    }

    [Fact]
    public void Verify_WithGarbageHash_ShouldFailNotThrow()
    {
        _sut.Verify("not-a-hash", "anything").ShouldBe(PasswordVerification.Failed);
    }
}
