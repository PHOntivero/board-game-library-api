using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Common.Persistence;
using BoardGameLibrary.Application.Services;
using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.Application.Categories;

public sealed class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Category category;

        try
        {
            category = Category.Create(command.Name);
        }
        catch (DomainException exception)
        {
            return Result<Guid>.Failure(DomainErrorMapper.Map(exception));
        }

        if (await _categoryRepository.ExistsWithNormalizedNameAsync(
                category.NormalizedName,
                null,
                cancellationToken))
        {
            return Result<Guid>.Failure(ServiceErrors.Conflict(
                ErrorCodes.Categories.DuplicateName,
                "A category with the same name already exists."));
        }

        _categoryRepository.Add(category);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saveResult.IsSuccess
            ? Result<Guid>.Success(category.Id)
            : Result<Guid>.Failure(saveResult.Errors);
    }

    public async Task<Result<CategoryDetails>> GetAsync(
        GetCategoryQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        CategoryDetails? category = await _categoryRepository.GetDetailsAsync(
            query.Id,
            cancellationToken);

        return category is null
            ? Result<CategoryDetails>.Failure(ServiceErrors.NotFound(
                ErrorCodes.Categories.NotFound,
                "Category"))
            : Result<CategoryDetails>.Success(category);
    }

    public async Task<Result<PagedResult<CategoryListItem>>> ListAsync(
        ListCategoriesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        ListCategoriesQuery normalizedQuery = query with
        {
            IsActive = query.IsActive ?? true,
        };
        PagedResult<CategoryListItem> result = await _categoryRepository.ListAsync(
            normalizedQuery,
            cancellationToken);

        return Result<PagedResult<CategoryListItem>>.Success(result);
    }

    public async Task<Result<CategoryDetails>> UpdateAsync(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Category? category = await _categoryRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (category is null)
        {
            return Result<CategoryDetails>.Failure(ServiceErrors.NotFound(
                ErrorCodes.Categories.NotFound,
                "Category"));
        }

        try
        {
            category.Update(command.Name);
        }
        catch (DomainException exception)
        {
            return Result<CategoryDetails>.Failure(DomainErrorMapper.Map(exception));
        }

        if (await _categoryRepository.ExistsWithNormalizedNameAsync(
                category.NormalizedName,
                category.Id,
                cancellationToken))
        {
            return Result<CategoryDetails>.Failure(ServiceErrors.Conflict(
                ErrorCodes.Categories.DuplicateName,
                "A category with the same name already exists."));
        }

        category.SetActive(command.IsActive);
        _categoryRepository.Update(category);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saveResult.IsSuccess
            ? Result<CategoryDetails>.Success(ToDetails(category))
            : Result<CategoryDetails>.Failure(saveResult.Errors);
    }

    public async Task<Result> DeleteAsync(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Category? category = await _categoryRepository.GetByIdAsync(
            command.Id,
            cancellationToken);

        if (category is null)
        {
            return Result.Failure(ServiceErrors.NotFound(
                ErrorCodes.Categories.NotFound,
                "Category"));
        }

        if (await _categoryRepository.HasBoardGamesAsync(category.Id, cancellationToken))
        {
            return Result.Failure(ServiceErrors.Conflict(
                ErrorCodes.Categories.HasBoardGames,
                "A category associated with board games cannot be deleted."));
        }

        _categoryRepository.Remove(category);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saveResult.IsSuccess ? Result.Success() : Result.Failure(saveResult.Errors);
    }

    private static CategoryDetails ToDetails(Category category) =>
        new(category.Id, category.Name, category.IsActive);
}
