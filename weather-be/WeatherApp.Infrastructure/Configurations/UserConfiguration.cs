using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();

        // No HasIndex for email here on purpose: PLAN §4 wants uniqueness on lower(email),
        // which the fluent API cannot express. ix_users_email is created as raw SQL in the
        // Initial migration; declaring it here as well would put an index in the model
        // snapshot that does not match the one actually in the database.

        builder.Navigation(u => u.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(u => u.Searches).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
