using System.ComponentModel.DataAnnotations;
using BoardGameLibrary.Domain.Members;

namespace BoardGameLibrary.Api.Contracts.Members;

public abstract class MemberRequest
{
    [Required]
    [StringLength(Member.MemberNumberMaximumLength)]
    public string? MemberNumber { get; init; }

    [Required]
    [StringLength(Member.FullNameMaximumLength)]
    public string? FullName { get; init; }

    [Required]
    [StringLength(Member.EmailMaximumLength)]
    [EmailAddress]
    public string? Email { get; init; }

    [StringLength(Member.PhoneNumberMaximumLength)]
    public string? PhoneNumber { get; init; }

    [Required]
    public DateOnly? JoinedOn { get; init; }
}

public sealed class CreateMemberRequest : MemberRequest;

public sealed class UpdateMemberRequest : MemberRequest
{
    [Required]
    public bool? IsActive { get; init; }
}
