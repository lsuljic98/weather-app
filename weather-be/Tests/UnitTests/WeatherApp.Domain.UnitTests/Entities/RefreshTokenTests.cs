using Shouldly;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Domain.UnitTests.Entities;

/// <summary>RefreshToken lifecycle and BaseEntity identity.</summary>
public class RefreshTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Expiry = Now.AddDays(7);

    private static RefreshToken Fresh() => new(Guid.NewGuid(), [1, 2, 3], Expiry);

    [Fact]
    public void IsActive_WhenTokenIsFresh_ShouldBeTrue()
    {
        var token = Fresh();

        token.IsRevoked.ShouldBeFalse();
        token.IsExpired(Now).ShouldBeFalse();
        token.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void IsExpired_WhenNowIsAtExpiryBoundary_ShouldBeInclusive()
    {
        var token = Fresh();

        token.IsExpired(Expiry).ShouldBeTrue();
        token.IsExpired(Expiry.AddTicks(-1)).ShouldBeFalse();
    }

    [Fact]
    public void IsActive_WhenExpiredButNotRevoked_ShouldBeFalse()
    {
        var token = Fresh();

        token.IsRevoked.ShouldBeFalse();
        token.IsActive(Expiry.AddSeconds(1)).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_WhenSuccessorGiven_ShouldRecordTimeAndSuccessor()
    {
        var token = Fresh();
        var successor = Guid.NewGuid();

        token.Revoke(Now, successor);

        token.RevokedAt.ShouldBe(Now);
        token.ReplacedByTokenId.ShouldBe(successor);
        token.IsRevoked.ShouldBeTrue();
        token.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldKeepFirstValues()
    {
        var token = Fresh();
        var first = Guid.NewGuid();

        token.Revoke(Now, first);
        token.Revoke(Now.AddMinutes(5), Guid.NewGuid());

        token.RevokedAt.ShouldBe(Now);
        token.ReplacedByTokenId.ShouldBe(first);
    }

    [Fact]
    public void Revoke_WhenNoSuccessorGiven_ShouldLeaveReplacedByNull()
    {
        var token = Fresh();

        token.Revoke(Now);

        token.IsRevoked.ShouldBeTrue();
        token.ReplacedByTokenId.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WhenTokenHashIsNull_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new RefreshToken(Guid.NewGuid(), null!, Expiry));
    }

    [Fact]
    public void Constructor_WhenCreated_ShouldAssignVersion7IdAndCreatedAt()
    {
        var a = Fresh();
        var b = Fresh();

        a.Id.ShouldNotBe(Guid.Empty);
        a.Id.Version.ShouldBe(7);
        a.Id.ShouldNotBe(b.Id);
        a.CreatedAt.ShouldBeInRange(DateTimeOffset.UtcNow.AddSeconds(-5), DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void Equals_WhenComparingEntities_ShouldUseIdentity()
    {
        var a = Fresh();
        var b = Fresh();

        a.ShouldBe(a);
        a.ShouldNotBe(b);
        a.Equals(null).ShouldBeFalse();
        a.GetHashCode().ShouldNotBe(b.GetHashCode());
    }
}
