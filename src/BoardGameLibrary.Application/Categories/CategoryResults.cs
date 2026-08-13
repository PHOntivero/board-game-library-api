namespace BoardGameLibrary.Application.Categories;

public sealed record CategoryListItem(
    Guid Id,
    string Name,
    bool IsActive);

public sealed record CategoryDetails(
    Guid Id,
    string Name,
    bool IsActive);
