using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Focal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "focus_x",
                table: "dream_images",
                type: "double precision",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "focus_y",
                table: "dream_images",
                type: "double precision",
                nullable: false,
                defaultValue: 0.5);

            migrationBuilder.AddColumn<double>(
                name: "zoom",
                table: "dream_images",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "focus_x",
                table: "dream_images");

            migrationBuilder.DropColumn(
                name: "focus_y",
                table: "dream_images");

            migrationBuilder.DropColumn(
                name: "zoom",
                table: "dream_images");
        }
    }
}
