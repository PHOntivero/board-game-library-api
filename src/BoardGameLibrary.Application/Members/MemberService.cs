using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.Common.Persistence;
using BoardGameLibrary.Application.Services;
using BoardGameLibrary.Domain.Common;
using BoardGameLibrary.Domain.Members;

namespace BoardGameLibrary.Application.Members;

public sealed class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public MemberService(
        IMemberRepository memberRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> CreateAsync(
        CreateMemberCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly todayUtc = DateOnly.FromDateTime(utcNow.UtcDateTime);
        Member member;

        try
        {
            member = Member.Create(
                command.MemberNumber,
                command.FullName,
                command.Email,
                command.PhoneNumber,
                command.JoinedOn,
                todayUtc);
        }
        catch (DomainException exception)
        {
            return Result<Guid>.Failure(DomainErrorMapper.Map(exception));
        }

        Error? uniquenessError = await CheckUniquenessAsync(member, null, cancellationToken);

        if (uniquenessError is not null)
        {
            return Result<Guid>.Failure(uniquenessError);
        }

        _memberRepository.Add(member);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saveResult.IsSuccess
            ? Result<Guid>.Success(member.Id)
            : Result<Guid>.Failure(saveResult.Errors);
    }

    public async Task<Result<MemberDetails>> GetAsync(
        GetMemberQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        MemberDetails? member = await _memberRepository.GetDetailsAsync(
            query.Id,
            cancellationToken);

        return member is null
            ? Result<MemberDetails>.Failure(NotFound())
            : Result<MemberDetails>.Success(member);
    }

    public async Task<Result<PagedResult<MemberListItem>>> ListAsync(
        ListMembersQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        ListMembersQuery normalizedQuery = query with
        {
            IsActive = query.IsActive ?? true,
        };
        PagedResult<MemberListItem> result = await _memberRepository.ListAsync(
            normalizedQuery,
            cancellationToken);

        return Result<PagedResult<MemberListItem>>.Success(result);
    }

    public async Task<Result<MemberDetails>> UpdateAsync(
        UpdateMemberCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Member? member = await _memberRepository.GetByIdAsync(command.Id, cancellationToken);

        if (member is null)
        {
            return Result<MemberDetails>.Failure(NotFound());
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly todayUtc = DateOnly.FromDateTime(utcNow.UtcDateTime);

        try
        {
            member.Update(
                command.MemberNumber,
                command.FullName,
                command.Email,
                command.PhoneNumber,
                command.JoinedOn,
                todayUtc);
        }
        catch (DomainException exception)
        {
            return Result<MemberDetails>.Failure(DomainErrorMapper.Map(exception));
        }

        Error? uniquenessError = await CheckUniquenessAsync(
            member,
            member.Id,
            cancellationToken);

        if (uniquenessError is not null)
        {
            return Result<MemberDetails>.Failure(uniquenessError);
        }

        member.SetActive(command.IsActive);
        _memberRepository.Update(member);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saveResult.IsSuccess
            ? Result<MemberDetails>.Success(ToDetails(member))
            : Result<MemberDetails>.Failure(saveResult.Errors);
    }

    public async Task<Result> DeleteAsync(
        DeleteMemberCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Member? member = await _memberRepository.GetByIdAsync(command.Id, cancellationToken);

        if (member is null)
        {
            return Result.Failure(NotFound());
        }

        if (await _memberRepository.HasLoanHistoryAsync(member.Id, cancellationToken))
        {
            return Result.Failure(ServiceErrors.Conflict(
                ErrorCodes.Members.HasLoanHistory,
                "A member with loan history cannot be deleted."));
        }

        _memberRepository.Remove(member);
        Result<int> saveResult = await _unitOfWork.SaveChangesAsync(cancellationToken);

        return saveResult.IsSuccess ? Result.Success() : Result.Failure(saveResult.Errors);
    }

    private async Task<Error?> CheckUniquenessAsync(
        Member member,
        Guid? excludingId,
        CancellationToken cancellationToken)
    {
        if (await _memberRepository.ExistsWithNormalizedMemberNumberAsync(
                member.MemberNumber,
                excludingId,
                cancellationToken))
        {
            return ServiceErrors.Conflict(
                ErrorCodes.Members.DuplicateMemberNumber,
                "A member with the same member number already exists.");
        }

        if (await _memberRepository.ExistsWithNormalizedEmailAsync(
                member.NormalizedEmail,
                excludingId,
                cancellationToken))
        {
            return ServiceErrors.Conflict(
                ErrorCodes.Members.DuplicateEmail,
                "A member with the same email already exists.");
        }

        return null;
    }

    private static MemberDetails ToDetails(Member member) => new(
        member.Id,
        member.MemberNumber,
        member.FullName,
        member.Email,
        member.PhoneNumber,
        member.IsActive,
        member.JoinedOn);

    private static Error NotFound() => ServiceErrors.NotFound(
        ErrorCodes.Members.NotFound,
        "Member");
}
