using BoardGameLibrary.Domain.GameCopies;

namespace BoardGameLibrary.Application.GameCopies;

public sealed record GameCopyListItem(
    Guid Id,
    Guid BoardGameId,
    string InventoryCode,
    GameCopyCondition Condition,
    bool IsActive,
    DateOnly? AcquiredOn,
    bool IsAvailable);

public sealed record GameCopyDetails(
    Guid Id,
    Guid BoardGameId,
    string BoardGameTitle,
    string InventoryCode,
    GameCopyCondition Condition,
    bool IsActive,
    DateOnly? AcquiredOn,
    bool IsAvailable);
