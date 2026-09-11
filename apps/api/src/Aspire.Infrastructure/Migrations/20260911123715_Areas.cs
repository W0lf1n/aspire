using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aspire.Infrastructure.Migrations
{
    /// <summary>
    /// The areas become want · be · do (D43). No schema change — the column
    /// was always one lower-case word — but every word already in it is one
    /// of D32's nine, which this build cannot read: <c>DreamCategoryNames.Parse</c>
    /// throws on it and the board endpoint 500s on the row.
    ///
    /// So the words go rather than being guessed at. Nine areas do not map
    /// onto three without inventing an answer the person never gave, and a
    /// dream showing an area it was never put in is worse than a dream
    /// showing none: the field is optional, and „no area" is a state the
    /// board already knows how to draw.
    /// </summary>
    public partial class Areas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE dreams SET category = NULL " +
                "WHERE category IS NOT NULL AND category NOT IN ('want', 'be', 'do');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to put back: the words this cleared are not recorded
            // anywhere else. An empty column is legal in either direction.
        }
    }
}
