using System.ComponentModel.DataAnnotations;
using BoardGameLibrary.Domain.BoardGames;

namespace BoardGameLibrary.Api.Contracts.BoardGames;

public abstract class BoardGameRequest : IValidatableObject
{
    [Required]
    [StringLength(BoardGame.TitleMaximumLength)]
    public string? Title { get; init; }

    [Required]
    [StringLength(BoardGame.PublisherMaximumLength)]
    public string? Publisher { get; init; }

    [StringLength(BoardGame.DescriptionMaximumLength)]
    public string? Description { get; init; }

    [Required]
    [Range(BoardGame.MinimumPublicationYear, int.MaxValue)]
    public int? PublicationYear { get; init; }

    [Required]
    [Range(BoardGame.MinimumPlayers, BoardGame.MaximumPlayers)]
    public int? MinPlayers { get; init; }

    [Required]
    [Range(BoardGame.MinimumPlayers, BoardGame.MaximumPlayers)]
    public int? MaxPlayers { get; init; }

    [Required]
    [Range(BoardGame.MinimumPlayingTimeMinutes, BoardGame.MaximumPlayingTimeMinutes)]
    public int? PlayingTimeMinutes { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyCollection<Guid>? CategoryIds { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPlayers.HasValue && MaxPlayers.HasValue && MaxPlayers < MinPlayers)
        {
            yield return new ValidationResult(
                "maxPlayers must be greater than or equal to minPlayers.",
                [nameof(MaxPlayers)]);
        }

        if (CategoryIds is null)
        {
            yield break;
        }

        if (CategoryIds.Any(id => id == Guid.Empty))
        {
            yield return new ValidationResult(
                "categoryIds cannot contain an empty identifier.",
                [nameof(CategoryIds)]);
        }

        if (CategoryIds.Distinct().Count() != CategoryIds.Count)
        {
            yield return new ValidationResult(
                "categoryIds cannot contain duplicate identifiers.",
                [nameof(CategoryIds)]);
        }
    }
}

public sealed class CreateBoardGameRequest : BoardGameRequest;

public sealed class UpdateBoardGameRequest : BoardGameRequest
{
    [Required]
    public bool? IsActive { get; init; }
}
