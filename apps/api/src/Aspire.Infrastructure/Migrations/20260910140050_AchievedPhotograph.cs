using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AchievedPhotograph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "kind",
                table: "dream_images",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                // Every photograph taken before this migration is a dreamt
                // one. EF's generated default is "", which
                // DreamImageKindNames.Parse throws on — so the board would
                // 500 on the first read after a deploy rather than show the
                // pictures that were already there.
                defaultValue: "dreamt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "kind",
                table: "dream_images");
        }
    }
}
