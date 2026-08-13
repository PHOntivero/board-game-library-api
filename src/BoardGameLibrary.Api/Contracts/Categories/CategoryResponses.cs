using BoardGameLibrary.Api.Contracts;
using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Api.Contracts.Categories;

public sealed record CategoryListItemResponse(
    Guid Id,
    string Name,
    bool IsActive);

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    bool IsActive);

internal static class CategoryResponseMappings
{
    internal static CategoryResponse ToResponse(
        this Application.Categories.CategoryDetails source) =>
        new(source.Id, source.Name, source.IsActive);

    internal static PagedResponse<CategoryListItemResponse> ToResponse(
        this PagedResult<Application.Categories.CategoryListItem> source) =>
        new(
            source.Items
                .Select(item => new CategoryListItemResponse(
                    item.Id,
                    item.Name,
                    item.IsActive))
                .ToArray(),
            source.Page,
            source.PageSize,
            source.TotalCount,
            source.TotalPages);
}
