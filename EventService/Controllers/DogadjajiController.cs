using EventService.Data;
using EventService.Domains;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventService.Controllers
{
    public class DogadjajiController : Controller
    {
        public DogadjajiController(EventDbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            DbContext = dbContext;
            HttpClientFactory = httpClientFactory;
        }

        public EventDbContext DbContext { get; }
        public IHttpClientFactory HttpClientFactory { get; }

        public IActionResult Index()
        {
            var dogadjaji = DbContext.Dogadjaji
                .Include(d => d.VrstaDogadjaja)
                .ToList();
            return Ok(dogadjaji);
        }

        // Dohvata lokaciju iz LocationService (demonstracija Polly)
        public async Task<IActionResult> GetLokacija(int lokacijaId)
        {
            try
            {
                var client = HttpClientFactory.CreateClient("LocationService");
                var response = await client.GetAsync($"/Lokacije/Edit/{lokacijaId}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(content);
                }

                return StatusCode((int)response.StatusCode, "LocationService vratio grešku.");
            }
            catch (Exception ex)
            {
                // Circuit Breaker je otvoren ili Timeout je istekao
                return StatusCode(503, $"LocationService trenutno nedostupan: {ex.Message}");
            }
        }

        [HttpPost]
        public IActionResult Create(Dogadjaj dogadjaj)
        {
            DbContext.Dogadjaji.Add(dogadjaj);
            DbContext.SaveChanges();
            return Ok(dogadjaj);
        }

        public IActionResult Edit(int id)
        {
            var dogadjaj = DbContext.Dogadjaji.Find(id);
            if (dogadjaj == null) return NotFound();
            return Ok(dogadjaj);
        }

        [HttpPost]
        public IActionResult Edit(Dogadjaj dogadjaj)
        {
            var existing = DbContext.Dogadjaji.Find(dogadjaj.DogadjajId);
            if (existing == null) return NotFound();
            existing.NazivDogadjaja = dogadjaj.NazivDogadjaja;
            existing.Agenda = dogadjaj.Agenda;
            existing.Datum = dogadjaj.Datum;
            existing.Trajanje = dogadjaj.Trajanje;
            existing.Cena = dogadjaj.Cena;
            existing.LokacijaId = dogadjaj.LokacijaId;
            existing.VrstaId = dogadjaj.VrstaId;
            DbContext.SaveChanges();
            return Ok(existing);
        }

        public IActionResult Delete(int id)
        {
            var dogadjaj = DbContext.Dogadjaji.Find(id);
            if (dogadjaj == null) return NotFound();
            DbContext.Dogadjaji.Remove(dogadjaj);
            DbContext.SaveChanges();
            return Ok("Obrisano");
        }
    }
}
