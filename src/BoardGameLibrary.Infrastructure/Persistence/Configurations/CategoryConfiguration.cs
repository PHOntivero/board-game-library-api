using BoardGameLibrary.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameLibrary.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_categories_name_not_blank",
                "char_length(btrim(name)) > 0");
            tableBuilder.HasCheckConstraint(
                "ck_categories_normalized_name",
                "normalized_name = upper(btrim(name))");
        });

        builder.HasKey(category => category.Id)
            .HasName("pk_categories");

        builder.Property(category => category.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(Category.NameMaximumLength)
            .IsRequired();

        builder.Property(category => category.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(Category.NameMaximumLength)
            .IsRequired();

        builder.Property(category => category.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.HasIndex(category => category.NormalizedName)
            .IsUnique()
            .HasDatabaseName("ux_categories_normalized_name");

        builder.HasIndex(category => category.Name)
            .HasDatabaseName("ix_categories_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
