using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.Categories;

namespace BoardGameLibrary.Application.Categories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken);

    Task<CategoryDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CategoryListItem>> ListAsync(
        ListCategoriesQuery query,
        CancellationToken cancellationToken);

    Task<bool> ExistsWithNormalizedNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken);

    Task<bool> HasBoardGamesAsync(Guid id, CancellationToken cancellationToken);

    void Add(Category category);

    void Update(Category category);

    void Remove(Category category);
}
