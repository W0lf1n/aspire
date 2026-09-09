using System.Text;
using Aspire.Domain;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Dream> Dreams => Set<Dream>();
    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Dream>(entity =>
        {
            entity.ToTable("dreams");
            entity.HasKey(d => d.Id);

            // The board reads in this order and nothing else reads at all.
            entity.HasIndex(d => d.SortOrder);

            entity.Property(d => d.Title).HasMaxLength(Dream.TitleMaxLength);
            entity.Property(d => d.Why).HasMaxLength(Dream.WhyMaxLength);
            entity.Property(d => d.Status)
                .HasConversion(s => DreamStatusNames.ToWire(s), s => DreamStatusNames.Parse(s))
                .HasMaxLength(16);
        });

        model.Entity<Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.Name).HasMaxLength(Device.NameMaxLength);
            entity.Property(e => e.TokenHash).HasMaxLength(88);
        });

        // Columns are snake_case, like the tables and like PLAN.md §5. EF's
        // default takes the property name verbatim, and Postgres folds unquoted
        // identifiers to lowercase — so `select title from dreams` would fail
        // against a column called "Title". Prosper lives with that; a fresh
        // schema does not have to.
        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(SnakeCase(property.Name));
            }
        }
    }

    private static string SnakeCase(string name)
    {
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
