using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PushSubscriptionDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "device_id",
                table: "push_subscriptions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_push_subscriptions_device_id",
                table: "push_subscriptions",
                column: "device_id");

            migrationBuilder.AddForeignKey(
                name: "FK_push_subscriptions_devices_device_id",
                table: "push_subscriptions",
                column: "device_id",
                principalTable: "devices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_push_subscriptions_devices_device_id",
                table: "push_subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_push_subscriptions_device_id",
                table: "push_subscriptions");

            migrationBuilder.DropColumn(
                name: "device_id",
                table: "push_subscriptions");
        }
    }
}
