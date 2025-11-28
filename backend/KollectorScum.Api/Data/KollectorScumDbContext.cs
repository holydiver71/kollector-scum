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

        /// <summary>
        /// Gets or sets the Countries DbSet
        /// </summary>
        public DbSet<Models.Country> Countries { get; set; }

        /// <summary>
        /// Gets or sets the Stores DbSet
        /// </summary>
        public DbSet<Models.Store> Stores { get; set; }

        /// <summary>
        /// Gets or sets the Formats DbSet
        /// </summary>
        public DbSet<Models.Format> Formats { get; set; }

        /// <summary>
        /// Gets or sets the Genres DbSet
        /// </summary>
        public DbSet<Models.Genre> Genres { get; set; }

        /// <summary>
        /// Gets or sets the Labels DbSet
        /// </summary>
        public DbSet<Models.Label> Labels { get; set; }

        /// <summary>
        /// Gets or sets the Artists DbSet
        /// </summary>
        public DbSet<Models.Artist> Artists { get; set; }

        /// <summary>
        /// Gets or sets the Packagings DbSet
        /// </summary>
        public DbSet<Models.Packaging> Packagings { get; set; }

        /// <summary>
        /// Gets or sets the MusicReleases DbSet
        /// </summary>
        public DbSet<Models.MusicRelease> MusicReleases { get; set; }

        /// <summary>
        /// Gets or sets the NowPlaying DbSet
        /// </summary>
        public DbSet<Models.NowPlaying> NowPlayings { get; set; }

        /// <summary>
        /// Configure the database model and relationships
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure indexes for performance
            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.Title)
                .HasDatabaseName("IX_MusicReleases_Title");

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.ReleaseYear)
                .HasDatabaseName("IX_MusicReleases_ReleaseYear");

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.LabelId)
                .HasDatabaseName("IX_MusicReleases_LabelId");

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.CountryId)
                .HasDatabaseName("IX_MusicReleases_CountryId");

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.FormatId)
                .HasDatabaseName("IX_MusicReleases_FormatId");

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.PackagingId)
                .HasDatabaseName("IX_MusicReleases_PackagingId");

            // Configure foreign key relationships
            modelBuilder.Entity<Models.MusicRelease>()
                .HasOne(mr => mr.Label)
                .WithMany(l => l.MusicReleases)
                .HasForeignKey(mr => mr.LabelId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.MusicRelease>()
                .HasOne(mr => mr.Country)
                .WithMany(c => c.MusicReleases)
                .HasForeignKey(mr => mr.CountryId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.MusicRelease>()
                .HasOne(mr => mr.Format)
                .WithMany(f => f.MusicReleases)
                .HasForeignKey(mr => mr.FormatId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Models.MusicRelease>()
                .HasOne(mr => mr.Packaging)
                .WithMany(p => p.MusicReleases)
                .HasForeignKey(mr => mr.PackagingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure NowPlaying relationships
            modelBuilder.Entity<Models.NowPlaying>()
                .HasIndex(np => np.MusicReleaseId)
                .HasDatabaseName("IX_NowPlayings_MusicReleaseId");

            modelBuilder.Entity<Models.NowPlaying>()
                .HasIndex(np => np.PlayedAt)
                .HasDatabaseName("IX_NowPlayings_PlayedAt");

            modelBuilder.Entity<Models.NowPlaying>()
                .HasOne(np => np.MusicRelease)
                .WithMany()
                .HasForeignKey(np => np.MusicReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
