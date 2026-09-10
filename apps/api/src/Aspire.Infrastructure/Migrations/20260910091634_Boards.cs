using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Boards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dreams_sort_order",
                table: "dreams");

            migrationBuilder.AddColumn<string>(
                name: "board_id",
                table: "dreams",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "board_id",
                table: "devices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "boards",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boards", x => x.id);
                });

            // Hand-written, the rest is scaffolded. Rows that predate boards
            // keep working: they get a board of their own, which takes
            // `Pairing:Code` on the next start (BoardSeed) because its code
            // is empty. A fresh server gets the same board with nothing yet
            // pointing at it, and the seed treats it the same way.
            migrationBuilder.Sql(
                """
                INSERT INTO boards (id, name, code_hash, created_at)
                VALUES ('2f0c6a1e2b8d4c7f9e3a5b1d8c4e6f20', 'Nástěnka', '', NOW());
                UPDATE devices SET board_id = '2f0c6a1e2b8d4c7f9e3a5b1d8c4e6f20' WHERE board_id = '';
                UPDATE dreams SET board_id = '2f0c6a1e2b8d4c7f9e3a5b1d8c4e6f20' WHERE board_id = '';
                """);

            // The empty-string default above existed only to get past the
            // rows that were already there; the model has no default.
            migrationBuilder.AlterColumn<string>(
                name: "board_id",
                table: "dreams",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "board_id",
                table: "devices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldDefaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_dreams_board_id_sort_order",
                table: "dreams",
                columns: new[] { "board_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_devices_board_id",
                table: "devices",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "IX_boards_name",
                table: "boards",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_devices_boards_board_id",
                table: "devices",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_dreams_boards_board_id",
                table: "dreams",
                column: "board_id",
                principalTable: "boards",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_devices_boards_board_id",
                table: "devices");

            migrationBuilder.DropForeignKey(
                name: "FK_dreams_boards_board_id",
                table: "dreams");

            migrationBuilder.DropTable(
                name: "boards");

            migrationBuilder.DropIndex(
                name: "IX_dreams_board_id_sort_order",
                table: "dreams");

            migrationBuilder.DropIndex(
                name: "IX_devices_board_id",
                table: "devices");

            migrationBuilder.DropColumn(
                name: "board_id",
                table: "dreams");

            migrationBuilder.DropColumn(
                name: "board_id",
                table: "devices");

            migrationBuilder.CreateIndex(
                name: "IX_dreams_sort_order",
                table: "dreams",
                column: "sort_order");
        }
    }
}
