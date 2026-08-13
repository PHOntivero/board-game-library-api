using BoardGameLibrary.Domain.Common;

namespace BoardGameLibrary.Domain.Categories;

public sealed class Category
{
    public const int NameMaximumLength = 80;

    private Category()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static Category Create(string name)
    {
        string normalizedDisplayName = NormalizeName(name);

        return new Category
        {
            Id = Guid.CreateVersion7(),
            Name = normalizedDisplayName,
            NormalizedName = normalizedDisplayName.ToUpperInvariant(),
            IsActive = true,
        };
    }

    public void Update(string name)
    {
        string normalizedDisplayName = NormalizeName(name);

        Name = normalizedDisplayName;
        NormalizedName = normalizedDisplayName.ToUpperInvariant();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    private static string NormalizeName(string? name) =>
        DomainGuard.RequiredText(name, NameMaximumLength, "Category name", "category.name");
}
