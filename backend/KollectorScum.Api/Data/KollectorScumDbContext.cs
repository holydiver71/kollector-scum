using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api.Data
{
    /// <summary>
    /// Entity Framework Core database context for Kollector Scum
    /// </summary>
    public class KollectorScumDbContext : DbContext
    {
        public KollectorScumDbContext(DbContextOptions<KollectorScumDbContext> options)
            : base(options)
        {
        }

        // TODO: Add DbSet properties for your entities, e.g.:
        // public DbSet<Artist> Artists { get; set; }
    }
}
