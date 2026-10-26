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
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(u => u.Id);

            user.Property(u => u.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            user.Property(u => u.Email).HasColumnName("email").IsRequired();
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

        modelBuilder.Entity<RevokedToken>(token =>
        {
            token.ToTable("revoked_tokens");
            token.HasKey(t => t.Jti);

            // The id comes from the token, not the database.
            token.Property(t => t.Jti).HasColumnName("jti").ValueGeneratedNever();
            token.Property(t => t.UserId).HasColumnName("user_id");
            token.Property(t => t.ExpiresAt).HasColumnName("expires_at");
            token.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.ToTable("refresh_tokens");
            token.HasKey(t => t.Id);

            token.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            token.Property(t => t.UserId).HasColumnName("user_id");
            token.Property(t => t.FamilyId).HasColumnName("family_id");
            token.Property(t => t.TokenHash).HasColumnName("token_hash").IsRequired();
            token.Property(t => t.ExpiresAt).HasColumnName("expires_at");
            token.Property(t => t.RevokedAt).HasColumnName("revoked_at");
            token.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_refresh_tokens_updated_at trigger.
            token.Property(t => t.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();

            token.HasIndex(t => t.TokenHash).IsUnique();
        });
    }
}
