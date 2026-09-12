using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Bytes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "bytes",
                table: "dream_images",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bytes",
                table: "dream_images");
        }
    }
}
