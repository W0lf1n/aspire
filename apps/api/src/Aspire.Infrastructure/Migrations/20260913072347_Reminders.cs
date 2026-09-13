using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <summary>
    /// One reminder a day becomes up to five (D72).
    ///
    /// Written by hand rather than as scaffolded: EF put the `DropColumn`
    /// first, which would have thrown away the hour every subscriber had
    /// chosen and started them all at nothing. The order here is add, carry
    /// across, and only then drop — and the same in reverse going back, so a
    /// rollback keeps the first of somebody's reminders instead of seven
    /// o'clock for everybody.
    /// </summary>
    public partial class Reminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "times",
                table: "push_subscriptions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "last_sent_minutes",
                table: "push_subscriptions",
                type: "integer",
                nullable: true);

            // The one hour each device had becomes its list of one.
            migrationBuilder.Sql(
                "UPDATE push_subscriptions SET times = at_minutes::text");

            // And a device already nudged today was nudged at that hour, so
            // today does not start again from the top on the morning this
            // ships.
            migrationBuilder.Sql(
                "UPDATE push_subscriptions SET last_sent_minutes = at_minutes WHERE last_sent_on IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "at_minutes",
                table: "push_subscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "at_minutes",
                table: "push_subscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Back to one: the first of them, which is the one a person who
            // only ever set one still has.
            migrationBuilder.Sql(
                "UPDATE push_subscriptions SET at_minutes = " +
                "COALESCE(NULLIF(split_part(times, ',', 1), '')::int, 420)");

            migrationBuilder.DropColumn(
                name: "last_sent_minutes",
                table: "push_subscriptions");

            migrationBuilder.DropColumn(
                name: "times",
                table: "push_subscriptions");
        }
    }
}
