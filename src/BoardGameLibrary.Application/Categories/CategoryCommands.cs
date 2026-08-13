namespace BoardGameLibrary.Application.Categories;

public sealed record CreateCategoryCommand(string Name);

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    bool IsActive);

public sealed record DeleteCategoryCommand(Guid Id);
