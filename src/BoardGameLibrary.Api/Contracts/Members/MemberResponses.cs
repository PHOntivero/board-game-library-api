using BoardGameLibrary.Api.Contracts;
using BoardGameLibrary.Application.Common;

namespace BoardGameLibrary.Api.Contracts.Members;

public sealed record MemberListItemResponse(
    Guid Id,
    string MemberNumber,
    string FullName,
    string Email,
    bool IsActive,
    DateOnly JoinedOn);

public sealed record MemberResponse(
    Guid Id,
    string MemberNumber,
    string FullName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    DateOnly JoinedOn);

internal static class MemberResponseMappings
{
    internal static MemberResponse ToResponse(
        this Application.Members.MemberDetails source) =>
        new(
            source.Id,
            source.MemberNumber,
            source.FullName,
            source.Email,
            source.PhoneNumber,
            source.IsActive,
            source.JoinedOn);

    internal static PagedResponse<MemberListItemResponse> ToResponse(
        this PagedResult<Application.Members.MemberListItem> source) =>
        new(
            source.Items
                .Select(item => new MemberListItemResponse(
                    item.Id,
                    item.MemberNumber,
                    item.FullName,
                    item.Email,
                    item.IsActive,
                    item.JoinedOn))
                .ToArray(),
            source.Page,
            source.PageSize,
            source.TotalCount,
            source.TotalPages);
}
