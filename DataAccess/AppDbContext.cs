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
    public DbSet<UserSession> UserSessions => Set<UserSession>();

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

        modelBuilder.Entity<UserSession>(session =>
        {
            session.ToTable("user_sessions");
            session.HasKey(d => d.Id);

            session.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            session.Property(d => d.UserId).HasColumnName("user_id");
            session.Property(d => d.PushToken).HasColumnName("push_token");
            session.Property(d => d.Platform).HasColumnName("platform");
            session.Property(d => d.PhoneModel).HasColumnName("phone_model");
            // Defaults to true in C#; a HasDefaultValue(true) would make EF skip sending false.
            session.Property(d => d.IsActive).HasColumnName("is_active");
            session.Property(d => d.LastSeenAt).HasColumnName("last_seen_at");
            session.Property(d => d.RefreshTokenHash).HasColumnName("refresh_token_hash");
            session.Property(d => d.RefreshTokenExpiresAt).HasColumnName("refresh_token_expires_at");
            session.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_user_sessions_updated_at trigger.
            session.Property(d => d.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            session.Property(d => d.DeletedAt).HasColumnName("deleted_at");

            session.HasIndex(d => d.RefreshTokenHash).IsUnique();
        });
    }
}
