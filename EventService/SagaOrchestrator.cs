using EventService.Data;
using EventService.Domains;

namespace EventService
{
    public class SagaOrchestrator
    {
        private readonly EventDbContext _db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SagaOrchestrator> _logger;

        public SagaOrchestrator(EventDbContext db, IHttpClientFactory httpClientFactory, ILogger<SagaOrchestrator> logger)
        {
            _db = db;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, int? DogadjajId)> ExecuteAsync(Dogadjaj dogadjaj)
        {
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                Status = "Started",
                LokacijaId = dogadjaj.LokacijaId
            };
            _db.SagaStates.Add(saga);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[Saga {SagaId}] Pokrenuta.", saga.SagaId);

            // KORAK 1 — kreiraj dogadjaj
            saga.CurrentStep = "KreiranjeDogadjaja";
            saga.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            try
            {
                _db.Dogadjaji.Add(dogadjaj);
                await _db.SaveChangesAsync();
                saga.DogadjajId = dogadjaj.DogadjajId;
                await _db.SaveChangesAsync();
                _logger.LogInformation("[Saga {SagaId}] Korak 1 OK — DogadjajId={Id}", saga.SagaId, dogadjaj.DogadjajId);
            }
            catch (Exception ex)
            {
                saga.Status = "Failed";
                saga.ErrorMessage = $"Korak 1 pao: {ex.Message}";
                saga.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                _logger.LogError("[Saga {SagaId}] Korak 1 PAO: {Msg}", saga.SagaId, ex.Message);
                return (false, saga.ErrorMessage, null);
            }

            // KORAK 2 — rezervisi lokaciju u LocationService
            saga.CurrentStep = "RezervacijaLokacije";
            saga.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            try
            {
                var locationClient = _httpClientFactory.CreateClient("LocationService");
                var response = await locationClient.PostAsJsonAsync("/Lokacije/Rezervisi", new
                {
                    LokacijaId = dogadjaj.LokacijaId,
                    DogadjajId = dogadjaj.DogadjajId
                });

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"LocationService vratio {response.StatusCode}");

                _logger.LogInformation("[Saga {SagaId}] Korak 2 OK — lokacija rezervisana.", saga.SagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError("[Saga {SagaId}] Korak 2 PAO — kompenziram.", saga.SagaId);
                saga.Status = "Compensating";
                saga.ErrorMessage = $"Korak 2 pao: {ex.Message}";
                saga.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // KOMPENZACIJA — obrisi dogadjaj
                await KompenzujKorak1Async(saga, dogadjaj);
                return (false, saga.ErrorMessage, null);
            }

            // KORAK 3 — kreiraj inicijalnu prijavu u RegistrationService
            saga.CurrentStep = "KreiranjePrijave";
            saga.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            try
            {
                var registrationClient = _httpClientFactory.CreateClient("RegistrationService");
                var response = await registrationClient.PostAsJsonAsync("/Prijave/KreirajInicijalno", new
                {
                    DogadjajId = dogadjaj.DogadjajId,
                    NazivDogadjaja = dogadjaj.NazivDogadjaja
                });

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"RegistrationService vratio {response.StatusCode}");

                _logger.LogInformation("[Saga {SagaId}] Korak 3 OK — prijava kreirana.", saga.SagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError("[Saga {SagaId}] Korak 3 PAO — kompenziram.", saga.SagaId);
                saga.Status = "Compensating";
                saga.ErrorMessage = $"Korak 3 pao: {ex.Message}";
                saga.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // KOMPENZACIJA — oslobodi lokaciju i obrisi dogadjaj
                await KompenzujKorak2Async(saga, dogadjaj);
                await KompenzujKorak1Async(saga, dogadjaj);
                return (false, saga.ErrorMessage, null);
            }

            // SVE PROSLO
            saga.Status = "Completed";
            saga.CurrentStep = "Zavrseno";
            saga.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("[Saga {SagaId}] Kompletirana uspesno.", saga.SagaId);
            return (true, "Saga uspesno zavrsena.", dogadjaj.DogadjajId);
        }

        private async Task KompenzujKorak1Async(SagaState saga, Dogadjaj dogadjaj)
        {
            try
            {
                var existing = await _db.Dogadjaji.FindAsync(dogadjaj.DogadjajId);
                if (existing != null)
                {
                    _db.Dogadjaji.Remove(existing);
                    await _db.SaveChangesAsync();
                }
                _logger.LogInformation("[Saga {SagaId}] Kompenzacija K1 — dogadjaj obrisan.", saga.SagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError("[Saga {SagaId}] Kompenzacija K1 PALA: {Msg}", saga.SagaId, ex.Message);
            }

            saga.Status = "Failed";
            saga.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        private async Task KompenzujKorak2Async(SagaState saga, Dogadjaj dogadjaj)
        {
            try
            {
                var locationClient = _httpClientFactory.CreateClient("LocationService");
                await locationClient.PostAsJsonAsync("/Lokacije/OslobodiRezervaciju", new
                {
                    LokacijaId = dogadjaj.LokacijaId,
                    DogadjajId = dogadjaj.DogadjajId
                });
                _logger.LogInformation("[Saga {SagaId}] Kompenzacija K2 — lokacija oslobodjena.", saga.SagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError("[Saga {SagaId}] Kompenzacija K2 PALA: {Msg}", saga.SagaId, ex.Message);
            }
        }
    }
}

