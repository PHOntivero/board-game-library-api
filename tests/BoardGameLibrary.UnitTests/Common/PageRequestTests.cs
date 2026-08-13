using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Loans;

namespace BoardGameLibrary.UnitTests.Common;

public sealed class PageRequestTests
{
    private static readonly string[] AllowedSortFields = ["title", "publicationYear"];

    [Fact]
    public void Create_WithOmittedValues_UsesDefaults()
    {
        Result<PageRequest> result = PageRequest.Create(
            null,
            null,
            null,
            null,
            AllowedSortFields,
            "title",
            SortDirection.Ascending);

        Assert.True(result.IsSuccess);
        Assert.Equal(PageRequest.DefaultPage, result.Value.Page);
        Assert.Equal(PageRequest.DefaultPageSize, result.Value.PageSize);
        Assert.Equal("title", result.Value.SortBy);
        Assert.Equal(SortDirection.Ascending, result.Value.SortDirection);
        Assert.Equal(0, result.Value.Offset);
    }

    [Fact]
    public void Create_WithValidValues_CanonicalizesSortAndCalculatesOffset()
    {
        Result<PageRequest> result = PageRequest.Create(
            3,
            25,
            "PUBLICATIONYEAR",
            "DESC",
            AllowedSortFields,
            "title",
            SortDirection.Ascending);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Page);
        Assert.Equal(25, result.Value.PageSize);
        Assert.Equal("publicationYear", result.Value.SortBy);
        Assert.Equal(SortDirection.Descending, result.Value.SortDirection);
        Assert.Equal(50, result.Value.Offset);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(2147483647, 100)]
    public void Create_WithInvalidPagination_ReturnsValidationFailure(int page, int pageSize)
    {
        Result<PageRequest> result = PageRequest.Create(
            page,
            pageSize,
            null,
            null,
            AllowedSortFields,
            "title",
            SortDirection.Ascending);

        Error error = Assert.Single(result.Errors);
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Common.ValidationFailed, error.Code);
        Assert.Equal(ErrorType.Validation, error.Type);
    }

    [Theory]
    [InlineData("unknown", "asc")]
    [InlineData("title", "ascending")]
    public void Create_WithInvalidSort_ReturnsValidationFailure(string sortBy, string direction)
    {
        Result<PageRequest> result = PageRequest.Create(
            1,
            20,
            sortBy,
            direction,
            AllowedSortFields,
            "title",
            SortDirection.Ascending);

        Assert.True(result.IsFailure);
        Assert.All(result.Errors, error =>
        {
            Assert.Equal(ErrorCodes.Common.ValidationFailed, error.Code);
            Assert.Equal(ErrorType.Validation, error.Type);
        });
    }

    [Fact]
    public void Create_WithInvalidSortConfiguration_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PageRequest.Create(
            null,
            null,
            null,
            null,
            ["title"],
            "publisher",
            SortDirection.Ascending));
    }

    [Fact]
    public void Create_WithOmittedDirection_UsesProvidedDescendingDefault()
    {
        Result<PageRequest> result = PageRequest.Create(
            null,
            null,
            null,
            null,
            LoanSortFields.Allowed,
            LoanSortFields.Default,
            LoanSortFields.DefaultDirection);

        Assert.True(result.IsSuccess);
        Assert.Equal(LoanSortFields.Default, result.Value.SortBy);
        Assert.Equal(SortDirection.Descending, result.Value.SortDirection);
    }

    [Fact]
    public void Create_WithInvalidDefaultDirection_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PageRequest.Create(
            null,
            null,
            null,
            null,
            AllowedSortFields,
            "title",
            (SortDirection)99));
    }
}
