using System.ComponentModel.DataAnnotations;
using BusinessLogic.Dtos.Item;

namespace Tests.BusinessLogic;

public class ItemRequestValidationTests
{
    private static List<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, true);
        return results;
    }

    private static CreateItemRequest Create(Action<CreateItemRequest>? change = null)
    {
        var request = new CreateItemRequest { ContainerId = Guid.NewGuid(), Name = "Milk" };
        change?.Invoke(request);
        return request;
    }

    private static UpdateItemRequest Update(Action<UpdateItemRequest>? change = null)
    {
        var request = new UpdateItemRequest
        {
            ContainerId = Guid.NewGuid(),
            Name = "Milk",
            ExpiryDate = new DateOnly(2030, 1, 1),
        };
        change?.Invoke(request);
        return request;
    }

    [Fact]
    public void ValidRequests_PassValidation()
    {
        Assert.Empty(Validate(Create()));
        Assert.Empty(Validate(Create(r => r.Name = null)));
        Assert.Empty(Validate(Update()));
        Assert.Empty(Validate(new SetItemStatusRequest { Status = "consumed" }));
    }

    [Fact]
    public void CreateWithoutAContainer_FailsValidation() =>
        Assert.NotEmpty(Validate(Create(r => r.ContainerId = null)));

    [Fact]
    public void UpdateWithoutAContainerNameOrExpiryDate_FailsValidation()
    {
        Assert.NotEmpty(Validate(Update(r => r.ContainerId = null)));
        Assert.NotEmpty(Validate(Update(r => r.Name = "")));
        Assert.NotEmpty(Validate(Update(r => r.ExpiryDate = null)));
    }

    [Theory]
    [InlineData("")]
    public void AnEmptyName_FailsValidation(string name) =>
        Assert.NotEmpty(Validate(Create(r => r.Name = name)));

    [Fact]
    public void TooLongText_FailsValidation()
    {
        Assert.NotEmpty(Validate(Create(r => r.Name = new string('a', 201))));
        Assert.NotEmpty(Validate(Create(r => r.Amount = new string('1', 51))));
        Assert.NotEmpty(Validate(Create(r => r.Unit = new string('g', 51))));
        Assert.NotEmpty(Validate(Create(r => r.Comment = new string('c', 1001))));
        Assert.NotEmpty(Validate(Update(r => r.Comment = new string('c', 1001))));
    }

    [Theory]
    [InlineData("https://example.com/milk.png", true)]
    [InlineData("http://example.com/milk.png", true)]
    [InlineData("ftp://example.com/milk.png", false)]
    [InlineData("not a url", false)]
    public void ImageUrl_MustBeHttpOrHttps(string url, bool valid)
    {
        Assert.Equal(valid, Validate(Create(r => r.ImageUrl = url)).Count == 0);
        Assert.Equal(valid, Validate(Update(r => r.ImageUrl = url)).Count == 0);
    }

    [Theory]
    [InlineData("active", true)]
    [InlineData("consumed", true)]
    [InlineData("removed", true)]
    [InlineData("expired", false)]
    [InlineData("", false)]
    public void Status_MustBeOneTheUserMaySet(string status, bool valid) =>
        Assert.Equal(valid, Validate(new SetItemStatusRequest { Status = status }).Count == 0);
}
