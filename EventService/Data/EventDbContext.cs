using EventService.Domains;
using Microsoft.EntityFrameworkCore;

namespace EventService.Data
{
    public class EventDbContext : DbContext
    {
        public EventDbContext(DbContextOptions<EventDbContext> options) : base(options) { }

        public DbSet<Dogadjaj> Dogadjaji { get; set; }
        public DbSet<VrstaDogadjaja> VrsteDogadjaja { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VrstaDogadjaja>().HasKey(v => v.VrstaId);
            modelBuilder.Entity<OutboxMessage>().HasIndex(x => x.CreatedAt);
        }
    }
}

