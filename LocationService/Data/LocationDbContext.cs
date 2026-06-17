using LocationService.Domains;
using Microsoft.EntityFrameworkCore;

namespace LocationService.Data
{
    public class LocationDbContext : DbContext
    {
        public LocationDbContext(DbContextOptions options) : base(options) { }
        protected LocationDbContext() { }

        public DbSet<Lokacija> Lokacije { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Lokacija>().HasKey(l => l.LokacijaId);
        }
    }
}
