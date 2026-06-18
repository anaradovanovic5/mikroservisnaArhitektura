using EventService.Data;
using EventService.Domains;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventService.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class DogadjajiController : Controller
    {
        public DogadjajiController(EventDbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            DbContext = dbContext;
            HttpClientFactory = httpClientFactory;
        }

        public EventDbContext DbContext { get; }
        public IHttpClientFactory HttpClientFactory { get; }

        [HttpGet]
        public IActionResult Index()
        {
            var dogadjaji = DbContext.Dogadjaji
                .Include(d => d.VrstaDogadjaja)
                .ToList();
            return Ok(dogadjaji);
        }

        // Dohvata lokaciju iz LocationService (demonstracija Polly)
        [HttpGet]
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
        public async Task<IActionResult> Create(Dogadjaj dogadjaj)
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                DbContext.Dogadjaji.Add(dogadjaj);
                await DbContext.SaveChangesAsync();

                var outboxMessage = new OutboxMessage
                {
                    EventType = "DogadjajCreated",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new Shared.Events.DogadjajCreatedEvent
                    {
                        DogadjajId = dogadjaj.DogadjajId,
                        NazivDogadjaja = dogadjaj.NazivDogadjaja,
                        Datum = dogadjaj.Datum
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                DbContext.OutboxMessages.Add(outboxMessage);
                await DbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(dogadjaj);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Greška: {ex.Message}");
            }
        }

        [HttpGet]
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

        [HttpGet]
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