using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace KollectorScum.Api;

public partial class KollectorscumContext : DbContext
{
    public KollectorscumContext()
    {
    }

    public KollectorscumContext(DbContextOptions<KollectorscumContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MusicRelease> MusicReleases { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql("Name=ConnectionStrings:DefaultConnection");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MusicRelease>(entity =>
        {
            entity.HasIndex(e => e.ArtistId, "IX_MusicReleases_ArtistId");

            entity.HasIndex(e => e.CountryId, "IX_MusicReleases_CountryId");

            entity.HasIndex(e => e.FormatId, "IX_MusicReleases_FormatId");

            entity.HasIndex(e => e.GenreId, "IX_MusicReleases_GenreId");

            entity.HasIndex(e => e.LabelId, "IX_MusicReleases_LabelId");

            entity.HasIndex(e => e.PackagingId, "IX_MusicReleases_PackagingId");

            entity.HasIndex(e => e.ReleaseYear, "IX_MusicReleases_ReleaseYear");

            entity.HasIndex(e => e.StoreId, "IX_MusicReleases_StoreId");

            entity.HasIndex(e => e.Title, "IX_MusicReleases_Title");

            entity.HasIndex(e => e.UserId, "IX_MusicReleases_UserId");

            entity.HasIndex(e => new { e.UserId, e.DiscogsId }, "IX_MusicReleases_UserId_DiscogsId")
                .IsUnique()
                .HasFilter("(\"DiscogsId\" IS NOT NULL)");

            entity.Property(e => e.LabelNumber).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.Upc).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
