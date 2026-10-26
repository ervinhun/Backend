using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

/// <summary>
/// The schema is owned by the Flyway migrations in db/migrations — this
/// context only maps onto it and must never be used to create or migrate it.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserDevice> UserDevices => Set<UserDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(u => u.Id);

            user.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            user.Property(u => u.Email).HasColumnName("email");
            user.Property(u => u.PasswordHash).HasColumnName("password_hash");
            user.Property(u => u.AuthProvider).HasColumnName("auth_provider").IsRequired();
            user.Property(u => u.DefaultDisplayName).HasColumnName("default_display_name").IsRequired();
            user.Property(u => u.Locale).HasColumnName("locale").IsRequired();
            user.Property(u => u.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_users_updated_at trigger.
            user.Property(u => u.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            user.Property(u => u.DeletedAt).HasColumnName("deleted_at");

            user.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<UserDevice>(device =>
        {
            device.ToTable("user_devices");
            device.HasKey(d => d.Id);

            device.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            device.Property(d => d.UserId).HasColumnName("user_id");
            device.Property(d => d.PushToken).HasColumnName("push_token");
            device.Property(d => d.Platform).HasColumnName("platform");
            // Defaults to true in C#; a HasDefaultValue(true) would make EF skip sending false.
            device.Property(d => d.IsActive).HasColumnName("is_active");
            device.Property(d => d.LastSeenAt).HasColumnName("last_seen_at");
            device.Property(d => d.RefreshTokenHash).HasColumnName("refresh_token_hash");
            device.Property(d => d.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at");
            device.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_user_devices_updated_at trigger.
            device.Property(d => d.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            device.Property(d => d.DeletedAt).HasColumnName("deleted_at");

            device.HasIndex(d => d.RefreshTokenHash).IsUnique();
        });
    }
}
