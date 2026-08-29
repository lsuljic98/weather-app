using Microsoft.EntityFrameworkCore;
using Npgsql;
using WeatherApp.Application.Abstractions.Repositories;
using WeatherApp.Application.Exceptions;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Repositories;

public sealed class UserRepository(WeatherDbContext context) : IUserRepository
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
        context.Users.SingleOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        // Translates to lower(email) = lower(@p), which the ix_users_email index serves.
        var needle = email.ToLower();
        return context.Users.SingleOrDefaultAsync(u => u.Email.ToLower() == needle, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        context.Users.Add(user);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Two registrations raced past the pre-check; the unique index is the real guard.
            context.Entry(user).State = EntityState.Detached;
            throw new EmailTakenException();
        }
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
