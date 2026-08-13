using System.Data;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Common.Persistence;
using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Domain.GameCopies;
using BoardGameLibrary.Domain.Loans;
using BoardGameLibrary.Domain.Members;
using BoardGameLibrary.Infrastructure.Persistence.Models;
using BoardGameLibrary.Infrastructure.Persistence.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Npgsql;

namespace BoardGameLibrary.Infrastructure.Persistence;

internal sealed class UnitOfWork(BoardGameLibraryDbContext dbContext) : IUnitOfWork
{
    private static readonly IReadOnlyDictionary<string, Error> KnownUniqueConstraintErrors =
        new Dictionary<string, Error>(StringComparer.Ordinal)
        {
            ["ux_categories_normalized_name"] = Error.Conflict(
                ErrorCodes.Categories.DuplicateName,
                "A category with the same name already exists."),
            ["ux_game_copies_inventory_code"] = Error.Conflict(
                ErrorCodes.GameCopies.DuplicateInventoryCode,
                "A game copy with the same inventory code already exists."),
            ["ux_members_member_number"] = Error.Conflict(
                ErrorCodes.Members.DuplicateMemberNumber,
                "A member with the same member number already exists."),
            ["ux_members_normalized_email"] = Error.Conflict(
                ErrorCodes.Members.DuplicateEmail,
                "A member with the same email already exists."),
            ["ux_loans_game_copy_id_open"] = Error.Conflict(
                ErrorCodes.GameCopies.HasOpenLoan,
                "The game copy already has an open loan."),
        };

    private static readonly Error BoardGameHasCopies = Error.Conflict(
        ErrorCodes.BoardGames.HasCopies,
        "A board game with physical copies cannot be deleted.");

    private static readonly Error CategoryHasBoardGames = Error.Conflict(
        ErrorCodes.Categories.HasBoardGames,
        "A category associated with board games cannot be deleted.");

    private static readonly Error GameCopyHasLoanHistory = Error.Conflict(
        ErrorCodes.GameCopies.HasLoanHistory,
        "A game copy with loan history cannot be deleted.");

    private static readonly Error MemberHasLoanHistory = Error.Conflict(
        ErrorCodes.Members.HasLoanHistory,
        "A member with loan history cannot be deleted.");

    private static readonly Error BoardGameNotFound = Error.NotFound(
        ErrorCodes.BoardGames.NotFound,
        "Board game was not found.");

    private static readonly Error CategoryNotFound = Error.NotFound(
        ErrorCodes.Categories.NotFound,
        "Category was not found.");

    private static readonly Error GameCopyNotFound = Error.NotFound(
        ErrorCodes.GameCopies.NotFound,
        "Game copy was not found.");

    private static readonly Error MemberNotFound = Error.NotFound(
        ErrorCodes.Members.NotFound,
        "Member was not found.");

    private readonly BoardGameLibraryDbContext _dbContext = dbContext;

    public async Task<ITransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(
            isolationLevel,
            cancellationToken);

        return new EfTransaction(transaction);
    }

    public async Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            int affectedRows = await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(affectedRows);
        }
        catch (DbUpdateException exception)
        {
            Error? error = GetKnownConstraintError(exception);

            if (error is null)
            {
                throw;
            }

            return Result<int>.Failure(error);
        }
    }

    private Error? GetKnownConstraintError(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException
            || string.IsNullOrEmpty(postgresException.ConstraintName))
        {
            return null;
        }

        if (postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return KnownUniqueConstraintErrors.GetValueOrDefault(
                postgresException.ConstraintName);
        }

        if (postgresException.SqlState == PostgresErrorCodes.RestrictViolation)
        {
            return GetKnownDeleteRestrictionError(
                postgresException.ConstraintName,
                exception);
        }

        if (postgresException.SqlState != PostgresErrorCodes.ForeignKeyViolation)
        {
            return null;
        }

        return GetKnownMissingReferenceError(postgresException.ConstraintName, exception);
    }

    private Error? GetKnownDeleteRestrictionError(
        string constraintName,
        DbUpdateException exception) =>
        constraintName switch
        {
            "fk_game_copies_board_games_board_game_id"
                when FindSingleEntry<BoardGame>(exception, EntityState.Deleted) is not null =>
                BoardGameHasCopies,
            "fk_board_game_categories_categories_category_id"
                when FindSingleEntry<Category>(exception, EntityState.Deleted) is not null =>
                CategoryHasBoardGames,
            "fk_loans_game_copies_game_copy_id"
                when FindSingleEntry<GameCopy>(exception, EntityState.Deleted) is not null =>
                GameCopyHasLoanHistory,
            "fk_loans_members_member_id"
                when FindSingleEntry<Member>(exception, EntityState.Deleted) is not null =>
                MemberHasLoanHistory,
            _ => null,
        };

    private Error? GetKnownMissingReferenceError(
        string constraintName,
        DbUpdateException exception) =>
        constraintName switch
        {
            "fk_game_copies_board_games_board_game_id"
                when FindSingleEntry<GameCopy>(
                    exception,
                    EntityState.Added,
                    EntityState.Modified) is not null => BoardGameNotFound,
            "fk_board_game_categories_categories_category_id"
                when FindSingleEntry<BoardGameCategory>(
                    exception,
                    EntityState.Added,
                    EntityState.Modified) is not null => CategoryNotFound,
            "fk_loans_game_copies_game_copy_id"
                when FindSingleEntry<Loan>(
                    exception,
                    EntityState.Added,
                    EntityState.Modified) is not null => GameCopyNotFound,
            "fk_loans_members_member_id"
                when FindSingleEntry<Loan>(
                    exception,
                    EntityState.Added,
                    EntityState.Modified) is not null => MemberNotFound,
            _ => null,
        };

    private EntityEntry? FindSingleEntry<TEntity>(
        DbUpdateException exception,
        params EntityState[] expectedStates)
        where TEntity : class
    {
        EntityEntry[] exceptionMatches = exception.Entries
            .Where(entry =>
                entry.Entity is TEntity
                && expectedStates.Contains(entry.State))
            .ToArray();

        if (exceptionMatches.Length == 1)
        {
            return exceptionMatches[0];
        }

        if (exceptionMatches.Length > 1)
        {
            return null;
        }

        EntityEntry[] trackedMatches = _dbContext.ChangeTracker
            .Entries<TEntity>()
            .Where(entry => expectedStates.Contains(entry.State))
            .Cast<EntityEntry>()
            .ToArray();

        return trackedMatches.Length == 1 ? trackedMatches[0] : null;
    }
}
