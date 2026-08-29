using WeatherApp.Domain.Entities;

namespace WeatherApp.Application.Abstractions;

/// <summary>Stores and looks up user accounts.</summary>
public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Case-insensitive lookup.</summary>
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);

    Task AddAsync(User user, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
