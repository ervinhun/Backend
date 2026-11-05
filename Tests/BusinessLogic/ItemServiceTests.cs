using BusinessLogic.Dtos.Item;
using BusinessLogic.Exceptions;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Tests.Database;

namespace Tests.BusinessLogic;

/// <summary>ItemService against the real repositories and Postgres.</summary>
public class ItemServiceTests(PostgresFixture database) : DatabaseTest(database)
{
    private static readonly DateTimeOffset Now = new(2030, 6, 15, 23, 30, 0, TimeSpan.Zero);

    private ItemService Service =>
        new(
            new ItemRepository(Database.CreateDbContext()),
            new ContainerRepository(Database.CreateDbContext()),
            new FridgeRepository(Database.CreateDbContext(), new FakeTimeProvider(Now)),
            new ProductCatalogRepository(Database.CreateDbContext(), TimeProvider.System),
            new FakeTimeProvider(Now)
        );

    private record Setup(Fridge Fridge, Container Container, User Owner);

    private async Task<Setup> AddFridgeAsync(string code = "CODE0001")
    {
        await using var db = Database.CreateDbContext();
        var user = new User
        {
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            DefaultDisplayName = "Anna",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var fridge = new Fridge
        {
            Name = "Home",
            InviteCode = code,
            CreatedByUserId = user.Id,
        };
        db.Fridges.Add(fridge);
        await db.SaveChangesAsync();
        var container = new Container
        {
            FridgeId = fridge.Id,
            Name = "Top shelf",
            ContainerType = ContainerType.Fridge,
        };
        db.Containers.Add(container);
        db.FridgeMembers.Add(
            new FridgeMember
            {
                FridgeId = fridge.Id,
                UserId = user.Id,
                IsOwner = true,
            }
        );
        await db.SaveChangesAsync();
        return new Setup(fridge, container, user);
    }

    private async Task<User> AddMemberAsync(Fridge fridge, bool canPut, bool canUse)
    {
        await using var db = Database.CreateDbContext();
        var user = new User
        {
            Email = $"{Guid.NewGuid():N}@example.com",
            PasswordHash = "hash",
            DefaultDisplayName = "Ben",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        db.FridgeMembers.Add(
            new FridgeMember
            {
                FridgeId = fridge.Id,
                UserId = user.Id,
                CanPut = canPut,
                CanUse = canUse,
            }
        );
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<(
        CatalogProduct Product,
        ProductType Type,
        ProductCategory Category
    )> AddProductAsync(bool withType = true)
    {
        await using var db = Database.CreateDbContext();
        // The categories and types are seeded reference data that the tests share, so they are only read here.
        var type = await db.ProductTypes.OrderBy(t => t.Name).FirstAsync(t => t.CategoryId != null);
        var category = await db.ProductCategories.FirstAsync(c => c.Id == type.CategoryId);
        var product = new CatalogProduct
        {
            Barcode = Guid.NewGuid().ToString("N"),
            Name = "Whole milk",
            ImageUrl = "https://example.com/milk.png",
            ProductTypeId = withType ? type.Id : null,
        };
        db.CatalogProducts.Add(product);
        await db.SaveChangesAsync();
        return (product, type, category);
    }

    private static CreateItemRequest Create(Setup s, Action<CreateItemRequest>? change = null)
    {
        var request = new CreateItemRequest
        {
            ContainerId = s.Container.Id,
            Name = "Butter",
            ExpiryDate = new DateOnly(2030, 7, 1),
        };
        change?.Invoke(request);
        return request;
    }

    private static UpdateItemRequest Update(Setup s, Action<UpdateItemRequest>? change = null)
    {
        var request = new UpdateItemRequest
        {
            ContainerId = s.Container.Id,
            Name = "Salted butter",
            ExpiryDate = new DateOnly(2030, 8, 1),
        };
        change?.Invoke(request);
        return request;
    }

    [Fact]
    public async Task Create_TrimsTheTextAndRecordsWhoAddedItAndWhen()
    {
        var s = await AddFridgeAsync();

        var item = await Service.CreateAsync(
            s.Fridge.Id,
            s.Owner.Id,
            Create(
                s,
                r =>
                {
                    r.Name = "  Butter ";
                    r.Amount = " 250 ";
                    r.Unit = " g ";
                    r.Comment = " Salted ";
                    r.ImageUrl = " https://example.com/b.png ";
                }
            )
        );

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(
            ("Butter", "250", "g", "Salted"),
            (item.Name, item.Amount, item.Unit, item.Comment)
        );
        Assert.Equal("https://example.com/b.png", item.ImageUrl);
        Assert.Equal(s.Container.Id, item.ContainerId);
        Assert.Equal(s.Owner.Id, item.AddedByUserId);
        Assert.Equal(new DateOnly(2030, 6, 15), item.AddedAt);
        Assert.Equal(new DateOnly(2030, 7, 1), item.ExpiryDate);
        Assert.Equal("active", item.Status);
        Assert.Null(item.ProductCatalogId);
        Assert.NotEqual(default, item.CreatedAt);
        Assert.Equal(item.Id, (await Service.GetAsync(s.Fridge.Id, item.Id, s.Owner.Id)).Id);
    }

    [Fact]
    public async Task Create_TurnsBlankOptionalTextIntoNone()
    {
        var s = await AddFridgeAsync();

        var item = await Service.CreateAsync(
            s.Fridge.Id,
            s.Owner.Id,
            Create(
                s,
                r =>
                {
                    r.Amount = " ";
                    r.Unit = "";
                    r.Comment = "  ";
                    r.ImageUrl = "";
                }
            )
        );

        Assert.Null(item.Amount);
        Assert.Null(item.Unit);
        Assert.Null(item.Comment);
        Assert.Null(item.ImageUrl);
    }

    [Fact]
    public async Task Create_FromAProduct_FillsInWhatIsLeftOut()
    {
        var s = await AddFridgeAsync();
        var (product, type, category) = await AddProductAsync();

        var item = await Service.CreateAsync(
            s.Fridge.Id,
            s.Owner.Id,
            new CreateItemRequest { ContainerId = s.Container.Id, ProductCatalogId = product.Id }
        );

        Assert.Equal(product.Id, item.ProductCatalogId);
        Assert.Equal("Whole milk", item.Name);
        Assert.Equal("https://example.com/milk.png", item.ImageUrl);
        Assert.Equal(category.Id, item.CategoryId);
        // Counted from the day it was added, which is the UTC date even late in the evening.
        Assert.Equal(new DateOnly(2030, 6, 15).AddDays(type.DefaultExpiryDays), item.ExpiryDate);
    }

    [Fact]
    public async Task Create_FromAProduct_KeepsWhatTheRequestGives()
    {
        var s = await AddFridgeAsync();
        var (product, _, category) = await AddProductAsync();
        await using var db = Database.CreateDbContext();
        var otherCategory = await db.ProductCategories.FirstAsync(c => c.Id != category.Id);

        var item = await Service.CreateAsync(
            s.Fridge.Id,
            s.Owner.Id,
            new CreateItemRequest
            {
                ContainerId = s.Container.Id,
                ProductCatalogId = product.Id,
                Name = "My milk",
                CategoryId = otherCategory.Id,
                ImageUrl = "https://example.com/mine.png",
                ExpiryDate = new DateOnly(2030, 6, 20),
            }
        );

        Assert.Equal("My milk", item.Name);
        Assert.Equal(otherCategory.Id, item.CategoryId);
        Assert.Equal("https://example.com/mine.png", item.ImageUrl);
        Assert.Equal(new DateOnly(2030, 6, 20), item.ExpiryDate);
    }

    [Fact]
    public async Task Create_WithoutNameOrProduct_ThrowsBadRequest()
    {
        var s = await AddFridgeAsync();

        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            Service.CreateAsync(s.Fridge.Id, s.Owner.Id, Create(s, r => r.Name = "  "))
        );

        Assert.Equal("A name is required when no product is given.", error.Message);
    }

    [Fact]
    public async Task Create_WithoutExpiryDateOrDefaultShelfLife_ThrowsBadRequest()
    {
        var s = await AddFridgeAsync();
        var (untyped, _, _) = await AddProductAsync(withType: false);

        var plain = await Assert.ThrowsAsync<BadRequestException>(() =>
            Service.CreateAsync(s.Fridge.Id, s.Owner.Id, Create(s, r => r.ExpiryDate = null))
        );
        var fromProduct = await Assert.ThrowsAsync<BadRequestException>(() =>
            Service.CreateAsync(
                s.Fridge.Id,
                s.Owner.Id,
                Create(
                    s,
                    r =>
                    {
                        r.ExpiryDate = null;
                        r.ProductCatalogId = untyped.Id;
                    }
                )
            )
        );

        Assert.Equal(
            "An expiry date is required when the product has no default shelf life.",
            plain.Message
        );
        Assert.Equal(plain.Message, fromProduct.Message);
    }

    [Fact]
    public async Task Create_WithUnknownReferences_ThrowsNotFound()
    {
        var s = await AddFridgeAsync();
        var other = await AddFridgeAsync("CODE0002");

        var container = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.CreateAsync(
                s.Fridge.Id,
                s.Owner.Id,
                Create(s, r => r.ContainerId = other.Container.Id)
            )
        );
        var product = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.CreateAsync(
                s.Fridge.Id,
                s.Owner.Id,
                Create(s, r => r.ProductCatalogId = Guid.NewGuid())
            )
        );
        var category = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.CreateAsync(
                s.Fridge.Id,
                s.Owner.Id,
                Create(s, r => r.CategoryId = Guid.NewGuid())
            )
        );

