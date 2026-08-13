using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.Domain.Members;

public sealed class Member
{
    public const int MemberNumberMaximumLength = 20;
    public const int FullNameMaximumLength = 150;
    public const int EmailMaximumLength = 254;
    public const int PhoneNumberMaximumLength = 30;

    private Member()
    {
    }

    public Guid Id { get; private set; }

    public string MemberNumber { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public bool IsActive { get; private set; }

    public DateOnly JoinedOn { get; private set; }

    public static Member Create(
        string memberNumber,
        string fullName,
        string email,
        string? phoneNumber,
        DateOnly joinedOn,
        DateOnly todayUtc)
    {
        ValidatedDetails details = ValidateDetails(
            memberNumber,
            fullName,
            email,
            phoneNumber,
            joinedOn,
            todayUtc);

        return new Member
        {
            Id = Guid.CreateVersion7(),
            MemberNumber = details.MemberNumber,
            FullName = details.FullName,
            Email = details.Email,
            NormalizedEmail = details.Email.ToUpperInvariant(),
            PhoneNumber = details.PhoneNumber,
            IsActive = true,
            JoinedOn = joinedOn,
        };
    }

    public void Update(
        string memberNumber,
        string fullName,
        string email,
        string? phoneNumber,
        DateOnly joinedOn,
        DateOnly todayUtc)
    {
        ValidatedDetails details = ValidateDetails(
            memberNumber,
            fullName,
            email,
            phoneNumber,
            joinedOn,
            todayUtc);

        MemberNumber = details.MemberNumber;
        FullName = details.FullName;
        Email = details.Email;
        NormalizedEmail = details.Email.ToUpperInvariant();
        PhoneNumber = details.PhoneNumber;
        JoinedOn = joinedOn;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    private static ValidatedDetails ValidateDetails(
        string? memberNumber,
        string? fullName,
        string? email,
        string? phoneNumber,
        DateOnly joinedOn,
        DateOnly todayUtc)
    {
        string normalizedMemberNumber = DomainGuard.RequiredText(
                memberNumber,
                MemberNumberMaximumLength,
                "Member number",
                "member.member_number")
            .ToUpperInvariant();
        string normalizedFullName = DomainGuard.RequiredText(
            fullName,
            FullNameMaximumLength,
            "Member full name",
            "member.full_name");
        string normalizedEmail = DomainGuard.RequiredText(
            email,
            EmailMaximumLength,
            "Member email",
            "member.email");
        string? normalizedPhoneNumber = DomainGuard.OptionalText(
            phoneNumber,
            PhoneNumberMaximumLength,
            "Member phone number",
            "member.phone_number");

        DomainGuard.NotFuture(
            joinedOn,
            todayUtc,
            "Join date",
            "member.joined_on_in_future");

        return new ValidatedDetails(
            normalizedMemberNumber,
            normalizedFullName,
            normalizedEmail,
            normalizedPhoneNumber);
    }

    private sealed record ValidatedDetails(
        string MemberNumber,
        string FullName,
        string Email,
        string? PhoneNumber);
}
