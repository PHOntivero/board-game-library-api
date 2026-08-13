using System.Data;
using BoardGameLibrary.Application.BoardGames;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Common.Persistence;
using BoardGameLibrary.Application.GameCopies;
using BoardGameLibrary.Application.Members;
using BoardGameLibrary.Application.Services;
using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Common;
using BoardGameLibrary.Domain.GameCopies;
using BoardGameLibrary.Domain.Loans;
using BoardGameLibrary.Domain.Members;

namespace BoardGameLibrary.Application.Loans;

public sealed class LoanService : ILoanService
{
    public const int MaximumOpenLoansPerMember = 3;

    private readonly ILoanRepository _loanRepository;
    private readonly IBoardGameRepository _boardGameRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IGameCopyRepository _gameCopyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public LoanService(
        ILoanRepository loanRepository,
        IBoardGameRepository boardGameRepository,
        IMemberRepository memberRepository,
        IGameCopyRepository gameCopyRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _loanRepository = loanRepository;
        _boardGameRepository = boardGameRepository;
        _memberRepository = memberRepository;
        _gameCopyRepository = gameCopyRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateLoanCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        GameCopy? initialCopy = await _gameCopyRepository.GetByIdAsync(
            command.GameCopyId,
            cancellationToken);

        if (initialCopy is null)
        {
            return Result<Guid>.Failure(GameCopyNotFound());
        }

        await using ITransaction transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        BoardGame? boardGame = await _boardGameRepository.GetByIdForShareAsync(
            initialCopy.BoardGameId,
            cancellationToken);

        if (boardGame is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<Guid>.Failure(ServiceErrors.NotFound(
                ErrorCodes.BoardGames.NotFound,
                "Board game"));
        }

        Member? member = await _memberRepository.GetByIdForUpdateAsync(
            command.MemberId,
            cancellationToken);

        if (member is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<Guid>.Failure(ServiceErrors.NotFound(
                ErrorCodes.Members.NotFound,
                "Member"));
        }

        GameCopy? gameCopy = await _gameCopyRepository.GetByIdForUpdateAsync(
            command.GameCopyId,
            cancellationToken);

        if (gameCopy is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<Guid>.Failure(GameCopyNotFound());
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        Error? eligibilityError = await GetEligibilityErrorAsync(
            boardGame,
            member,
            gameCopy,
            utcNow,
            cancellationToken);

        if (eligibilityError is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<Guid>.Failure(eligibilityError);
        }

        Loan loan;

        try
        {
            loan = Loan.Create(gameCopy.Id, member.Id, utcNow);
        }
        catch (DomainException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<Guid>.Failure(DomainErrorMapper.Map(exception));
        }

        _loanRepository.Add(loan);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<Guid>.Failure(saveResult.Errors);
        }

        await transaction.CommitAsync(cancellationToken);
        return Result<Guid>.Success(loan.Id);
    }

    public async Task<Result<LoanDetails>> GetAsync(
        GetLoanQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        LoanDetails? loan = await _loanRepository.GetDetailsAsync(
            query.Id,
            utcNow,
            cancellationToken);

        return loan is null
            ? Result<LoanDetails>.Failure(LoanNotFound())
            : Result<LoanDetails>.Success(loan);
    }

    public async Task<Result<PagedResult<LoanListItem>>> ListAsync(
        ListLoansQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Error? validationError = ValidateListQuery(query);

        if (validationError is not null)
        {
            return Result<PagedResult<LoanListItem>>.Failure(validationError);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PagedResult<LoanListItem> result = await _loanRepository.ListAsync(
            query,
            utcNow,
            cancellationToken);

        return Result<PagedResult<LoanListItem>>.Success(result);
    }

    public async Task<Result> ReturnAsync(
        ReturnLoanCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using ITransaction transaction = await _unitOfWork.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        Loan? loan = await _loanRepository.GetByIdForUpdateAsync(
            command.Id,
            cancellationToken);

        if (loan is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(LoanNotFound());
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        try
        {
            loan.Return(utcNow);
        }
        catch (DomainException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(DomainErrorMapper.Map(exception));
        }

        _loanRepository.Update(loan);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (saveResult.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure(saveResult.Errors);
        }

        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Error?> GetEligibilityErrorAsync(
        BoardGame boardGame,
        Member member,
        GameCopy gameCopy,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (gameCopy.BoardGameId != boardGame.Id)
        {
            return ServiceErrors.Conflict(
                ErrorCodes.GameCopies.Unavailable,
                "The physical copy no longer belongs to the expected board game.");
        }

        if (!boardGame.IsActive)
        {
            return ServiceErrors.Conflict(
                ErrorCodes.BoardGames.Inactive,
                "An inactive board game cannot be loaned.");
        }

        if (!member.IsActive)
        {
            return ServiceErrors.Conflict(
                ErrorCodes.Members.Inactive,
                "An inactive member cannot start a loan.");
        }

        if (!gameCopy.IsActive)
        {
            return ServiceErrors.Conflict(
                ErrorCodes.GameCopies.Inactive,
                "An inactive physical copy cannot be loaned.");
        }

        if (gameCopy.Condition == GameCopyCondition.Damaged)
        {
            return ServiceErrors.Conflict(
                ErrorCodes.GameCopies.Damaged,
                "A damaged physical copy cannot be loaned.");
        }

        if (await _loanRepository.HasOpenLoanForGameCopyAsync(
                gameCopy.Id,
                cancellationToken))
        {
            return ServiceErrors.Conflict(
                ErrorCodes.GameCopies.HasOpenLoan,
                "The physical copy is already on loan.");
        }

        if (await _loanRepository.HasOverdueLoanForMemberAsync(
                member.Id,
                utcNow,
                cancellationToken))
        {
            return ServiceErrors.Conflict(
                ErrorCodes.Members.HasOverdueLoan,
                "A member with an overdue loan cannot start another loan.");
        }

        int openLoanCount = await _loanRepository.CountOpenLoansForMemberAsync(
            member.Id,
            cancellationToken);

        return openLoanCount >= MaximumOpenLoansPerMember
            ? ServiceErrors.Conflict(
                ErrorCodes.Members.LoanLimitReached,
                $"A member cannot have more than {MaximumOpenLoansPerMember} open loans.")
            : null;
    }

    private static Error? ValidateListQuery(ListLoansQuery query)
    {
        if (query.MemberId == Guid.Empty)
        {
            return ServiceErrors.Validation("memberId must be a non-empty identifier.");
        }

        if (query.GameCopyId == Guid.Empty)
        {
            return ServiceErrors.Validation("gameCopyId must be a non-empty identifier.");
        }

        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
        {
            return ServiceErrors.Validation("Loan status is invalid.");
        }

        if (query.LoanedFromUtc.HasValue && query.LoanedFromUtc.Value.Offset != TimeSpan.Zero ||
            query.LoanedToUtc.HasValue && query.LoanedToUtc.Value.Offset != TimeSpan.Zero)
        {
            return ServiceErrors.Validation("Loan date filters must be expressed in UTC.");
        }

        if (query.LoanedFromUtc > query.LoanedToUtc)
        {
            return ServiceErrors.Validation("loanedFrom cannot be later than loanedTo.");
        }

        return null;
    }

    private static Error LoanNotFound() => ServiceErrors.NotFound(
        ErrorCodes.Loans.NotFound,
        "Loan");

    private static Error GameCopyNotFound() => ServiceErrors.NotFound(
        ErrorCodes.GameCopies.NotFound,
        "Physical copy");
}
