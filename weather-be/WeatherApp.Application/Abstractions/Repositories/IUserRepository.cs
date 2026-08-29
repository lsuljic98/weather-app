using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Abstractions.Repositories;

/// <summary>Stores and looks up user accounts.</summary>
public interface IUserRepository
{
    /// <summary>The user with this id. Null if none.</summary>
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>The user with this email, matched case-insensitively. Null if none.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
