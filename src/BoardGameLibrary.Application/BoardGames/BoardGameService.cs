using BoardGameLibrary.Application.Categories;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Common.Persistence;
using BoardGameLibrary.Application.Services;
using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.Application.BoardGames;

public sealed class BoardGameService : IBoardGameService
{
    private readonly IBoardGameRepository _boardGameRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public BoardGameService(
        IBoardGameRepository boardGameRepository,
        ICategoryRepository categoryRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _boardGameRepository = boardGameRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateBoardGameCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Result<IReadOnlyList<Category>> categoryResult = await ResolveCategoriesAsync(
            command.CategoryIds,
            cancellationToken);

        if (categoryResult.IsFailure)
        {
            return Result<Guid>.Failure(categoryResult.Errors);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly todayUtc = DateOnly.FromDateTime(utcNow.UtcDateTime);
        BoardGame boardGame;

        try
        {
            boardGame = BoardGame.Create(
                command.Title,
                command.Publisher,
                command.Description,
                command.PublicationYear,
                command.MinPlayers,
                command.MaxPlayers,
                command.PlayingTimeMinutes,
                categoryResult.Value,
                todayUtc);
        }
        catch (DomainException exception)
        {
            return Result<Guid>.Failure(DomainErrorMapper.Map(exception));
        }

        _boardGameRepository.Add(boardGame);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saveResult.IsSuccess
            ? Result<Guid>.Success(boardGame.Id)
            : Result<Guid>.Failure(saveResult.Errors);
    }

    public async Task<Result<BoardGameDetails>> GetAsync(
        GetBoardGameQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        BoardGameDetails? boardGame = await _boardGameRepository.GetDetailsAsync(
            query.Id,
            cancellationToken);

        return boardGame is null
            ? Result<BoardGameDetails>.Failure(NotFound())
            : Result<BoardGameDetails>.Success(boardGame);
    }

    public async Task<Result<PagedResult<BoardGameListItem>>> ListAsync(
        ListBoardGamesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CategoryId == Guid.Empty)
        {
            return Result<PagedResult<BoardGameListItem>>.Failure(ServiceErrors.Validation(
                "categoryId must be a non-empty identifier."));
        }

        if (query.Players is < BoardGame.MinimumPlayers or > BoardGame.MaximumPlayers)
        {
            return Result<PagedResult<BoardGameListItem>>.Failure(ServiceErrors.Validation(
                $"players must be between {BoardGame.MinimumPlayers} and {BoardGame.MaximumPlayers}."));
        }

        ListBoardGamesQuery normalizedQuery = query with
        {
            IsActive = query.IsActive ?? true,
        };
        PagedResult<BoardGameListItem> result = await _boardGameRepository.ListAsync(
            normalizedQuery,
            cancellationToken);

        return Result<PagedResult<BoardGameListItem>>.Success(result);
    }

    public async Task<Result<BoardGameDetails>> UpdateAsync(
        UpdateBoardGameCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        BoardGame? boardGame = await _boardGameRepository.GetByIdForUpdateAsync(
            command.Id,
            cancellationToken);

        if (boardGame is null)
        {
            return Result<BoardGameDetails>.Failure(NotFound());
        }

        Result<IReadOnlyList<Category>> categoryResult = await ResolveCategoriesAsync(
            command.CategoryIds,
            cancellationToken);

        if (categoryResult.IsFailure)
        {
            return Result<BoardGameDetails>.Failure(categoryResult.Errors);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly todayUtc = DateOnly.FromDateTime(utcNow.UtcDateTime);

        try
        {
            boardGame.Update(
                command.Title,
                command.Publisher,
                command.Description,
                command.PublicationYear,
                command.MinPlayers,
                command.MaxPlayers,
                command.PlayingTimeMinutes,
                categoryResult.Value,
                todayUtc);
        }
        catch (DomainException exception)
        {
            return Result<BoardGameDetails>.Failure(DomainErrorMapper.Map(exception));
        }

        boardGame.SetActive(command.IsActive);
        _boardGameRepository.Update(boardGame);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            return Result<BoardGameDetails>.Failure(saveResult.Errors);
        }

        BoardGameDetails? details = await _boardGameRepository.GetDetailsAsync(
            boardGame.Id,
            cancellationToken);

        return details is null
            ? Result<BoardGameDetails>.Failure(NotFound())
            : Result<BoardGameDetails>.Success(details);
    }

    public async Task<Result> DeleteAsync(
        DeleteBoardGameCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        BoardGame? boardGame = await _boardGameRepository.GetByIdForUpdateAsync(
            command.Id,
            cancellationToken);

        if (boardGame is null)
        {
            return Result.Failure(NotFound());
        }

        if (await _boardGameRepository.HasCopiesAsync(boardGame.Id, cancellationToken))
        {
            return Result.Failure(ServiceErrors.Conflict(
                ErrorCodes.BoardGames.HasCopies,
                "A board game with physical copies cannot be deleted."));
        }

        _boardGameRepository.Remove(boardGame);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saveResult.IsSuccess ? Result.Success() : Result.Failure(saveResult.Errors);
    }

    private async Task<Result<IReadOnlyList<Category>>> ResolveCategoriesAsync(
        IReadOnlyCollection<Guid>? categoryIds,
        CancellationToken cancellationToken)
    {
        if (categoryIds is null || categoryIds.Count == 0)
        {
            return Result<IReadOnlyList<Category>>.Failure(ServiceErrors.Validation(
                "At least one category is required."));
        }

        if (categoryIds.Any(id => id == Guid.Empty) || categoryIds.Distinct().Count() != categoryIds.Count)
        {
            return Result<IReadOnlyList<Category>>.Failure(ServiceErrors.Validation(
                "Category identifiers must be non-empty and cannot be repeated."));
        }

        IReadOnlyList<Category> categories = await _categoryRepository.GetByIdsAsync(
            categoryIds,
            cancellationToken);

        return categories.Count == categoryIds.Count
            ? Result<IReadOnlyList<Category>>.Success(categories)
            : Result<IReadOnlyList<Category>>.Failure(ServiceErrors.NotFound(
                ErrorCodes.Categories.NotFound,
                "One or more categories"));
    }

    private static Error NotFound() => ServiceErrors.NotFound(
        ErrorCodes.BoardGames.NotFound,
        "Board game");
}
