using DogadjajiApp.Data;
using DogadjajiApp.Domains;
using Microsoft.AspNetCore.Mvc;

namespace DogadjajiApp.Controllers
{
    public class LokacijeController : Controller
    {
        public LokacijeController(AppDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public AppDbContext DbContext { get; }

        public IActionResult Index()
        {
            var lokacije = DbContext.Lokacije.ToList();
            return Ok(lokacije);
        }

        public IActionResult Create()
        {
            return Ok("Forma za kreiranje lokacije");
        }

        [HttpPost]
        public IActionResult Create(Lokacija lokacija)
        {
            DbContext.Lokacije.Add(lokacija);
            DbContext.SaveChanges();
            return Ok(lokacija);
        }

        public IActionResult Edit(int id)
        {
            var lokacija = DbContext.Lokacije.Find(id);
            if (lokacija == null) return NotFound();
            return Ok(lokacija);
        }

        [HttpPost]
        public IActionResult Edit(Lokacija lokacija)
        {
            var existing = DbContext.Lokacije.Find(lokacija.LokacijaId);
            if (existing == null) return NotFound();

            existing.NazivLokacije = lokacija.NazivLokacije;
            existing.Adresa = lokacija.Adresa;
            existing.Kapacitet = lokacija.Kapacitet;

            DbContext.SaveChanges();
            return Ok(existing);
        }

        public IActionResult Delete(int id)
        {
            var lokacija = DbContext.Lokacije.Find(id);
            if (lokacija == null) return NotFound();

            DbContext.Lokacije.Remove(lokacija);
            DbContext.SaveChanges();
            return Ok("Obrisano");
        }
    }
}