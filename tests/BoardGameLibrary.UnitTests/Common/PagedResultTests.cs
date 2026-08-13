using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.UnitTests.Common;

public sealed class PagedResultTests
{
    [Fact]
    public void Create_CalculatesTotalPagesAndCopiesMetadata()
    {
        PageRequest request = CreatePageRequest(page: 2, pageSize: 20);

        PagedResult<int> result = PagedResult<int>.Create([21, 22], request, 42);

        Assert.Equal([21, 22], result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(42, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void Create_WithNoMatches_HasZeroTotalPages()
    {
        PageRequest request = CreatePageRequest(page: 4, pageSize: 20);

        PagedResult<int> result = PagedResult<int>.Create([], request, 0);

        Assert.Empty(result.Items);
        Assert.Equal(4, result.Page);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void Create_MaterializesItemsOnce()
    {
        var items = new List<int> { 1, 2 };
        PageRequest request = CreatePageRequest(page: 1, pageSize: 20);

        PagedResult<int> result = PagedResult<int>.Create(items, request, 2);
        items.Add(3);

        Assert.Equal([1, 2], result.Items);
    }

    [Fact]
    public void Create_WithNegativeTotalCount_ThrowsArgumentOutOfRangeException()
    {
        PageRequest request = CreatePageRequest(page: 1, pageSize: 20);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PagedResult<int>.Create([], request, -1));
    }

    private static PageRequest CreatePageRequest(int page, int pageSize)
    {
        Result<PageRequest> result = PageRequest.Create(
            page,
            pageSize,
            null,
            null,
            ["id"],
            "id",
            SortDirection.Ascending);

        return result.Value;
    }
}
