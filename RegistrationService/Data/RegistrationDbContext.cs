using Microsoft.EntityFrameworkCore;
using RegistrationService.Domains;

namespace RegistrationService.Data
{
    public class RegistrationDbContext : DbContext
    {
        public RegistrationDbContext(DbContextOptions options) : base(options) { }
        protected RegistrationDbContext() { }

        public DbSet<Predavac> Predavaci { get; set; }
        public DbSet<Prijava> Prijave { get; set; }
        public DbSet<DogadjajPredavac> DogadjajPredavaci { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Predavac>().HasKey(p => p.PredavacId);

            modelBuilder.Entity<DogadjajPredavac>().HasKey(dp => dp.Id);
        }
    }
}
