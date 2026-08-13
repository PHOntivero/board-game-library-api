using BoardGameLibrary.Domain.GameCopies;

namespace BoardGameLibrary.Application.GameCopies;

public sealed record CreateGameCopyCommand(
    Guid BoardGameId,
    string InventoryCode,
    GameCopyCondition Condition,
    DateOnly? AcquiredOn);

public sealed record UpdateGameCopyCommand(
    Guid Id,
    string InventoryCode,
    GameCopyCondition Condition,
    DateOnly? AcquiredOn,
    bool IsActive);

public sealed record DeleteGameCopyCommand(Guid Id);
