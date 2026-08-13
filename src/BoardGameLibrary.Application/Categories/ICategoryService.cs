using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Application.Categories;

public interface ICategoryService
{
    Task<Result<Guid>> CreateAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken);

    Task<Result<CategoryDetails>> GetAsync(
        GetCategoryQuery query,
        CancellationToken cancellationToken);

    Task<Result<PagedResult<CategoryListItem>>> ListAsync(
        ListCategoriesQuery query,
        CancellationToken cancellationToken);

    Task<Result<CategoryDetails>> UpdateAsync(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken);

    Task<Result> DeleteAsync(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken);
}
