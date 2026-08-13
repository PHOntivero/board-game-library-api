using BoardGameLibrary.Domain.Common;
using BoardGameLibrary.Domain.Loans;

namespace BoardGameLibrary.UnitTests.Loans;

public sealed class LoanTests
{
    private static readonly Guid GameCopyId = Guid.Parse("0198a4dd-59ad-7dd3-bec4-81a4a5ca8f4e");
    private static readonly Guid MemberId = Guid.Parse("0198a4dd-59ae-780e-92d6-7291debc8872");
    private static readonly DateTimeOffset LoanedAtUtc = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidData_SetsFixedTermAndCreatesVersionSevenIdentifier()
    {
        Loan loan = CreateLoan();

        Assert.Equal(GameCopyId, loan.GameCopyId);
        Assert.Equal(MemberId, loan.MemberId);
        Assert.Equal(LoanedAtUtc, loan.LoanedAtUtc);
        Assert.Equal(LoanedAtUtc.AddDays(Loan.LendingTermDays), loan.DueAtUtc);
        Assert.Null(loan.ReturnedAtUtc);
        Assert.Equal(LoanStatus.Active, loan.GetStatus(LoanedAtUtc));
        DomainTestAssertions.VersionIsSeven(loan.Id);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_WhenRelatedIdentifierIsEmpty_Throws(bool emptyGameCopyIdentifier)
    {
        Guid gameCopyId = emptyGameCopyIdentifier ? Guid.Empty : GameCopyId;
        Guid memberId = emptyGameCopyIdentifier ? MemberId : Guid.Empty;
        string expectedCode = emptyGameCopyIdentifier
            ? "loan.game_copy_id_required"
            : "loan.member_id_required";

        DomainTestAssertions.Throws(
            expectedCode,
            () => Loan.Create(gameCopyId, memberId, LoanedAtUtc));
    }

    [Fact]
    public void Create_WhenLoanDateIsNotUtc_Throws()
    {
        var nonUtcTimestamp = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.FromHours(-3));

        DomainTestAssertions.Throws(
            "loan.loaned_at_not_utc",
            () => Loan.Create(GameCopyId, MemberId, nonUtcTimestamp));
    }

    [Fact]
    public void GetStatus_AtDeadline_IsActive()
    {
        Loan loan = CreateLoan();

        Assert.Equal(LoanStatus.Active, loan.GetStatus(loan.DueAtUtc));
    }

    [Fact]
    public void GetStatus_AfterDeadline_IsOverdue()
    {
        Loan loan = CreateLoan();

        Assert.Equal(LoanStatus.Overdue, loan.GetStatus(loan.DueAtUtc.AddTicks(1)));
    }

    [Fact]
    public void GetStatus_WhenCurrentTimeIsNotUtc_Throws()
    {
        Loan loan = CreateLoan();
        var nonUtcTimestamp = new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.FromHours(1));

        DomainTestAssertions.Throws(
            "loan.current_time_not_utc",
            () => loan.GetStatus(nonUtcTimestamp));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(14)]
    [InlineData(20)]
    public void Return_BeforeAtOrAfterDeadline_RecordsActualTimestamp(int daysAfterLoan)
    {
        Loan loan = CreateLoan();
        DateTimeOffset returnedAtUtc = LoanedAtUtc.AddDays(daysAfterLoan);

        loan.Return(returnedAtUtc);

        Assert.Equal(returnedAtUtc, loan.ReturnedAtUtc);
        Assert.Equal(LoanStatus.Returned, loan.GetStatus(returnedAtUtc));
    }

    [Fact]
    public void Return_AtLoanTime_IsAllowed()
    {
        Loan loan = CreateLoan();

        loan.Return(LoanedAtUtc);

        Assert.Equal(LoanedAtUtc, loan.ReturnedAtUtc);
    }

    [Fact]
    public void Return_BeforeLoanDate_Throws()
    {
        Loan loan = CreateLoan();

        DomainTestAssertions.Throws(
            "loan.returned_before_loan",
            () => loan.Return(LoanedAtUtc.AddTicks(-1)));
        Assert.Null(loan.ReturnedAtUtc);
    }

    [Fact]
    public void Return_WhenTimestampIsNotUtc_Throws()
    {
        Loan loan = CreateLoan();
        var nonUtcTimestamp = new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.FromHours(-3));

        DomainTestAssertions.Throws(
            "loan.returned_at_not_utc",
            () => loan.Return(nonUtcTimestamp));
    }

    [Fact]
    public void Return_WhenAlreadyReturned_ThrowsAndPreservesOriginalTimestamp()
    {
        Loan loan = CreateLoan();
        DateTimeOffset firstReturn = LoanedAtUtc.AddDays(2);
        loan.Return(firstReturn);

        DomainTestAssertions.Throws(
            "loan_already_returned",
            () => loan.Return(LoanedAtUtc.AddDays(3)),
            DomainErrorType.Conflict);
        Assert.Equal(firstReturn, loan.ReturnedAtUtc);
    }

    private static Loan CreateLoan() => Loan.Create(GameCopyId, MemberId, LoanedAtUtc);
}
