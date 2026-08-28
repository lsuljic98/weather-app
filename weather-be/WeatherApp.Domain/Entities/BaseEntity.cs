namespace WeatherApp.Domain.Entities;

/// <summary>
/// Fields shared by every persisted entity. <see cref="Id"/> is generated in the domain
/// rather than by the database: a UUIDv7 is time-ordered, so it keeps the primary-key
/// index append-only, and it lets an entity be referenced before it is saved.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; private init; } = Guid.CreateVersion7();

    /// <summary>UTC. Mapped to <c>created_at</c> — on <see cref="Search"/>, to <c>searched_at</c>.</summary>
    public DateTimeOffset CreatedAt { get; private init; } = DateTimeOffset.UtcNow;

    public override bool Equals(object? obj) =>
        obj is BaseEntity other && GetType() == other.GetType() && Id == other.Id;

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
