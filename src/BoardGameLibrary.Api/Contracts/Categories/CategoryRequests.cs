using System.ComponentModel.DataAnnotations;
using BoardGameLibrary.Domain.Categories;

namespace BoardGameLibrary.Api.Contracts.Categories;

public sealed class CreateCategoryRequest
{
    [Required]
    [StringLength(Category.NameMaximumLength)]
    public string? Name { get; init; }
}

public sealed class UpdateCategoryRequest
{
    [Required]
    [StringLength(Category.NameMaximumLength)]
    public string? Name { get; init; }

    [Required]
    public bool? IsActive { get; init; }
}
