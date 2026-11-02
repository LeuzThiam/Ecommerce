using Catalog.Domain;
using Catalog.Domain.Common;

namespace Catalog.Domain.Tests;

public class CategoryTests
{
    [Fact]
    public void Create_WithValidName_CreatesCategory()
    {
        var category = Category.Create("Électronique");

        Assert.Equal("Électronique", category.Name);
        Assert.NotEqual(Guid.Empty, category.Id);
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Category.Create(""));
    }
}
