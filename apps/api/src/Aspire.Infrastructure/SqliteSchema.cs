using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Infrastructure;

/// <summary>
/// Whether the laptop's database file can still answer for the model (D5,
/// D55).
///
/// SQLite mode creates the schema with <c>EnsureCreated</c> — migrations are
/// written for Npgsql and are not run here — and <c>EnsureCreated</c> makes a
/// schema once and never touches it again. So the morning after a model gains
/// a column, the file is a column short, the API starts perfectly well, and
/// the first request for a dream comes back „no such column: d.focus_rank“ as
/// a 500. From a screen that is nothing but „Server odpověděl 500“, which is
/// a bad way to find out.
///
/// This asks every table for one row before the server serves anything, which
/// makes SQLite prepare a statement naming every column the model expects. A
/// file that is behind says so here, at start, once, with the fix in the same
/// sentence — the same bargain as <c>MediaStore.EnsureWritable</c> (D26): the
/// API proves it can do the thing before it promises to.
/// </summary>
public static class SqliteSchema
{
    /// <summary>
    /// What SQLite complained about, or null when the file has everything the
    /// model asks for. An empty table is a fine answer: the statement is
    /// prepared either way, which is the whole of the check.
    /// </summary>
    public static async Task<string?> BehindTheModelAsync(AppDbContext db, CancellationToken ct = default)
    {
        try
        {
            await db.Boards.AsNoTracking().FirstOrDefaultAsync(ct);
            await db.Dreams.AsNoTracking().FirstOrDefaultAsync(ct);
            await db.DreamImages.AsNoTracking().FirstOrDefaultAsync(ct);
            await db.Devices.AsNoTracking().FirstOrDefaultAsync(ct);
            await db.PushSubscriptions.AsNoTracking().FirstOrDefaultAsync(ct);
            return null;
        }
        catch (SqliteException e)
        {
            return e.Message;
        }
    }

    /// <summary>
    /// The file a connection string points at, for an error message that can
    /// be acted on rather than one that says „the database“.
    /// </summary>
    public static string FileOf(string? connectionString)
    {
        try
        {
            var path = new SqliteConnectionStringBuilder(connectionString).DataSource;
            return string.IsNullOrWhiteSpace(path) ? "aspire.db" : Path.GetFullPath(path);
        }
        catch (ArgumentException)
        {
            return "aspire.db";
        }
    }
}
