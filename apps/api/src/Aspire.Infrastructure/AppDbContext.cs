using System.Text;
using Aspire.Domain;
using Microsoft.EntityFrameworkCore;

namespace Aspire.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Dream> Dreams => Set<Dream>();
    public DbSet<Device> Devices => Set<Device>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Board>(entity =>
        {
            entity.ToTable("boards");
            entity.HasKey(b => b.Id);
            // The operator addresses a board by name (`board code <name>`).
            entity.HasIndex(b => b.Name).IsUnique();
            entity.Property(b => b.Id).HasMaxLength(64);
            entity.Property(b => b.Name).HasMaxLength(Board.NameMaxLength);
            entity.Property(b => b.CodeHash).HasMaxLength(Board.CodeHashMaxLength);
        });

        model.Entity<Dream>(entity =>
        {
            entity.ToTable("dreams");
            entity.HasKey(d => d.Id);

            // The board reads in this order, inside its own board, and
            // nothing else reads at all.
            entity.HasIndex(d => new { d.BoardId, d.SortOrder });

            entity.Property(d => d.BoardId).HasMaxLength(64);
            entity.Property(d => d.Title).HasMaxLength(Dream.TitleMaxLength);
            entity.Property(d => d.Why).HasMaxLength(Dream.WhyMaxLength);
            entity.Property(d => d.Status)
                .HasConversion(s => DreamStatusNames.ToWire(s), s => DreamStatusNames.Parse(s))
                .HasMaxLength(16);

            // A board that goes takes its dreams with it; there is nowhere
            // else for them to be.
            entity.HasOne<Board>()
                .WithMany()
                .HasForeignKey(d => d.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.BoardId);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.BoardId).HasMaxLength(64);
            entity.Property(e => e.Name).HasMaxLength(Device.NameMaxLength);
            entity.Property(e => e.TokenHash).HasMaxLength(88);

            entity.HasOne<Board>()
                .WithMany()
                .HasForeignKey(e => e.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
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
