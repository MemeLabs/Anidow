using System;
using Anidow.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Anidow.Database
{
    public class TrackContext : DbContext
    {
        public DbSet<AppState> AppStates { get; set; }
        public DbSet<Anime> Anime { get; set; }
        public DbSet<Episode> Episodes { get; set; }
        public DbSet<Cover> Covers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=anime.db");
            optionsBuilder.UseLazyLoadingProxies();
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppState>()
                        .Property(b => b.Created)
                        .HasDefaultValue(DateTime.UtcNow);
        }
    }
}