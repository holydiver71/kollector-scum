using Microsoft.EntityFrameworkCore;

namespace KollectorScrum.Api.Data
{
    /// <summary>
    /// Entity Framework Core database context for Kollector Scrum
    /// </summary>
    public class KollectorScrumDbContext : DbContext
    {
        public KollectorScrumDbContext(DbContextOptions<KollectorScrumDbContext> options)
            : base(options)
        {
        }

        // TODO: Add DbSet properties for your entities, e.g.:
        // public DbSet<Artist> Artists { get; set; }
    }
}
