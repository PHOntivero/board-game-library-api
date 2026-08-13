using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoardGameLibrary.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "board_games",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    publisher = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    publication_year = table.Column<int>(type: "integer", nullable: false),
                    min_players = table.Column<int>(type: "integer", nullable: false),
                    max_players = table.Column<int>(type: "integer", nullable: false),
                    playing_time_minutes = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_games", x => x.id);
                    table.CheckConstraint("ck_board_games_max_players_range", "max_players BETWEEN 1 AND 99");
                    table.CheckConstraint("ck_board_games_min_players_range", "min_players BETWEEN 1 AND 99");
                    table.CheckConstraint("ck_board_games_player_range", "max_players >= min_players");
                    table.CheckConstraint("ck_board_games_playing_time_minutes_range", "playing_time_minutes BETWEEN 1 AND 1440");
                    table.CheckConstraint("ck_board_games_publication_year_minimum", "publication_year >= 1900");
                    table.CheckConstraint("ck_board_games_publisher_not_blank", "char_length(btrim(publisher)) > 0");
                    table.CheckConstraint("ck_board_games_title_not_blank", "char_length(btrim(title)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                    table.CheckConstraint("ck_categories_name_not_blank", "char_length(btrim(name)) > 0");
                    table.CheckConstraint("ck_categories_normalized_name", "normalized_name = upper(btrim(name))");
                });

            migrationBuilder.CreateTable(
                name: "members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    joined_on = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members", x => x.id);
                    table.CheckConstraint("ck_members_email_not_blank", "char_length(btrim(email)) > 0");
                    table.CheckConstraint("ck_members_full_name_not_blank", "char_length(btrim(full_name)) > 0");
                    table.CheckConstraint("ck_members_member_number_normalized", "member_number = upper(btrim(member_number))");
                    table.CheckConstraint("ck_members_member_number_not_blank", "char_length(btrim(member_number)) > 0");
                    table.CheckConstraint("ck_members_normalized_email", "normalized_email = upper(btrim(email))");
                    table.CheckConstraint("ck_members_phone_number_not_blank", "phone_number IS NULL OR char_length(btrim(phone_number)) > 0");
                });

            migrationBuilder.CreateTable(
                name: "game_copies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inventory_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    condition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    acquired_on = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_copies", x => x.id);
                    table.CheckConstraint("ck_game_copies_condition", "condition IN ('Excellent', 'Good', 'Fair', 'Damaged')");
                    table.CheckConstraint("ck_game_copies_inventory_code_normalized", "inventory_code = upper(btrim(inventory_code))");
                    table.CheckConstraint("ck_game_copies_inventory_code_not_blank", "char_length(btrim(inventory_code)) > 0");
                    table.ForeignKey(
                        name: "fk_game_copies_board_games_board_game_id",
                        column: x => x.board_game_id,
                        principalTable: "board_games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "board_game_categories",
                columns: table => new
                {
                    board_game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_board_game_categories", x => new { x.board_game_id, x.category_id });
                    table.ForeignKey(
                        name: "fk_board_game_categories_board_games_board_game_id",
                        column: x => x.board_game_id,
                        principalTable: "board_games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_board_game_categories_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_copy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loaned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    returned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loans", x => x.id);
                    table.CheckConstraint("ck_loans_fixed_lending_term", "EXTRACT(EPOCH FROM (due_at_utc - loaned_at_utc)) = 1209600");
                    table.CheckConstraint("ck_loans_returned_after_loaned", "returned_at_utc IS NULL OR returned_at_utc >= loaned_at_utc");
                    table.ForeignKey(
                        name: "fk_loans_game_copies_game_copy_id",
                        column: x => x.game_copy_id,
                        principalTable: "game_copies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loans_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_board_game_categories_category_id",
                table: "board_game_categories",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_board_games_title_trgm",
                table: "board_games",
                column: "title")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_name_trgm",
                table: "categories",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ux_categories_normalized_name",
                table: "categories",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_copies_board_game_id",
                table: "game_copies",
                column: "board_game_id");

            migrationBuilder.CreateIndex(
                name: "ux_game_copies_inventory_code",
                table: "game_copies",
                column: "inventory_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_loans_game_copy_id_loaned_at_utc",
                table: "loans",
                columns: new[] { "game_copy_id", "loaned_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_loans_loaned_at_utc",
                table: "loans",
                column: "loaned_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_loans_member_id_returned_at_utc_due_at_utc",
                table: "loans",
                columns: new[] { "member_id", "returned_at_utc", "due_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ux_loans_game_copy_id_open",
                table: "loans",
                column: "game_copy_id",
                unique: true,
                filter: "returned_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_members_email_trgm",
                table: "members",
                column: "email")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_members_full_name_trgm",
                table: "members",
                column: "full_name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ux_members_member_number",
                table: "members",
                column: "member_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_members_normalized_email",
                table: "members",
                column: "normalized_email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "board_game_categories");

            migrationBuilder.DropTable(
                name: "loans");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "game_copies");

            migrationBuilder.DropTable(
                name: "members");

            migrationBuilder.DropTable(
                name: "board_games");
        }
    }
}
