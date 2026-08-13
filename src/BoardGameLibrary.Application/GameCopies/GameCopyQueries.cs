using BoardGameLibrary.Application.Common;
using BoardGameLibrary.Domain.GameCopies;

namespace BoardGameLibrary.Application.GameCopies;

public sealed record GetGameCopyQuery(Guid Id);

public sealed record ListGameCopiesQuery(
    Guid BoardGameId,
    GameCopyCondition? Condition,
    bool? IsAvailable,
    bool? IsActive,
    PageRequest PageRequest);

public static class GameCopySortFields
{
    public const string InventoryCode = "inventoryCode";
    public const string Condition = "condition";
    public const string AcquiredOn = "acquiredOn";

    public const string Default = InventoryCode;

    public static IReadOnlyCollection<string> Allowed { get; } =
        Array.AsReadOnly([InventoryCode, Condition, AcquiredOn]);

    public const SortDirection DefaultDirection = SortDirection.Ascending;
}
