using Anidow.Database.Models;
using Anidow.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Anidow.Database
{
    public class TrackContext : DbContext
    {
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

        }
    }
}