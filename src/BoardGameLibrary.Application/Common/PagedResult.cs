using System.Collections.ObjectModel;

namespace BoardGameLibrary.Application.Common;

public sealed class PagedResult<T>
{
    private PagedResult(
        IReadOnlyList<T> items,
        int page,
        int pageSize,
        int totalCount,
        int totalPages)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = totalPages;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages { get; }

    public static PagedResult<T> Create(
        IEnumerable<T> items,
        PageRequest pageRequest,
        int totalCount)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(pageRequest);

        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount));
        }

        T[] materializedItems = items.ToArray();

        if (materializedItems.Length > pageRequest.PageSize)
        {
            throw new ArgumentException("The number of items cannot exceed the requested page size.", nameof(items));
        }

        if (materializedItems.Length > totalCount)
        {
            throw new ArgumentException("The number of items cannot exceed the total count.", nameof(items));
        }

        int totalPages = totalCount / pageRequest.PageSize;

        if (totalCount % pageRequest.PageSize != 0)
        {
            totalPages++;
        }

        return new PagedResult<T>(
            new ReadOnlyCollection<T>(materializedItems),
            pageRequest.Page,
            pageRequest.PageSize,
            totalCount,
            totalPages);
    }
}
