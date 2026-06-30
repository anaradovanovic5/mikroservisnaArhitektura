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

        // SAGA — korak 2: rezervisi lokaciju
        [HttpPost]
        public IActionResult Rezervisi([FromBody] RezervacijaRequest request)
        {
            var lokacija = DbContext.Lokacije.Find(request.LokacijaId);
            if (lokacija == null)
                return NotFound($"Lokacija {request.LokacijaId} ne postoji.");

            lokacija.BrojRezervacija += 1;
            DbContext.SaveChanges();

            Console.WriteLine($"[Saga] Lokacija {request.LokacijaId} rezervisana za DogadjajId={request.DogadjajId} (BrojRezervacija={lokacija.BrojRezervacija})");
            return Ok(new { Poruka = "Lokacija rezervisana.", request.LokacijaId });
        }

        // SAGA — kompenzacija koraka 2: oslobodi rezervaciju
        [HttpPost]
        public IActionResult OslobodiRezervaciju([FromBody] RezervacijaRequest request)
        {
            var lokacija = DbContext.Lokacije.Find(request.LokacijaId);
            if (lokacija != null && lokacija.BrojRezervacija > 0)
            {
                lokacija.BrojRezervacija -= 1;
                DbContext.SaveChanges();
            }

            Console.WriteLine($"[Saga][Kompenzacija] Lokacija {request.LokacijaId} oslobodjena za DogadjajId={request.DogadjajId} (BrojRezervacija={lokacija?.BrojRezervacija})");
            return Ok(new { Poruka = "Rezervacija oslobodjena.", request.LokacijaId });
        }
    }

    public class RezervacijaRequest
    {
        public int LokacijaId { get; set; }
        public int DogadjajId { get; set; }
    }
}