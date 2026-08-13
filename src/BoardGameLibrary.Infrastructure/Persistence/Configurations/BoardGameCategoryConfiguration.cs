using BoardGameLibrary.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameLibrary.Infrastructure.Persistence.Configurations;

internal sealed class BoardGameCategoryConfiguration : IEntityTypeConfiguration<BoardGameCategory>
{
    public void Configure(EntityTypeBuilder<BoardGameCategory> builder)
    {
        builder.ToTable("board_game_categories");

        builder.HasKey(join => new { join.BoardGameId, join.CategoryId })
            .HasName("pk_board_game_categories");

        builder.Property(join => join.BoardGameId)
            .HasColumnName("board_game_id")
            .HasColumnType("uuid");

        builder.Property(join => join.CategoryId)
            .HasColumnName("category_id")
            .HasColumnType("uuid");

        builder.HasIndex(join => join.CategoryId)
            .HasDatabaseName("ix_board_game_categories_category_id");
    }
}
