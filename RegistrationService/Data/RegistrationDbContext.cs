using Microsoft.EntityFrameworkCore;
using RegistrationService.Domains;

namespace RegistrationService.Data
{
    public class RegistrationDbContext : DbContext
    {
        public RegistrationDbContext(DbContextOptions<RegistrationDbContext> options) : base(options) { }

        public DbSet<Predavac> Predavaci { get; set; }
        public DbSet<Prijava> Prijave { get; set; }
        public DbSet<DogadjajPredavac> DogadjajPredavaci { get; set; }
        public DbSet<DogadjajReference> DogadjajReference { get; set; }
        public DbSet<ProcessedMessage> ProcessedMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DogadjajReference>().HasIndex(x => x.DogadjajId).IsUnique();
            modelBuilder.Entity<ProcessedMessage>().HasIndex(x => x.EventId).IsUnique();
        }
    }
}