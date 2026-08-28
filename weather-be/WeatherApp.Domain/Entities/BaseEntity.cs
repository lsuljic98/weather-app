namespace WeatherApp.Domain.Entities;

public abstract class BaseEntity
{
    // UUIDv7: time-ordered, so the primary-key index stays append-only.
    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; private init; } = DateTimeOffset.UtcNow;

    public override bool Equals(object? obj) =>
        obj is BaseEntity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
