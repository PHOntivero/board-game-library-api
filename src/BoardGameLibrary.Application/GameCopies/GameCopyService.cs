using System.Data;
using BoardGameLibrary.Application.BoardGames;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Common.Persistence;
using BoardGameLibrary.Application.Loans;
using BoardGameLibrary.Application.Services;
using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Common;
using BoardGameLibrary.Domain.GameCopies;

namespace BoardGameLibrary.Application.GameCopies;

public sealed class GameCopyService : IGameCopyService
{
    private readonly IGameCopyRepository _gameCopyRepository;
    private readonly IBoardGameRepository _boardGameRepository;
    private readonly ILoanRepository _loanRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public GameCopyService(
        IGameCopyRepository gameCopyRepository,
        IBoardGameRepository boardGameRepository,
        ILoanRepository loanRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _gameCopyRepository = gameCopyRepository;
        _boardGameRepository = boardGameRepository;
        _loanRepository = loanRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateGameCopyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly todayUtc = DateOnly.FromDateTime(utcNow.UtcDateTime);
        GameCopy gameCopy;

        try
        {
            gameCopy = GameCopy.Create(
                command.BoardGameId,
                command.InventoryCode,
                command.Condition,
                command.AcquiredOn,
                todayUtc);
        }
        catch (DomainException exception)
        {
            return Result<Guid>.Failure(DomainErrorMapper.Map(exception));
        }

        BoardGame? boardGame = await _boardGameRepository.GetByIdForUpdateAsync(
            command.BoardGameId,
            cancellationToken);

        if (boardGame is null)
        {
            return Result<Guid>.Failure(ServiceErrors.NotFound(
                ErrorCodes.BoardGames.NotFound,
                "Board game"));
        }

        if (!boardGame.IsActive)
        {
            return Result<Guid>.Failure(ServiceErrors.Conflict(
                ErrorCodes.BoardGames.Inactive,
                "Physical copies cannot be added to an inactive board game."));
        }

        if (await _gameCopyRepository.ExistsWithNormalizedInventoryCodeAsync(
                gameCopy.InventoryCode,
                null,
                cancellationToken))
        {
            return Result<Guid>.Failure(DuplicateInventoryCode());
        }

        _gameCopyRepository.Add(gameCopy);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saveResult.IsSuccess
            ? Result<Guid>.Success(gameCopy.Id)
            : Result<Guid>.Failure(saveResult.Errors);
    }

    public async Task<Result<GameCopyDetails>> GetAsync(
        GetGameCopyQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        GameCopyDetails? gameCopy = await _gameCopyRepository.GetDetailsAsync(
            query.Id,
            cancellationToken);

        return gameCopy is null
            ? Result<GameCopyDetails>.Failure(NotFound())
            : Result<GameCopyDetails>.Success(gameCopy);
    }

    public async Task<Result<PagedResult<GameCopyListItem>>> ListAsync(
        ListGameCopiesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.BoardGameId == Guid.Empty)
        {
            return Result<PagedResult<GameCopyListItem>>.Failure(ServiceErrors.Validation(
                "boardGameId must be a non-empty identifier."));
        }

        BoardGameDetails? boardGame = await _boardGameRepository.GetDetailsAsync(
            query.BoardGameId,
            cancellationToken);

        if (boardGame is null)
        {
            return Result<PagedResult<GameCopyListItem>>.Failure(ServiceErrors.NotFound(
                ErrorCodes.BoardGames.NotFound,
                "Board game"));
        }

        ListGameCopiesQuery normalizedQuery = query with
        {
            IsActive = query.IsActive ?? true,
        };
        PagedResult<GameCopyListItem> result = await _gameCopyRepository.ListByBoardGameAsync(
            normalizedQuery,
            cancellationToken);

        return Result<PagedResult<GameCopyListItem>>.Success(result);
    }

    public async Task<Result<GameCopyDetails>> UpdateAsync(
        UpdateGameCopyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly todayUtc = DateOnly.FromDateTime(utcNow.UtcDateTime);
        await using ITransaction transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        GameCopy? gameCopy = await _gameCopyRepository.GetByIdForUpdateAsync(
            command.Id,
            cancellationToken);

        if (gameCopy is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<GameCopyDetails>.Failure(NotFound());
        }

        if (!command.IsActive &&
            await _loanRepository.HasOpenLoanForGameCopyAsync(gameCopy.Id, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<GameCopyDetails>.Failure(ServiceErrors.Conflict(
                ErrorCodes.GameCopies.HasOpenLoan,
                "A physical copy with an open loan cannot be deactivated."));
        }

        try
        {
            gameCopy.Update(
                command.InventoryCode,
                command.Condition,
                command.AcquiredOn,
                todayUtc);
        }
        catch (DomainException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<GameCopyDetails>.Failure(DomainErrorMapper.Map(exception));
        }

        if (await _gameCopyRepository.ExistsWithNormalizedInventoryCodeAsync(
                gameCopy.InventoryCode,
                gameCopy.Id,
                cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<GameCopyDetails>.Failure(DuplicateInventoryCode());
        }

        gameCopy.SetActive(command.IsActive);
        _gameCopyRepository.Update(gameCopy);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<GameCopyDetails>.Failure(saveResult.Errors);
        }

        await transaction.CommitAsync(cancellationToken);
        GameCopyDetails? details = await _gameCopyRepository.GetDetailsAsync(
            gameCopy.Id,
            cancellationToken);

        return details is null
            ? Result<GameCopyDetails>.Failure(NotFound())
            : Result<GameCopyDetails>.Success(details);
    }

    public async Task<Result> DeleteAsync(
        DeleteGameCopyCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using ITransaction transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        GameCopy? gameCopy = await _gameCopyRepository.GetByIdForUpdateAsync(
            command.Id,
            cancellationToken);

        if (gameCopy is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(NotFound());
        }

        if (await _gameCopyRepository.HasLoanHistoryAsync(gameCopy.Id, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(ServiceErrors.Conflict(
                ErrorCodes.GameCopies.HasLoanHistory,
                "A physical copy with loan history cannot be deleted."));
        }

        _gameCopyRepository.Remove(gameCopy);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(saveResult.Errors);
        }

        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    private static Error NotFound() => ServiceErrors.NotFound(
        ErrorCodes.GameCopies.NotFound,
        "Physical copy");

    private static Error DuplicateInventoryCode() => ServiceErrors.Conflict(
        ErrorCodes.GameCopies.DuplicateInventoryCode,
        "A physical copy with the same inventory code already exists.");
}
