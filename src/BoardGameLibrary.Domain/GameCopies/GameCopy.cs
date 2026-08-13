using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.Domain.GameCopies;

public sealed class GameCopy
{
    public const int InventoryCodeMaximumLength = 30;

    private GameCopy()
    {
    }

    public Guid Id { get; private set; }

    public Guid BoardGameId { get; private set; }

    public string InventoryCode { get; private set; } = string.Empty;

    public GameCopyCondition Condition { get; private set; }

    public bool IsActive { get; private set; }

    public DateOnly? AcquiredOn { get; private set; }

    public static GameCopy Create(
        Guid boardGameId,
        string inventoryCode,
        GameCopyCondition condition,
        DateOnly? acquiredOn,
        DateOnly todayUtc)
    {
        DomainGuard.NotEmpty(boardGameId, "Board game identifier", "game_copy.board_game_id_required");
        ValidatedDetails details = ValidateDetails(inventoryCode, condition, acquiredOn, todayUtc);

        return new GameCopy
        {
            Id = Guid.CreateVersion7(),
            BoardGameId = boardGameId,
            InventoryCode = details.InventoryCode,
            Condition = condition,
            IsActive = true,
            AcquiredOn = acquiredOn,
        };
    }

    public void Update(
        string inventoryCode,
        GameCopyCondition condition,
        DateOnly? acquiredOn,
        DateOnly todayUtc)
    {
        ValidatedDetails details = ValidateDetails(inventoryCode, condition, acquiredOn, todayUtc);

        InventoryCode = details.InventoryCode;
        Condition = condition;
        AcquiredOn = acquiredOn;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    private static ValidatedDetails ValidateDetails(
        string? inventoryCode,
        GameCopyCondition condition,
        DateOnly? acquiredOn,
        DateOnly todayUtc)
    {
        string normalizedInventoryCode = DomainGuard.RequiredText(
                inventoryCode,
                InventoryCodeMaximumLength,
                "Inventory code",
                "game_copy.inventory_code")
            .ToUpperInvariant();

        if (!Enum.IsDefined(condition))
        {
            throw new DomainException(
                "game_copy.condition_invalid",
                "Game copy condition is invalid.",
                DomainErrorType.Validation);
        }

        if (acquiredOn.HasValue)
        {
            DomainGuard.NotFuture(
                acquiredOn.Value,
                todayUtc,
                "Acquisition date",
                "game_copy.acquired_on_in_future");
        }

        return new ValidatedDetails(normalizedInventoryCode);
    }

    private sealed record ValidatedDetails(string InventoryCode);
}
