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
        public DbSet<SagaState> SagaStates { get; set; }
        public DbSet<EventStoreEntry> EventStoreEntries { get; set; }
        public DbSet<EventSourcing.DogadjajSnapshot> DogadjajSnapshots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VrstaDogadjaja>().HasKey(v => v.VrstaId);
            modelBuilder.Entity<OutboxMessage>().HasIndex(x => x.CreatedAt);
            modelBuilder.Entity<SagaState>().HasIndex(x => x.SagaId);
            modelBuilder.Entity<EventStoreEntry>().HasIndex(x => x.AggregateId);
        }
    }
}