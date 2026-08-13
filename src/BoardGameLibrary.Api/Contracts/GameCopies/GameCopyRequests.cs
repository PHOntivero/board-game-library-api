using System.ComponentModel.DataAnnotations;
using BoardGameLibrary.Domain.GameCopies;

namespace BoardGameLibrary.Api.Contracts.GameCopies;

public abstract class GameCopyRequest
{
    [Required]
    [StringLength(GameCopy.InventoryCodeMaximumLength)]
    public string? InventoryCode { get; init; }

    [Required]
    public GameCopyCondition? Condition { get; init; }

    public DateOnly? AcquiredOn { get; init; }
}

public sealed class CreateGameCopyRequest : GameCopyRequest;

public sealed class UpdateGameCopyRequest : GameCopyRequest
{
    [Required]
    public bool? IsActive { get; init; }
}
