using DogadjajiApp.Domains;
using Microsoft.EntityFrameworkCore;

namespace DogadjajiApp.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        protected AppDbContext()
        {
        }

        public DbSet<Dogadjaj> Dogadjaji { get; set; }
        public DbSet<Lokacija> Lokacije { get; set; }
        public DbSet<Predavac> Predavaci { get; set; }
        public DbSet<VrstaDogadjaja> VrsteDogadjaja { get; set; }
        public DbSet<DogadjajPredavac> DogadjajPredavaci { get; set; }
        public DbSet<Prijava> Prijave { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VrstaDogadjaja>()
                .HasKey(v => v.VrstaId);

            modelBuilder.Entity<Lokacija>()
                .HasKey(l => l.LokacijaId);

            modelBuilder.Entity<Predavac>()
                .HasKey(p => p.PredavacId);

            modelBuilder.Entity<Dogadjaj>()
                .HasKey(d => d.DogadjajId);
        }
    }
}
