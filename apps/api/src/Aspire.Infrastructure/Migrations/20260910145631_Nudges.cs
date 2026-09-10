using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Nudges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "push_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    board_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    p256dh = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    auth = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    mode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    at_minutes = table.Column<int>(type: "integer", nullable: false),
                    utc_offset_minutes = table.Column<int>(type: "integer", nullable: false),
                    last_sent_on = table.Column<DateOnly>(type: "date", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_push_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_push_subscriptions_boards_board_id",
                        column: x => x.board_id,
                        principalTable: "boards",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_push_subscriptions_board_id",
                table: "push_subscriptions",
                column: "board_id");

            migrationBuilder.CreateIndex(
                name: "IX_push_subscriptions_endpoint",
                table: "push_subscriptions",
                column: "endpoint",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "push_subscriptions");
        }
    }
}
