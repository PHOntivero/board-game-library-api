using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Application.Categories;

public sealed record GetCategoryQuery(Guid Id);

public sealed record ListCategoriesQuery(
    string? Search,
    bool? IsActive,
    PageRequest PageRequest);

public static class CategorySortFields
{
    public const string Name = "name";
    public const string Default = Name;

    public static IReadOnlyCollection<string> Allowed { get; } = Array.AsReadOnly([Name]);

    public const SortDirection DefaultDirection = SortDirection.Ascending;
}
