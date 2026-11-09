using System.ComponentModel.DataAnnotations;
using BusinessLogic.Dtos.Product;

namespace Tests.BusinessLogic;

public class CreateProductRequestValidationTests
{
    private static bool IsValid(string? barcode = "5901234123457", string? name = "Milk", string? imageUrl = null)
    {
        var request = new CreateProductRequest
        {
            Barcode = barcode!,
            Name = name!,
            ImageUrl = imageUrl,
        };
        return Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            [],
            validateAllProperties: true
        );
    }

    [Fact]
    public void ValidRequest_IsValid() => Assert.True(IsValid());

    [Theory]
    [InlineData("1")]
    [InlineData("not-a-number")]
    public void Barcode_IsPlainText_AnyOfItIsValid(string barcode) =>
        Assert.True(IsValid(barcode: barcode));

    [Fact]
    public void Barcode_AtMaxLength_IsValid() => Assert.True(IsValid(barcode: new string('1', 64)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Barcode_MissingOrEmpty_IsInvalid(string? barcode) =>
        Assert.False(IsValid(barcode: barcode));

    [Fact]
    public void Barcode_OverMaxLength_IsInvalid() => Assert.False(IsValid(barcode: new string('1', 65)));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Name_MissingOrEmpty_IsInvalid(string? name) => Assert.False(IsValid(name: name));

    [Fact]
    public void Name_AtMaxLength_IsValid() => Assert.True(IsValid(name: new string('a', 200)));

    [Fact]
    public void Name_OverMaxLength_IsInvalid() => Assert.False(IsValid(name: new string('a', 201)));

    [Fact]
    public void ImageUrl_AValidUrl_IsValid() =>
        Assert.True(IsValid(imageUrl: "https://example.com/milk.jpg"));

    [Theory]
    [InlineData("milk")]
    [InlineData("ftp://example.com/m.jpg")]
    [InlineData(" https://example.com/m.jpg")]
    public void ImageUrl_NotAnHttpUrl_IsInvalid(string url) => Assert.False(IsValid(imageUrl: url));

    [Fact]
    public void ImageUrl_Empty_IsValid() => Assert.True(IsValid(imageUrl: ""));

    [Fact]
    public void ImageUrl_OverMaxLength_IsInvalid() =>
        Assert.False(IsValid(imageUrl: "https://example.com/" + new string('a', 2000)));
}
