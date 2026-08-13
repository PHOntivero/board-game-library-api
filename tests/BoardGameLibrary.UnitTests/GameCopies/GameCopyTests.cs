using BoardGameLibrary.Domain.GameCopies;

namespace BoardGameLibrary.UnitTests.GameCopies;

public sealed class GameCopyTests
{
    private static readonly DateOnly TodayUtc = new(2026, 8, 13);
    private static readonly Guid BoardGameId = Guid.Parse("0198a4dd-59ad-7dd3-bec4-81a4a5ca8f4e");

    [Fact]
    public void Create_WithValidData_NormalizesCodeAndCreatesActiveVersionSevenIdentifier()
    {
        var acquiredOn = new DateOnly(2025, 1, 20);

        GameCopy copy = GameCopy.Create(
            BoardGameId,
            "  catan-002  ",
            GameCopyCondition.Good,
            acquiredOn,
            TodayUtc);

        Assert.Equal(BoardGameId, copy.BoardGameId);
        Assert.Equal("CATAN-002", copy.InventoryCode);
        Assert.Equal(GameCopyCondition.Good, copy.Condition);
        Assert.Equal(acquiredOn, copy.AcquiredOn);
        Assert.True(copy.IsActive);
        DomainTestAssertions.VersionIsSeven(copy.Id);
    }

    [Fact]
    public void Create_WithNoAcquisitionDate_AllowsNull()
    {
        GameCopy copy = GameCopy.Create(
            BoardGameId,
            "CATAN-002",
            GameCopyCondition.Excellent,
            null,
            TodayUtc);

        Assert.Null(copy.AcquiredOn);
    }

    [Fact]
    public void Create_WhenBoardGameIdentifierIsEmpty_Throws()
    {
        DomainTestAssertions.Throws(
            "game_copy.board_game_id_required",
            () => GameCopy.Create(
                Guid.Empty,
                "CATAN-002",
                GameCopyCondition.Good,
                null,
                TodayUtc));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenInventoryCodeIsMissing_Throws(string? inventoryCode)
    {
        DomainTestAssertions.Throws(
            "game_copy.inventory_code.required",
            () => GameCopy.Create(
                BoardGameId,
                inventoryCode!,
                GameCopyCondition.Good,
                null,
                TodayUtc));
    }

    [Fact]
    public void Create_WhenInventoryCodeExceedsLimit_Throws()
    {
        string inventoryCode = new('a', GameCopy.InventoryCodeMaximumLength + 1);

        DomainTestAssertions.Throws(
            "game_copy.inventory_code.too_long",
            () => GameCopy.Create(
                BoardGameId,
                inventoryCode,
                GameCopyCondition.Good,
                null,
                TodayUtc));
    }

    [Fact]
    public void Create_WhenConditionIsUndefined_Throws()
    {
        DomainTestAssertions.Throws(
            "game_copy.condition_invalid",
            () => GameCopy.Create(
                BoardGameId,
                "CATAN-002",
                (GameCopyCondition)99,
                null,
                TodayUtc));
    }

    [Fact]
    public void Create_WhenAcquisitionDateIsInFuture_Throws()
    {
        DomainTestAssertions.Throws(
            "game_copy.acquired_on_in_future",
            () => GameCopy.Create(
                BoardGameId,
                "CATAN-002",
                GameCopyCondition.Good,
                TodayUtc.AddDays(1),
                TodayUtc));
    }

    [Fact]
    public void Update_ReplacesAndNormalizesMutableDetails()
    {
        GameCopy copy = GameCopy.Create(
            BoardGameId,
            "CATAN-002",
            GameCopyCondition.Good,
            null,
            TodayUtc);
        var acquiredOn = new DateOnly(2024, 3, 10);

        copy.Update("  catan-099  ", GameCopyCondition.Fair, acquiredOn, TodayUtc);

        Assert.Equal("CATAN-099", copy.InventoryCode);
        Assert.Equal(GameCopyCondition.Fair, copy.Condition);
        Assert.Equal(acquiredOn, copy.AcquiredOn);
        Assert.Equal(BoardGameId, copy.BoardGameId);
    }

    [Fact]
    public void Update_WhenAcquisitionDateIsInFuture_DoesNotPartiallyChangeState()
    {
        GameCopy copy = GameCopy.Create(
            BoardGameId,
            "CATAN-002",
            GameCopyCondition.Good,
            null,
            TodayUtc);

        DomainTestAssertions.Throws(
            "game_copy.acquired_on_in_future",
            () => copy.Update(
                "CATAN-099",
                GameCopyCondition.Damaged,
                TodayUtc.AddDays(1),
                TodayUtc));

        Assert.Equal("CATAN-002", copy.InventoryCode);
        Assert.Equal(GameCopyCondition.Good, copy.Condition);
    }

    [Fact]
    public void SetActive_ChangesActiveState()
    {
        GameCopy copy = GameCopy.Create(
            BoardGameId,
            "CATAN-002",
            GameCopyCondition.Good,
            null,
            TodayUtc);

        copy.SetActive(false);
        Assert.False(copy.IsActive);

        copy.SetActive(true);
        Assert.True(copy.IsActive);
    }
}
