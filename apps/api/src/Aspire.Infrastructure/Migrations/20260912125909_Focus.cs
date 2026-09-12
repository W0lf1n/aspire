using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Focus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "focus_rank",
                table: "dreams",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dreams_board_id_focus_rank",
                table: "dreams",
                columns: new[] { "board_id", "focus_rank" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_dreams_board_id_focus_rank",
                table: "dreams");

            migrationBuilder.DropColumn(
                name: "focus_rank",
                table: "dreams");
        }
    }
}
