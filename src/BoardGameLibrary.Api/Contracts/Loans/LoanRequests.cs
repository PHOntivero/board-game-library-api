using System.ComponentModel.DataAnnotations;

namespace BoardGameLibrary.Api.Contracts.Loans;

public sealed class CreateLoanRequest : IValidatableObject
{
    [Required]
    public Guid? MemberId { get; init; }

    [Required]
    public Guid? GameCopyId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MemberId == Guid.Empty)
        {
            yield return new ValidationResult(
                "memberId cannot be an empty identifier.",
                [nameof(MemberId)]);
        }

        if (GameCopyId == Guid.Empty)
        {
            yield return new ValidationResult(
                "gameCopyId cannot be an empty identifier.",
                [nameof(GameCopyId)]);
        }
    }
}
