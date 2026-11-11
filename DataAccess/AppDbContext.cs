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
    public DbSet<UserLogin> UserLogins => Set<UserLogin>();
    public DbSet<Fridge> Fridges => Set<Fridge>();
    public DbSet<FridgeMember> FridgeMembers => Set<FridgeMember>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<CatalogProduct> CatalogProducts => Set<CatalogProduct>();

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
            user.HasMany(u => u.Logins).WithOne().HasForeignKey(l => l.UserId);
        });

        modelBuilder.Entity<UserLogin>(login =>
        {
            login.ToTable("user_logins");
            login.HasKey(l => l.Id);

            login.Property(l => l.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            login.Property(l => l.UserId).HasColumnName("user_id");
            login.Property(l => l.ProviderUserId).HasColumnName("provider_user_id").IsRequired();
            login.Property(l => l.Provider).HasColumnName("provider").IsRequired();
            login.Property(l => l.IsActive).HasColumnName("is_active");
            login.Property(l => l.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_user_logins_updated_at trigger.
            login.Property(l => l.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            login.Property(l => l.DeletedAt).HasColumnName("deleted_at");
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

        modelBuilder.Entity<ProductCategory>(category =>
        {
            category.ToTable("product_categories");
            category.HasKey(c => c.Id);
            category.HasQueryFilter(c => c.DeletedAt == null);

            category.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            category.Property(c => c.Name).HasColumnName("name").IsRequired();
            category.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_product_categories_updated_at trigger.
            category.Property(c => c.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            category.Property(c => c.DeletedAt).HasColumnName("deleted_at");

            category.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<ProductType>(type =>
        {
            type.ToTable("product_types");
            type.HasKey(t => t.Id);
            type.HasQueryFilter(t => t.DeletedAt == null);

            type.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            type.Property(t => t.CategoryId).HasColumnName("category_id");
            type.Property(t => t.Name).HasColumnName("name").IsRequired();
            type.Property(t => t.DefaultExpiryDays).HasColumnName("default_expiry_days");
            type.Property(t => t.PantryExpiryDays).HasColumnName("pantry_expiry_days");
            type.Property(t => t.FridgeExpiryDays).HasColumnName("fridge_expiry_days");
            type.Property(t => t.FreezerExpiryDays).HasColumnName("freezer_expiry_days");
            type.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_product_types_updated_at trigger.
            type.Property(t => t.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            type.Property(t => t.DeletedAt).HasColumnName("deleted_at");

            type.HasOne(t => t.Category).WithMany(c => c.ProductTypes).HasForeignKey(t => t.CategoryId);
        });

        modelBuilder.Entity<CatalogProduct>(product =>
        {
            product.ToTable("product_catalog");
            product.HasKey(p => p.Id);
            product.HasQueryFilter(p => p.DeletedAt == null);

            product.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            product.Property(p => p.Barcode).HasColumnName("barcode").IsRequired();
            product.Property(p => p.ProductTypeId).HasColumnName("product_type_id");
            product.Property(p => p.Name).HasColumnName("name").IsRequired();
            product.Property(p => p.ImageUrl).HasColumnName("image_url");
            product.Property(p => p.Source).HasColumnName("source").IsRequired();
            product.Property(p => p.CachedAt).HasColumnName("cached_at").HasDefaultValueSql("now()");
            product.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            // Bumped server-side by the trg_product_catalog_updated_at trigger.
            product.Property(p => p.UpdatedAt).HasColumnName("updated_at")
                .HasDefaultValueSql("now()").ValueGeneratedOnAddOrUpdate();
            product.Property(p => p.DeletedAt).HasColumnName("deleted_at");

            product.HasOne<ProductType>().WithMany().HasForeignKey(p => p.ProductTypeId);
            product.HasIndex(p => p.Barcode).IsUnique();
        });
    }
}
