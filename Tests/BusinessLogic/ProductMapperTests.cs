using BusinessLogic.Mappers;
using DataAccess.Entities;

namespace Tests.BusinessLogic;

public class ProductMapperTests
{
    [Fact]
    public void ProductToResponse_CopiesTheFields()
    {
        var product = new CatalogProduct
        {
            Id = Guid.NewGuid(),
            Barcode = "123",
            Name = "Milk",
            ProductTypeId = Guid.NewGuid(),
            ImageUrl = "https://example.com/m.jpg",
            Source = "open_food_facts",
        };

        var response = product.ToResponse();

        Assert.Equal(product.Id, response.Id);
        Assert.Equal("123", response.Barcode);
        Assert.Equal("Milk", response.Name);
        Assert.Equal(product.ProductTypeId, response.ProductTypeId);
        Assert.Equal(product.ImageUrl, response.ImageUrl);
        Assert.Equal("open_food_facts", response.Source);
    }

    [Fact]
    public void TypeToResponse_CopiesTheFields()
    {
        var type = new ProductType
        {
            Id = Guid.NewGuid(),
            CategoryId = Guid.NewGuid(),
            Name = "Milk",
            DefaultExpiryDays = 7,
        };

        var response = type.ToResponse();

        Assert.Equal(type.Id, response.Id);
        Assert.Equal(type.CategoryId, response.CategoryId);
        Assert.Equal("Milk", response.Name);
        Assert.Equal(7, response.DefaultExpiryDays);
    }

    [Fact]
    public void CategoryToResponse_CopiesTheFields()
    {
        var category = new ProductCategory { Id = Guid.NewGuid(), Name = "Dairy" };

        var response = category.ToResponse();

        Assert.Equal(category.Id, response.Id);
        Assert.Equal("Dairy", response.Name);
    }
}
