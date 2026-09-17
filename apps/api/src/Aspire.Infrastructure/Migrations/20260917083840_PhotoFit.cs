using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PhotoFit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fit",
                table: "dream_images",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "fill");

            migrationBuilder.AddColumn<string>(
                name: "mat",
                table: "dream_images",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "night");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fit",
                table: "dream_images");

            migrationBuilder.DropColumn(
                name: "mat",
                table: "dream_images");
        }
    }
}
