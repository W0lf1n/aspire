using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeznamOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No column changes: `sort_order` changes what it means (D79).
            //
            // It was the order dreams were written in, oldest lowest, and the
            // Seznam read it backwards — newest first, by `created_at`. Now it
            // is the Seznam's own order, lowest first, and a new dream goes in
            // front. Turning every number over keeps the list exactly as it
            // stood the day before: what was written last is still line one.
            migrationBuilder.Sql("UPDATE dreams SET sort_order = -sort_order;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE dreams SET sort_order = -sort_order;");
        }
    }
}
