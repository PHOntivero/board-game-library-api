using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.GameCopies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameLibrary.Infrastructure.Persistence.Configurations;

internal sealed class GameCopyConfiguration : IEntityTypeConfiguration<GameCopy>
{
    public void Configure(EntityTypeBuilder<GameCopy> builder)
    {
        builder.ToTable("game_copies", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_game_copies_inventory_code_not_blank",
                "char_length(btrim(inventory_code)) > 0");
            tableBuilder.HasCheckConstraint(
                "ck_game_copies_inventory_code_normalized",
                "inventory_code = upper(btrim(inventory_code))");
            tableBuilder.HasCheckConstraint(
                "ck_game_copies_condition",
                "condition IN ('Excellent', 'Good', 'Fair', 'Damaged')");
        });

        builder.HasKey(gameCopy => gameCopy.Id)
            .HasName("pk_game_copies");

        builder.Property(gameCopy => gameCopy.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(gameCopy => gameCopy.BoardGameId)
            .HasColumnName("board_game_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(gameCopy => gameCopy.InventoryCode)
            .HasColumnName("inventory_code")
            .HasMaxLength(GameCopy.InventoryCodeMaximumLength)
            .IsRequired();

        builder.Property(gameCopy => gameCopy.Condition)
            .HasColumnName("condition")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(gameCopy => gameCopy.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(gameCopy => gameCopy.AcquiredOn)
            .HasColumnName("acquired_on")
            .HasColumnType("date");

        builder.HasOne<BoardGame>()
            .WithMany()
            .HasForeignKey(gameCopy => gameCopy.BoardGameId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_game_copies_board_games_board_game_id");

        builder.HasIndex(gameCopy => gameCopy.BoardGameId)
            .HasDatabaseName("ix_game_copies_board_game_id");

        builder.HasIndex(gameCopy => gameCopy.InventoryCode)
            .IsUnique()
            .HasDatabaseName("ux_game_copies_inventory_code");
    }
}
