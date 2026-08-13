using BoardGameLibrary.Domain.Categories;

namespace BoardGameLibrary.UnitTests.Categories;

public sealed class CategoryTests
{
    [Fact]
    public void Create_NormalizesNameAndCreatesActiveVersionSevenIdentifier()
    {
        Category category = Category.Create("  Science Fiction  ");

        Assert.Equal("Science Fiction", category.Name);
        Assert.Equal("SCIENCE FICTION", category.NormalizedName);
        Assert.True(category.IsActive);
        DomainTestAssertions.VersionIsSeven(category.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenNameIsMissing_Throws(string? name)
    {
        DomainTestAssertions.Throws("category.name.required", () => Category.Create(name!));
    }

    [Fact]
    public void Create_WhenNameExceedsLimit_Throws()
    {
        string name = new('a', Category.NameMaximumLength + 1);

        DomainTestAssertions.Throws("category.name.too_long", () => Category.Create(name));
    }

    [Fact]
    public void Update_NormalizesAndReplacesName()
    {
        Category category = Category.Create("Strategy");

        category.Update("  Family  ");

        Assert.Equal("Family", category.Name);
        Assert.Equal("FAMILY", category.NormalizedName);
    }

    [Fact]
    public void SetActive_ChangesActiveStateIdempotently()
    {
        Category category = Category.Create("Strategy");

        category.SetActive(false);
        category.SetActive(false);

        Assert.False(category.IsActive);

        category.SetActive(true);

        Assert.True(category.IsActive);
    }
}
