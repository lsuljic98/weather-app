using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WeatherApp.Domain.Entities;

namespace WeatherApp.Infrastructure.Configurations;

public sealed class SearchConfiguration : IEntityTypeConfiguration<Search>
{
    public void Configure(EntityTypeBuilder<Search> builder)
    {
        builder.ToTable("searches");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(s => s.CreatedAt).HasColumnName("searched_at");
        builder.Property(s => s.UserId).HasColumnName("user_id");
        builder.Property(s => s.CityName).HasColumnName("city_name").HasMaxLength(200).IsRequired();
        builder.Property(s => s.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
        builder.Property(s => s.Latitude).HasColumnName("latitude");
        builder.Property(s => s.Longitude).HasColumnName("longitude");
        builder.Property(s => s.ConditionMain).HasColumnName("condition_main").HasMaxLength(50).IsRequired();
        builder.Property(s => s.Description).HasColumnName("description").HasMaxLength(200).IsRequired();
        builder.Property(s => s.Icon).HasColumnName("icon").HasMaxLength(10).IsRequired();
        builder.Property(s => s.TemperatureC).HasColumnName("temp_c");
        builder.Property(s => s.Humidity).HasColumnName("humidity");
        builder.Property(s => s.WindSpeed).HasColumnName("wind_speed");

        // Matches the three read paths: history (newest first), top cities, condition distribution.
        builder.HasIndex(s => new { s.UserId, s.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_searches_user_id_searched_at");
        builder.HasIndex(s => new { s.UserId, s.CityName })
            .HasDatabaseName("ix_searches_user_id_city_name");
        builder.HasIndex(s => new { s.UserId, s.ConditionMain })
            .HasDatabaseName("ix_searches_user_id_condition_main");

        builder.HasOne(s => s.User)
            .WithMany(u => u.Searches)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