        Assert.Equal("Container not found.", container.Message);
        Assert.Equal("Product not found.", product.Message);
        Assert.Equal("Category not found.", category.Message);
        Assert.Empty(await Service.GetForFridgeAsync(s.Fridge.Id, s.Owner.Id, null, null));
    }

    [Fact]
    public async Task GetForFridge_ListsActiveItemsSoonestToExpireFirst_AndFiltersByContainerAndStatus()
    {
        var s = await AddFridgeAsync();
        Guid drawerId;
        await using (var db = Database.CreateDbContext())
        {
            var drawer = new Container
            {
                FridgeId = s.Fridge.Id,
                Name = "Drawer",
                ContainerType = ContainerType.Freezer,
            };
            db.Containers.Add(drawer);
            await db.SaveChangesAsync();
            drawerId = drawer.Id;
        }
        await Service.CreateAsync(
            s.Fridge.Id,
            s.Owner.Id,
            Create(
                s,
                r =>
                {
                    r.Name = "Late";
                    r.ExpiryDate = new DateOnly(2030, 9, 1);
                }
            )
        );
        var soon = await Service.CreateAsync(
            s.Fridge.Id,
            s.Owner.Id,
            Create(
                s,
                r =>
                {
                    r.Name = "Soon";
                    r.ExpiryDate = new DateOnly(2030, 7, 1);
                }
            )
        );
        await Service.CreateAsync(
            s.Fridge.Id,
            s.Owner.Id,
            Create(
                s,
                r =>
                {
                    r.Name = "Frozen";
                    r.ContainerId = drawerId;
                }
            )
        );
        await Service.SetStatusAsync(
            s.Fridge.Id,
            soon.Id,
            s.Owner.Id,
            new SetItemStatusRequest { Status = "Consumed" }
        );

        var active = await Service.GetForFridgeAsync(s.Fridge.Id, s.Owner.Id, null, null);
        var inDrawer = await Service.GetForFridgeAsync(s.Fridge.Id, s.Owner.Id, drawerId, "active");
        var consumed = await Service.GetForFridgeAsync(s.Fridge.Id, s.Owner.Id, null, "CONSUMED");

        Assert.Equal(["Frozen", "Late"], active.Select(i => i.Name));
        Assert.Equal(["Frozen"], inDrawer.Select(i => i.Name));
        Assert.Equal(["Soon"], consumed.Select(i => i.Name));
    }

    [Fact]
    public async Task GetForFridge_WithAnUnknownStatus_ThrowsBadRequest()
    {
        var s = await AddFridgeAsync();

        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            Service.GetForFridgeAsync(s.Fridge.Id, s.Owner.Id, null, "spoiled")
        );

        Assert.Equal("The status must be active, consumed, expired or removed.", error.Message);
    }

    [Fact]
    public async Task Update_ReplacesTheDetails_AndCanMoveTheItem()
    {
        var s = await AddFridgeAsync();
        var item = await Service.CreateAsync(s.Fridge.Id, s.Owner.Id, Create(s));
        Guid drawerId;
        await using (var db = Database.CreateDbContext())
        {
            var drawer = new Container
            {
                FridgeId = s.Fridge.Id,
                Name = "Drawer",
                ContainerType = ContainerType.Pantry,
            };
            db.Containers.Add(drawer);
            await db.SaveChangesAsync();
            drawerId = drawer.Id;
        }

        var updated = await Service.UpdateAsync(
            s.Fridge.Id,
            item.Id,
            s.Owner.Id,
            Update(
                s,
                r =>
                {
                    r.Name = "  Salted butter ";
                    r.ContainerId = drawerId;
                    r.Amount = " 2 ";
                    r.Comment = " ";
                }
            )
        );

        Assert.Equal("Salted butter", updated.Name);
        Assert.Equal(drawerId, updated.ContainerId);
        Assert.Equal("2", updated.Amount);
        Assert.Null(updated.Comment);
        Assert.Equal(new DateOnly(2030, 8, 1), updated.ExpiryDate);
        Assert.Equal(item.AddedAt, updated.AddedAt);
        Assert.Equal(item.AddedByUserId, updated.AddedByUserId);
    }

    [Fact]
    public async Task Update_WithUnknownThings_ThrowsNotFound()
    {
        var s = await AddFridgeAsync();
        var other = await AddFridgeAsync("CODE0002");
        var item = await Service.CreateAsync(s.Fridge.Id, s.Owner.Id, Create(s));

        var missing = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.UpdateAsync(s.Fridge.Id, Guid.NewGuid(), s.Owner.Id, Update(s))
        );
        var container = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.UpdateAsync(
                s.Fridge.Id,
                item.Id,
                s.Owner.Id,
                Update(s, r => r.ContainerId = other.Container.Id)
            )
        );
        var category = await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.UpdateAsync(
                s.Fridge.Id,
                item.Id,
                s.Owner.Id,
                Update(s, r => r.CategoryId = Guid.NewGuid())
            )
        );

        Assert.Equal("Item not found.", missing.Message);
        Assert.Equal("Container not found.", container.Message);
        Assert.Equal("Category not found.", category.Message);
    }

    [Fact]
    public async Task SetStatus_MarksTheItem_InAnyCase()
    {
        var s = await AddFridgeAsync();
        var item = await Service.CreateAsync(s.Fridge.Id, s.Owner.Id, Create(s));

        var removed = await Service.SetStatusAsync(
            s.Fridge.Id,
            item.Id,
            s.Owner.Id,
            new SetItemStatusRequest { Status = "REMOVED" }
        );
        var active = await Service.SetStatusAsync(
            s.Fridge.Id,
            item.Id,
            s.Owner.Id,
            new SetItemStatusRequest { Status = "active" }
        );

        Assert.Equal("removed", removed.Status);
        Assert.Equal("active", active.Status);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.SetStatusAsync(
                s.Fridge.Id,
                Guid.NewGuid(),
                s.Owner.Id,
                new SetItemStatusRequest { Status = "consumed" }
            )
        );
    }

    [Fact]
    public async Task Delete_HidesTheItem_AndCannotBeRepeated()
    {
        var s = await AddFridgeAsync();
        var item = await Service.CreateAsync(s.Fridge.Id, s.Owner.Id, Create(s));

        await Service.DeleteAsync(s.Fridge.Id, item.Id, s.Owner.Id);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.GetAsync(s.Fridge.Id, item.Id, s.Owner.Id)
        );
        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service.DeleteAsync(s.Fridge.Id, item.Id, s.Owner.Id)
        );
        Assert.Empty(await Service.GetForFridgeAsync(s.Fridge.Id, s.Owner.Id, null, null));
    }

    [Fact]
    public async Task NonMembers_GetNotFound_ForEveryAction()
    {
        var s = await AddFridgeAsync();
        var stranger = (await AddFridgeAsync("CODE0002")).Owner;
        var item = await Service.CreateAsync(s.Fridge.Id, s.Owner.Id, Create(s));
        var status = new SetItemStatusRequest { Status = "consumed" };

        var calls = new Func<Task>[]
        {
            () => Service.CreateAsync(s.Fridge.Id, stranger.Id, Create(s)),
            () => Service.GetForFridgeAsync(s.Fridge.Id, stranger.Id, null, null),
            () => Service.GetAsync(s.Fridge.Id, item.Id, stranger.Id),
            () => Service.UpdateAsync(s.Fridge.Id, item.Id, stranger.Id, Update(s)),
            () => Service.SetStatusAsync(s.Fridge.Id, item.Id, stranger.Id, status),
            () => Service.DeleteAsync(s.Fridge.Id, item.Id, stranger.Id),
        };

        foreach (var call in calls)
            Assert.Equal(
                "Fridge not found.",
                (await Assert.ThrowsAsync<NotFoundException>(call)).Message
            );
    }

    [Fact]
    public async Task MemberWhoMayOnlyPut_AddsAndChangesButCannotUse()
    {
        var s = await AddFridgeAsync();
        var ben = await AddMemberAsync(s.Fridge, canPut: true, canUse: false);

        var item = await Service.CreateAsync(s.Fridge.Id, ben.Id, Create(s));
        await Service.UpdateAsync(s.Fridge.Id, item.Id, ben.Id, Update(s));
        var error = await Assert.ThrowsAsync<ForbiddenException>(() =>
            Service.SetStatusAsync(
                s.Fridge.Id,
                item.Id,
                ben.Id,
                new SetItemStatusRequest { Status = "consumed" }
            )
        );
        await Service.DeleteAsync(s.Fridge.Id, item.Id, ben.Id);

        Assert.Equal("You are not allowed to use this fridge's items.", error.Message);
    }

    [Fact]
    public async Task MemberWhoMayOnlyUse_SeesAndConsumesButCannotChange()
    {
        var s = await AddFridgeAsync();
        var ben = await AddMemberAsync(s.Fridge, canPut: false, canUse: true);
        var item = await Service.CreateAsync(s.Fridge.Id, s.Owner.Id, Create(s));

        var consumed = await Service.SetStatusAsync(
            s.Fridge.Id,
            item.Id,
            ben.Id,
            new SetItemStatusRequest { Status = "consumed" }
        );
        Assert.Equal("consumed", consumed.Status);
        Assert.Equal(item.Id, (await Service.GetAsync(s.Fridge.Id, item.Id, ben.Id)).Id);
        Assert.Single(await Service.GetForFridgeAsync(s.Fridge.Id, ben.Id, null, "consumed"));

        var calls = new Func<Task>[]
        {
            () => Service.CreateAsync(s.Fridge.Id, ben.Id, Create(s)),
            () => Service.UpdateAsync(s.Fridge.Id, item.Id, ben.Id, Update(s)),
            () => Service.DeleteAsync(s.Fridge.Id, item.Id, ben.Id),
        };
        foreach (var call in calls)
            Assert.Equal(
                "You are not allowed to change this fridge's items.",
                (await Assert.ThrowsAsync<ForbiddenException>(call)).Message
            );
    }

    [Fact]
    public async Task MemberWithoutAnyRights_OnlySees()
    {
        var s = await AddFridgeAsync();
        var ben = await AddMemberAsync(s.Fridge, canPut: false, canUse: false);
        var item = await Service.CreateAsync(s.Fridge.Id, s.Owner.Id, Create(s));

        Assert.Single(await Service.GetForFridgeAsync(s.Fridge.Id, ben.Id, null, null));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Service.SetStatusAsync(
                s.Fridge.Id,
                item.Id,
                ben.Id,
                new SetItemStatusRequest { Status = "consumed" }
            )
        );
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Service.DeleteAsync(s.Fridge.Id, item.Id, ben.Id)
        );
    }

    [Fact]
    public async Task Owner_MayAlwaysChangeAndUse_EvenWithTheMemberRightsOff()
    {
        var s = await AddFridgeAsync();
        await using (var db = Database.CreateDbContext())
            await db.FridgeMembers.ExecuteUpdateAsync(m =>
                m.SetProperty(x => x.CanPut, false).SetProperty(x => x.CanUse, false)
            );

        var item = await Service.CreateAsync(s.Fridge.Id, s.Owner.Id, Create(s));
        var consumed = await Service.SetStatusAsync(
            s.Fridge.Id,
            item.Id,
            s.Owner.Id,
            new SetItemStatusRequest { Status = "consumed" }
        );
        await Service.DeleteAsync(s.Fridge.Id, item.Id, s.Owner.Id);

        Assert.Equal("consumed", consumed.Status);
    }
}
