using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aspire.Infrastructure;

/// <summary>
/// What `dotnet ef` builds the context from.
///
/// Migrations are always generated for Postgres, whatever the environment the
/// command happens to run in: the API's own startup picks SQLite in
/// Development, and a migration generated against SQLite would be one that
/// never runs where it matters. The connection string is never opened —
/// `migrations add` only needs the provider's model.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=aspire")
            .Options);
}
