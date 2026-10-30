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
    public DbSet<Fridge> Fridges => Set<Fridge>();
    public DbSet<FridgeMember> FridgeMembers => Set<FridgeMember>();
    public DbSet<Container> Containers => Set<Container>();

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

        modelBuilder.Entity<Fridge>(fridge =>
        {
            fridge.ToTable("fridges");
            fridge.HasKey(f => f.Id);
            // Soft-deleted fridges are left out of every query; IgnoreQueryFilters() brings them back.
            fridge.HasQueryFilter(f => f.DeletedAt == null);

            fridge.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            fridge.Property(f => f.Name).HasColumnName("name").IsRequired();
            fridge.Property(f => f.InviteCode).HasColumnName("invite_code").IsRequired();
            fridge.Property(f => f.MaxMembers).HasColumnName("max_members");
            fridge.Property(f => f.MaxContainers).HasColumnName("max_containers");
            fridge.Property(f => f.CreatedByUserId).HasColumnName("created_by_user_id");
            fridge.Property(f => f.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_fridges_updated_at trigger.
            fridge.Property(f => f.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            fridge.Property(f => f.DeletedAt).HasColumnName("deleted_at");

            fridge.HasIndex(f => f.InviteCode).IsUnique();
        });

        modelBuilder.Entity<FridgeMember>(member =>
        {
            member.ToTable("fridge_members");
            member.HasKey(m => m.Id);
            // Same for members who left or were removed.
            member.HasQueryFilter(m => m.DeletedAt == null);

            member.Property(m => m.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            member.Property(m => m.FridgeId).HasColumnName("fridge_id");
            member.Property(m => m.UserId).HasColumnName("user_id");
            member.Property(m => m.DisplayName).HasColumnName("display_name");
            member.Property(m => m.IsOwner).HasColumnName("is_owner");
            member.Property(m => m.CanPut).HasColumnName("can_put");
            member.Property(m => m.CanUse).HasColumnName("can_use");
            member.Property(m => m.AccessExpiresAt).HasColumnName("access_expires_at");
            member.Property(m => m.InvitedByUserId).HasColumnName("invited_by_user_id");
            member.Property(m => m.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_fridge_members_updated_at trigger.
            member.Property(m => m.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            member.Property(m => m.DeletedAt).HasColumnName("deleted_at");

            member.HasOne(m => m.Fridge).WithMany().HasForeignKey(m => m.FridgeId);
            member.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId);
        });

        modelBuilder.Entity<Container>(container =>
        {
            container.ToTable("containers");
            container.HasKey(c => c.Id);
            // Soft-deleted containers are left out of every query, like deleted fridges and members.
            container.HasQueryFilter(c => c.DeletedAt == null);

            container.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            container.Property(c => c.FridgeId).HasColumnName("fridge_id");
            container.Property(c => c.Name).HasColumnName("name").IsRequired();
            // The column only accepts 'fridge', 'freezer' and 'pantry'.
            container.Property(c => c.ContainerType).HasColumnName("container_type")
                .HasConversion(t => t.ToString().ToLowerInvariant(), s => Enum.Parse<ContainerType>(s, true));
            container.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_containers_updated_at trigger.
            container.Property(c => c.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            container.Property(c => c.DeletedAt).HasColumnName("deleted_at");
        });
    }
}
