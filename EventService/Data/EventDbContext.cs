using EventService.Domains;
using Microsoft.EntityFrameworkCore;

namespace EventService.Data
{
    public class EventDbContext : DbContext
    {
        public EventDbContext(DbContextOptions options) : base(options) { }
        protected EventDbContext() { }

        public DbSet<Dogadjaj> Dogadjaji { get; set; }
        public DbSet<VrstaDogadjaja> VrsteDogadjaja { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Dogadjaj>().HasKey(d => d.DogadjajId);
            modelBuilder.Entity<VrstaDogadjaja>().HasKey(v => v.VrstaId);
        }
    }
}

