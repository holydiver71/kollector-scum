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
        /// Gets or sets the Kollections DbSet
        /// </summary>
        public DbSet<Models.Kollection> Kollections { get; set; }

        /// <summary>
        /// Gets or sets the KollectionGenres DbSet
        /// </summary>
        public DbSet<Models.KollectionGenre> KollectionGenres { get; set; }

        /// <summary>
        /// Gets or sets the Lists DbSet
        /// </summary>
        public DbSet<Models.List> Lists { get; set; }

        /// <summary>
        /// Gets or sets the ListReleases DbSet
        /// </summary>
        public DbSet<Models.ListRelease> ListReleases { get; set; }

        /// <summary>
        /// Gets or sets the ApplicationUsers DbSet
        /// </summary>
        public DbSet<Models.ApplicationUser> ApplicationUsers { get; set; }

        /// <summary>
        /// Gets or sets the UserProfiles DbSet
        /// </summary>
        public DbSet<Models.UserProfile> UserProfiles { get; set; }

        /// <summary>
        /// Gets or sets the UserInvitations DbSet
        /// </summary>
        public DbSet<Models.UserInvitation> UserInvitations { get; set; }

        /// <summary>
        /// Configure the database model and relationships
        /// </summary>
        /// <param name="modelBuilder">The model builder</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure indexes for performance
            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.UserId)
                .HasDatabaseName("IX_MusicReleases_UserId");

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.Title)
                .HasDatabaseName("IX_MusicReleases_Title");

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.ReleaseYear)
                .HasDatabaseName("IX_MusicReleases_ReleaseYear");

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.DateAdded)
                .HasDatabaseName("IX_MusicReleases_DateAdded");

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => mr.OrigReleaseYear)
                .HasDatabaseName("IX_MusicReleases_OrigReleaseYear");

            // Composite indexes for common query patterns
            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => new { mr.UserId, mr.DateAdded })
                .HasDatabaseName("IX_MusicReleases_UserId_DateAdded");

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => new { mr.UserId, mr.Title })
                .HasDatabaseName("IX_MusicReleases_UserId_Title");

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

            modelBuilder.Entity<Models.MusicRelease>()
                .HasIndex(mr => new { mr.UserId, mr.DiscogsId })
                .IsUnique()
                .HasFilter("\"DiscogsId\" IS NOT NULL")
                .HasDatabaseName("IX_MusicReleases_UserId_DiscogsId");

            // Configure lookup entity indexes and unique constraints
            modelBuilder.Entity<Models.Artist>()
                .HasIndex(a => a.UserId)
                .HasDatabaseName("IX_Artists_UserId");

            modelBuilder.Entity<Models.Artist>()
                .HasIndex(a => new { a.UserId, a.Name })
                .IsUnique()
                .HasDatabaseName("IX_Artists_UserId_Name");

            modelBuilder.Entity<Models.Genre>()
                .HasIndex(g => g.UserId)
                .HasDatabaseName("IX_Genres_UserId");

            modelBuilder.Entity<Models.Genre>()
                .HasIndex(g => new { g.UserId, g.Name })
                .IsUnique()
                .HasDatabaseName("IX_Genres_UserId_Name");

            modelBuilder.Entity<Models.Label>()
                .HasIndex(l => l.UserId)
                .HasDatabaseName("IX_Labels_UserId");

            modelBuilder.Entity<Models.Label>()
                .HasIndex(l => new { l.UserId, l.Name })
                .IsUnique()
                .HasDatabaseName("IX_Labels_UserId_Name");

            modelBuilder.Entity<Models.Country>()
                .HasIndex(c => c.UserId)
                .HasDatabaseName("IX_Countries_UserId");

            modelBuilder.Entity<Models.Country>()
                .HasIndex(c => new { c.UserId, c.Name })
                .IsUnique()
                .HasDatabaseName("IX_Countries_UserId_Name");

            modelBuilder.Entity<Models.Format>()
                .HasIndex(f => f.UserId)
                .HasDatabaseName("IX_Formats_UserId");

            modelBuilder.Entity<Models.Format>()
                .HasIndex(f => new { f.UserId, f.Name })
                .IsUnique()
                .HasDatabaseName("IX_Formats_UserId_Name");

            modelBuilder.Entity<Models.Packaging>()
                .HasIndex(p => p.UserId)
                .HasDatabaseName("IX_Packagings_UserId");

            modelBuilder.Entity<Models.Packaging>()
                .HasIndex(p => new { p.UserId, p.Name })
                .IsUnique()
                .HasDatabaseName("IX_Packagings_UserId_Name");

            modelBuilder.Entity<Models.Store>()
                .HasIndex(s => s.UserId)
                .HasDatabaseName("IX_Stores_UserId");

            modelBuilder.Entity<Models.Store>()
                .HasIndex(s => new { s.UserId, s.Name })
                .IsUnique()
                .HasDatabaseName("IX_Stores_UserId_Name");

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

            // Configure Kollection relationships
            modelBuilder.Entity<Models.KollectionGenre>()
                .HasKey(kg => new { kg.KollectionId, kg.GenreId });

            modelBuilder.Entity<Models.KollectionGenre>()
                .HasOne(kg => kg.Kollection)
                .WithMany(k => k.KollectionGenres)
                .HasForeignKey(kg => kg.KollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.KollectionGenre>()
                .HasOne(kg => kg.Genre)
                .WithMany()
                .HasForeignKey(kg => kg.GenreId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.Kollection>()
                .HasIndex(k => k.UserId)
                .HasDatabaseName("IX_Kollections_UserId");

            modelBuilder.Entity<Models.Kollection>()
                .HasIndex(k => new { k.UserId, k.Name })
                .IsUnique()
                .HasDatabaseName("IX_Kollections_UserId_Name");

            // Configure List relationships
            modelBuilder.Entity<Models.List>()
                .HasIndex(l => l.UserId)
                .HasDatabaseName("IX_Lists_UserId");

            modelBuilder.Entity<Models.List>()
                .HasIndex(l => l.Name)
                .HasDatabaseName("IX_Lists_Name");

            modelBuilder.Entity<Models.ListRelease>()
                .HasIndex(lr => new { lr.ListId, lr.ReleaseId })
                .IsUnique()
                .HasDatabaseName("IX_ListReleases_ListId_ReleaseId");

            modelBuilder.Entity<Models.ListRelease>()
                .HasOne(lr => lr.List)
                .WithMany(l => l.ListReleases)
                .HasForeignKey(lr => lr.ListId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.ListRelease>()
                .HasOne(lr => lr.Release)
                .WithMany()
                .HasForeignKey(lr => lr.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure ApplicationUser relationships
            modelBuilder.Entity<Models.ApplicationUser>()
                .HasIndex(u => u.GoogleSub)
                .IsUnique()
                .HasDatabaseName("IX_ApplicationUsers_GoogleSub");

            modelBuilder.Entity<Models.ApplicationUser>()
                .HasIndex(u => u.Email)
                .HasDatabaseName("IX_ApplicationUsers_Email");

            // Configure UserProfile relationships
            modelBuilder.Entity<Models.UserProfile>()
                .HasIndex(up => up.UserId)
                .IsUnique()
                .HasDatabaseName("IX_UserProfiles_UserId");

            modelBuilder.Entity<Models.UserProfile>()
                .HasOne(up => up.User)
                .WithOne(u => u.UserProfile)
                .HasForeignKey<Models.UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Models.UserProfile>()
                .HasOne(up => up.SelectedKollection)
                .WithMany()
                .HasForeignKey(up => up.SelectedKollectionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure UserInvitation relationships
            modelBuilder.Entity<Models.UserInvitation>()
                .HasIndex(ui => ui.Email)
                .IsUnique()
                .HasDatabaseName("IX_UserInvitations_Email");

            modelBuilder.Entity<Models.UserInvitation>()
                .HasIndex(ui => ui.CreatedAt)
                .HasDatabaseName("IX_UserInvitations_CreatedAt");
        }
    }
}
