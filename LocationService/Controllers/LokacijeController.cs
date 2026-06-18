using LocationService.Data;
using LocationService.Domains;
using Microsoft.AspNetCore.Mvc;

namespace LocationService.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class LokacijeController : Controller
    {
        public LokacijeController(LocationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public LocationDbContext DbContext { get; }

        [HttpGet]
        public IActionResult Index()
        {
            var lokacije = DbContext.Lokacije.ToList();
            return Ok(lokacije);
        }

        [HttpPost]
        public IActionResult Create(Lokacija lokacija)
        {
            DbContext.Lokacije.Add(lokacija);
            DbContext.SaveChanges();
            return Ok(lokacija);
        }

        [HttpGet]
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

        [HttpGet]
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