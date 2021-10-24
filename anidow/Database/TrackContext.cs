using System;
using Anidow.Database.Models;
using Anidow.GraphQL;
using Microsoft.EntityFrameworkCore;

namespace Anidow.Database;

public class TrackContext : DbContext
{
    public DbSet<AppState> AppStates { get; set; }
    public DbSet<Anime> Anime { get; set; }
    public DbSet<Episode> Episodes { get; set; }
    public DbSet<Cover> Covers { get; set; }
    public DbSet<AniListAnime> AniListAnime { get; set; }

    public DbSet<NotifyItem> NotifyItems { get; set; }
    public DbSet<NotifyItemMatch> NotifyItemMatches { get; set; }
    public DbSet<NotifyItemKeyword> NotifyItemKeywords { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=anime.db");
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotifyItemMatch>()
                    .Property(p => p.Keywords)
                    .HasConversion(
                        v => string.Join("\0", v),
                        v => v.Split("\0", StringSplitOptions.RemoveEmptyEntries));
        modelBuilder.Entity<AniListAnime>()
                    .Property(p => p.Genres)
                    .HasConversion(
                        v => string.Join("\0", v),
                        v => v.Split("\0", StringSplitOptions.RemoveEmptyEntries));
    }
}