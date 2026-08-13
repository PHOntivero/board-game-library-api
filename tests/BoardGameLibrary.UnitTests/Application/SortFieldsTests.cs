using BoardGameLibrary.Application.BoardGames;
using BoardGameLibrary.Application.Categories;
using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Application.GameCopies;
using BoardGameLibrary.Application.Loans;
using BoardGameLibrary.Application.Members;

namespace BoardGameLibrary.UnitTests.Application;

public sealed class SortFieldsTests
{
    public static TheoryData<IReadOnlyCollection<string>, string, SortDirection, string[]> Contracts => new()
    {
        {
            BoardGameSortFields.Allowed,
            BoardGameSortFields.Default,
            BoardGameSortFields.DefaultDirection,
            ["title", "publisher", "publicationYear", "minPlayers", "maxPlayers", "playingTimeMinutes"]
        },
        {
            CategorySortFields.Allowed,
            CategorySortFields.Default,
            CategorySortFields.DefaultDirection,
            ["name"]
        },
        {
            GameCopySortFields.Allowed,
            GameCopySortFields.Default,
            GameCopySortFields.DefaultDirection,
            ["inventoryCode", "condition", "acquiredOn"]
        },
        {
            MemberSortFields.Allowed,
            MemberSortFields.Default,
            MemberSortFields.DefaultDirection,
            ["fullName", "memberNumber", "joinedOn"]
        },
        {
            LoanSortFields.Allowed,
            LoanSortFields.Default,
            LoanSortFields.DefaultDirection,
            ["loanedAtUtc", "dueAtUtc", "returnedAtUtc"]
        },
    };

    [Theory]
    [MemberData(nameof(Contracts))]
    public void Contract_ContainsExactAllowlistAndDefault(
        IReadOnlyCollection<string> allowed,
        string defaultField,
        SortDirection defaultDirection,
        string[] expected)
    {
        Assert.Equal(expected, allowed);
        Assert.Contains(defaultField, allowed);

        SortDirection expectedDirection = defaultField == LoanSortFields.Default
            ? SortDirection.Descending
            : SortDirection.Ascending;

        Assert.Equal(expectedDirection, defaultDirection);
    }

    [Theory]
    [MemberData(nameof(Contracts))]
    public void AllowedCollection_CannotBeMutated(
        IReadOnlyCollection<string> allowed,
        string defaultField,
        SortDirection defaultDirection,
        string[] expected)
    {
        ICollection<string> mutableView = Assert.IsAssignableFrom<ICollection<string>>(allowed);

        Assert.True(mutableView.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => mutableView.Add("unsupported"));
        Assert.Equal(expected, allowed);
        Assert.Contains(defaultField, allowed);
        Assert.True(Enum.IsDefined(defaultDirection));
    }
}
