using BoardGameLibrary.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace BoardGameLibrary.Api.Contracts;

public sealed class PaginationQuery
{
    [FromQuery(Name = "page")]
    public int? Page { get; init; }

    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; init; }

    [FromQuery(Name = "sortBy")]
    public string? SortBy { get; init; }

    [FromQuery(Name = "sortDirection")]
    public string? SortDirection { get; init; }

    public Result<PageRequest> ToPageRequest(
        IReadOnlyCollection<string> allowedSortFields,
        string defaultSortBy,
        SortDirection defaultDirection) =>
        PageRequest.Create(
            Page,
            PageSize,
            SortBy,
            SortDirection,
            allowedSortFields,
            defaultSortBy,
            defaultDirection);
}
