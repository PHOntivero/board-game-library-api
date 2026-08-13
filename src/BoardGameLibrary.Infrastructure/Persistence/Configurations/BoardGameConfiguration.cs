using BoardGameLibrary.Domain.BoardGames;
using BoardGameLibrary.Domain.Categories;
using BoardGameLibrary.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BoardGameLibrary.Infrastructure.Persistence.Configurations;

internal sealed class BoardGameConfiguration : IEntityTypeConfiguration<BoardGame>
{
    public void Configure(EntityTypeBuilder<BoardGame> builder)
    {
        builder.ToTable("board_games", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "ck_board_games_title_not_blank",
                "char_length(btrim(title)) > 0");
            tableBuilder.HasCheckConstraint(
                "ck_board_games_publisher_not_blank",
                "char_length(btrim(publisher)) > 0");
            tableBuilder.HasCheckConstraint(
                "ck_board_games_publication_year_minimum",
                $"publication_year >= {BoardGame.MinimumPublicationYear}");
            tableBuilder.HasCheckConstraint(
                "ck_board_games_min_players_range",
                $"min_players BETWEEN {BoardGame.MinimumPlayers} AND {BoardGame.MaximumPlayers}");
            tableBuilder.HasCheckConstraint(
                "ck_board_games_max_players_range",
                $"max_players BETWEEN {BoardGame.MinimumPlayers} AND {BoardGame.MaximumPlayers}");
            tableBuilder.HasCheckConstraint(
                "ck_board_games_player_range",
                "max_players >= min_players");
            tableBuilder.HasCheckConstraint(
                "ck_board_games_playing_time_minutes_range",
                $"playing_time_minutes BETWEEN {BoardGame.MinimumPlayingTimeMinutes} AND {BoardGame.MaximumPlayingTimeMinutes}");
        });

        builder.HasKey(boardGame => boardGame.Id)
            .HasName("pk_board_games");

        builder.Property(boardGame => boardGame.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(boardGame => boardGame.Title)
            .HasColumnName("title")
            .HasMaxLength(BoardGame.TitleMaximumLength)
            .IsRequired();

        builder.Property(boardGame => boardGame.Publisher)
            .HasColumnName("publisher")
            .HasMaxLength(BoardGame.PublisherMaximumLength)
            .IsRequired();

        builder.Property(boardGame => boardGame.Description)
            .HasColumnName("description")
            .HasMaxLength(BoardGame.DescriptionMaximumLength);

        builder.Property(boardGame => boardGame.PublicationYear)
            .HasColumnName("publication_year")
            .IsRequired();

        builder.Property(boardGame => boardGame.MinPlayers)
            .HasColumnName("min_players")
            .IsRequired();

        builder.Property(boardGame => boardGame.MaxPlayers)
            .HasColumnName("max_players")
            .IsRequired();

        builder.Property(boardGame => boardGame.PlayingTimeMinutes)
            .HasColumnName("playing_time_minutes")
            .IsRequired();

        builder.Property(boardGame => boardGame.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.HasIndex(boardGame => boardGame.Title)
            .HasDatabaseName("ix_board_games_title_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasMany(boardGame => boardGame.Categories)
            .WithMany()
            .UsingEntity<BoardGameCategory>(
                joinBuilder => joinBuilder
                    .HasOne<Category>()
                    .WithMany()
                    .HasForeignKey(join => join.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_board_game_categories_categories_category_id"),
                joinBuilder => joinBuilder
                    .HasOne<BoardGame>()
                    .WithMany()
                    .HasForeignKey(join => join.BoardGameId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("fk_board_game_categories_board_games_board_game_id"));

        builder.Navigation(boardGame => boardGame.Categories)
            .HasField("_categories")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
