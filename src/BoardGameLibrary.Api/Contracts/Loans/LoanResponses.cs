using BoardGameLibrary.Api.Contracts;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.Loans;

namespace BoardGameLibrary.Api.Contracts.Loans;

public sealed record LoanGameCopyResponse(
    Guid Id,
    string InventoryCode,
    Guid BoardGameId,
    string BoardGameTitle);

public sealed record LoanMemberResponse(
    Guid Id,
    string MemberNumber,
    string FullName);

public sealed record LoanListItemResponse(
    Guid Id,
    LoanGameCopyResponse GameCopy,
    LoanMemberResponse Member,
    DateTimeOffset LoanedAtUtc,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    LoanStatus Status);

public sealed record LoanResponse(
    Guid Id,
    LoanGameCopyResponse GameCopy,
    LoanMemberResponse Member,
    DateTimeOffset LoanedAtUtc,
    DateTimeOffset DueAtUtc,
    DateTimeOffset? ReturnedAtUtc,
    LoanStatus Status);

internal static class LoanResponseMappings
{
    internal static LoanResponse ToResponse(
        this Application.Loans.LoanDetails source) =>
        new(
            source.Id,
            source.GameCopy.ToResponse(),
            source.Member.ToResponse(),
            source.LoanedAtUtc,
            source.DueAtUtc,
            source.ReturnedAtUtc,
            source.Status);

    internal static PagedResponse<LoanListItemResponse> ToResponse(
        this PagedResult<Application.Loans.LoanListItem> source) =>
        new(
            source.Items
                .Select(item => new LoanListItemResponse(
                    item.Id,
                    item.GameCopy.ToResponse(),
                    item.Member.ToResponse(),
                    item.LoanedAtUtc,
                    item.DueAtUtc,
                    item.ReturnedAtUtc,
                    item.Status))
                .ToArray(),
            source.Page,
            source.PageSize,
            source.TotalCount,
            source.TotalPages);

    private static LoanGameCopyResponse ToResponse(
        this Application.Loans.LoanGameCopy source) =>
        new(
            source.Id,
            source.InventoryCode,
            source.BoardGameId,
            source.BoardGameTitle);

    private static LoanMemberResponse ToResponse(
        this Application.Loans.LoanMember source) =>
        new(source.Id, source.MemberNumber, source.FullName);
}
