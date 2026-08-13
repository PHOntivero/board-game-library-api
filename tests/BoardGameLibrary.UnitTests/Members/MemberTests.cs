using BoardGameLibrary.Domain.Members;

namespace BoardGameLibrary.UnitTests.Members;

public sealed class MemberTests
{
    private static readonly DateOnly TodayUtc = new(2026, 8, 13);

    [Fact]
    public void Create_WithValidData_NormalizesFieldsAndCreatesActiveVersionSevenIdentifier()
    {
        var joinedOn = new DateOnly(2025, 6, 15);

        Member member = Member.Create(
            "  mem-001  ",
            "  Ada Lovelace  ",
            "  Ada@example.com  ",
            "  +55 11 99999-0000  ",
            joinedOn,
            TodayUtc);

        Assert.Equal("MEM-001", member.MemberNumber);
        Assert.Equal("Ada Lovelace", member.FullName);
        Assert.Equal("Ada@example.com", member.Email);
        Assert.Equal("ADA@EXAMPLE.COM", member.NormalizedEmail);
        Assert.Equal("+55 11 99999-0000", member.PhoneNumber);
        Assert.Equal(joinedOn, member.JoinedOn);
        Assert.True(member.IsActive);
        DomainTestAssertions.VersionIsSeven(member.Id);
    }

    [Fact]
    public void Create_WhenPhoneNumberIsWhitespace_StoresNull()
    {
        Member member = CreateMember(phoneNumber: "   ");

        Assert.Null(member.PhoneNumber);
    }

    [Theory]
    [InlineData("memberNumber", "member.member_number.required")]
    [InlineData("fullName", "member.full_name.required")]
    [InlineData("email", "member.email.required")]
    public void Create_WhenRequiredTextIsMissing_Throws(string field, string expectedCode)
    {
        Action action = field switch
        {
            "memberNumber" => () => CreateMember(memberNumber: "   "),
            "fullName" => () => CreateMember(fullName: "   "),
            "email" => () => CreateMember(email: "   "),
            _ => throw new InvalidOperationException(),
        };

        DomainTestAssertions.Throws(expectedCode, action);
    }

    [Theory]
    [InlineData("memberNumber", "member.member_number.too_long")]
    [InlineData("fullName", "member.full_name.too_long")]
    [InlineData("email", "member.email.too_long")]
    [InlineData("phoneNumber", "member.phone_number.too_long")]
    public void Create_WhenTextExceedsLimit_Throws(string field, string expectedCode)
    {
        Action action = field switch
        {
            "memberNumber" => () => CreateMember(
                memberNumber: new string('a', Member.MemberNumberMaximumLength + 1)),
            "fullName" => () => CreateMember(
                fullName: new string('a', Member.FullNameMaximumLength + 1)),
            "email" => () => CreateMember(
                email: new string('a', Member.EmailMaximumLength + 1)),
            "phoneNumber" => () => CreateMember(
                phoneNumber: new string('a', Member.PhoneNumberMaximumLength + 1)),
            _ => throw new InvalidOperationException(),
        };

        DomainTestAssertions.Throws(expectedCode, action);
    }

    [Fact]
    public void Create_WhenJoinDateIsInFuture_Throws()
    {
        DomainTestAssertions.Throws(
            "member.joined_on_in_future",
            () => CreateMember(joinedOn: TodayUtc.AddDays(1)));
    }

    [Fact]
    public void Update_ReplacesAndNormalizesMutableDetails()
    {
        Member member = CreateMember();
        var joinedOn = new DateOnly(2024, 4, 12);

        member.Update(
            "  mem-099  ",
            "  Grace Hopper  ",
            "  Grace@example.com  ",
            "  +1 555 0100  ",
            joinedOn,
            TodayUtc);

        Assert.Equal("MEM-099", member.MemberNumber);
        Assert.Equal("Grace Hopper", member.FullName);
        Assert.Equal("Grace@example.com", member.Email);
        Assert.Equal("GRACE@EXAMPLE.COM", member.NormalizedEmail);
        Assert.Equal("+1 555 0100", member.PhoneNumber);
        Assert.Equal(joinedOn, member.JoinedOn);
    }

    [Fact]
    public void Update_WhenJoinDateIsInFuture_DoesNotPartiallyChangeState()
    {
        Member member = CreateMember();

        DomainTestAssertions.Throws(
            "member.joined_on_in_future",
            () => member.Update(
                "MEM-099",
                "Grace Hopper",
                "grace@example.com",
                null,
                TodayUtc.AddDays(1),
                TodayUtc));

        Assert.Equal("MEM-001", member.MemberNumber);
        Assert.Equal("Ada Lovelace", member.FullName);
    }

    [Fact]
    public void SetActive_ChangesActiveState()
    {
        Member member = CreateMember();

        member.SetActive(false);
        Assert.False(member.IsActive);

        member.SetActive(true);
        Assert.True(member.IsActive);
    }

    private static Member CreateMember(
        string memberNumber = "MEM-001",
        string fullName = "Ada Lovelace",
        string email = "ada@example.com",
        string? phoneNumber = null,
        DateOnly? joinedOn = null) =>
        Member.Create(
            memberNumber,
            fullName,
            email,
            phoneNumber,
            joinedOn ?? new DateOnly(2025, 6, 15),
            TodayUtc);
}
