using BoardGameLibrary.Application.Categories;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace BoardGameLibrary.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(BoardGameLibraryDbContext dbContext) : ICategoryRepository
{
    private readonly BoardGameLibraryDbContext _dbContext = dbContext;

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Categories.SingleOrDefaultAsync(category => category.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        await _dbContext.Categories
            .Where(category => ids.Contains(category.Id))
            .ToListAsync(cancellationToken);

    public Task<CategoryDetails?> GetDetailsAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Id == id)
            .Select(category => new CategoryDetails(category.Id, category.Name, category.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<CategoryListItem>> ListAsync(
        ListCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        bool isActive = query.IsActive
            ?? throw new InvalidOperationException(
                "The category active filter must be normalized by the application layer.");
        IQueryable<Category> filtered = _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive == isActive);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = RepositoryQuery.LiteralContainsPattern(query.Search);
            filtered = filtered.Where(category => EF.Functions.ILike(
                category.Name,
                pattern,
                RepositoryQuery.LikeEscapeCharacter));
        }

        int totalCount = await filtered.CountAsync(cancellationToken);
        bool descending = query.PageRequest.SortDirection == SortDirection.Descending;
        IOrderedQueryable<Category> ordered = descending
            ? filtered.OrderByDescending(category => category.Name)
                .ThenByDescending(category => category.Id)
            : filtered.OrderBy(category => category.Name)
                .ThenBy(category => category.Id);
        IQueryable<CategoryListItem> projected = ordered.Select(category =>
            new CategoryListItem(category.Id, category.Name, category.IsActive));

        return await RepositoryQuery.ToPagedResultAsync(
            projected,
            query.PageRequest,
            totalCount,
            cancellationToken);
    }

    public Task<bool> ExistsWithNormalizedNameAsync(
        string normalizedName,
        Guid? excludingId,
        CancellationToken cancellationToken) =>
        _dbContext.Categories
            .AsNoTracking()
            .AnyAsync(
                category => category.NormalizedName == normalizedName
                    && (!excludingId.HasValue || category.Id != excludingId.Value),
                cancellationToken);

    public Task<bool> HasBoardGamesAsync(Guid id, CancellationToken cancellationToken) =>
        _dbContext.BoardGames
            .AsNoTracking()
            .AnyAsync(
                boardGame => boardGame.Categories.Any(category => category.Id == id),
                cancellationToken);

    public void Add(Category category) => _dbContext.Categories.Add(category);

    public void Update(Category category) => _dbContext.Categories.Update(category);

    public void Remove(Category category) => _dbContext.Categories.Remove(category);
}
