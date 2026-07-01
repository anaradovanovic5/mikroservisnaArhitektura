using EventService.Commands;
using EventService.Data;
using EventService.Domains;
using EventService.Queries;
using Microsoft.AspNetCore.Mvc;

namespace EventService.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class DogadjajiController : Controller
    {
        public DogadjajiController(
            EventDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            IRabbitMqRequestReplyClient requestReplyClient,
            CreateDogadjajCommandHandler createHandler,
            EditDogadjajCommandHandler editHandler,
            DeleteDogadjajCommandHandler deleteHandler,
            GetAllDogadjajiQueryHandler getAllHandler,
            GetDogadjajByIdQueryHandler getByIdHandler,
            FilterDogadjajiByVrstaQueryHandler filterHandler,
            GetDogadjajEventHistoryQueryHandler historyHandler,
            GetDogadjajCurrentStateQueryHandler currentStateHandler)
        {
            DbContext = dbContext;
            HttpClientFactory = httpClientFactory;
            RequestReplyClient = requestReplyClient;
            CreateHandler = createHandler;
            EditHandler = editHandler;
            DeleteHandler = deleteHandler;
            GetAllHandler = getAllHandler;
            GetByIdHandler = getByIdHandler;
            FilterHandler = filterHandler;
            HistoryHandler = historyHandler;
            CurrentStateHandler = currentStateHandler;
        }

        public EventDbContext DbContext { get; }
        public IHttpClientFactory HttpClientFactory { get; }
        public IRabbitMqRequestReplyClient RequestReplyClient { get; }
        public CreateDogadjajCommandHandler CreateHandler { get; }
        public EditDogadjajCommandHandler EditHandler { get; }
        public DeleteDogadjajCommandHandler DeleteHandler { get; }
        public GetAllDogadjajiQueryHandler GetAllHandler { get; }
        public GetDogadjajByIdQueryHandler GetByIdHandler { get; }
        public FilterDogadjajiByVrstaQueryHandler FilterHandler { get; }
        public GetDogadjajEventHistoryQueryHandler HistoryHandler { get; }
        public GetDogadjajCurrentStateQueryHandler CurrentStateHandler { get; }

        //CQRS upiti

        [HttpGet]
        public async Task<IActionResult> Index()
            => Ok(await GetAllHandler.Handle(new GetAllDogadjajiQuery()));

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var dto = await GetByIdHandler.Handle(new GetDogadjajByIdQuery { DogadjajId = id });
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpGet]
        public async Task<IActionResult> FilterByVrsta(int vrstaId)
            => Ok(await FilterHandler.Handle(new FilterDogadjajiByVrstaQuery { VrstaId = vrstaId }));

        //CQRS komande

        [HttpPost]
        public async Task<IActionResult> Create(Dogadjaj dogadjaj)
        {
            var rezultat = await CreateHandler.Handle(new CreateDogadjajCommand
            {
                NazivDogadjaja = dogadjaj.NazivDogadjaja,
                Agenda = dogadjaj.Agenda,
                Datum = dogadjaj.Datum,
                Trajanje = dogadjaj.Trajanje,
                Cena = dogadjaj.Cena,
                LokacijaId = dogadjaj.LokacijaId,
                VrstaId = dogadjaj.VrstaId
            });
            return rezultat.Success ? Ok(rezultat) : BadRequest(rezultat);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Dogadjaj dogadjaj)
        {
            var rezultat = await EditHandler.Handle(new EditDogadjajCommand
            {
                DogadjajId = dogadjaj.DogadjajId,
                NazivDogadjaja = dogadjaj.NazivDogadjaja,
                Agenda = dogadjaj.Agenda,
                Datum = dogadjaj.Datum,
                Trajanje = dogadjaj.Trajanje,
                Cena = dogadjaj.Cena,
                LokacijaId = dogadjaj.LokacijaId,
                VrstaId = dogadjaj.VrstaId
            });
            return rezultat.Success ? Ok(rezultat) : BadRequest(rezultat);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var rezultat = await DeleteHandler.Handle(new DeleteDogadjajCommand { DogadjajId = id });
            return rezultat.Success ? Ok(rezultat) : BadRequest(rezultat);
        }

        //Event Sourcing

        [HttpGet]
        public async Task<IActionResult> GetEventHistory(int dogadjajId)
            => Ok(await HistoryHandler.Handle(new GetDogadjajEventHistoryQuery { DogadjajId = dogadjajId }));

        [HttpGet]
        public async Task<IActionResult> GetReconstructedState(int dogadjajId)
            => Ok(await CurrentStateHandler.Handle(new GetDogadjajCurrentStateQuery { DogadjajId = dogadjajId }));


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

        [HttpGet]
        public async Task<IActionResult> GetBrojPrijava(int dogadjajId)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new { DogadjajId = dogadjajId });
            var odgovor = await RequestReplyClient.SendRequestAsync(payload, "dogadjaji.request", HttpContext.RequestAborted);

            if (odgovor == null)
                return StatusCode(504, "RegistrationService nije odgovorio na vreme.");

            return Ok(odgovor);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSaga(Dogadjaj dogadjaj)
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<SagaOrchestrator>();
            var (success, message, dogadjajId) = await orchestrator.ExecuteAsync(dogadjaj);

            if (!success)
                return StatusCode(500, new { Greska = message });

            return Ok(new { Poruka = message, DogadjajId = dogadjajId });
        }

        [HttpPost]
        public async Task<IActionResult> CreateSagaKoreografija(Dogadjaj dogadjaj)
        {
            DbContext.Dogadjaji.Add(dogadjaj);
            await DbContext.SaveChangesAsync();

            var sagaId = Guid.NewGuid();

            // upis početnog stanja saga procesa
            DbContext.SagaStates.Add(new SagaState
            {
                SagaId = sagaId,
                Status = "Started",
                DogadjajId = dogadjaj.DogadjajId,
                LokacijaId = dogadjaj.LokacijaId,
                CurrentStep = "dogadjaj.kreiran"
            });
            await DbContext.SaveChangesAsync();

            var ev = new Shared.Events.DogadjajKreiranSagaEvent
            {
                SagaId = sagaId,
                DogadjajId = dogadjaj.DogadjajId,
                NazivDogadjaja = dogadjaj.NazivDogadjaja,
                LokacijaId = dogadjaj.LokacijaId
            };

            var publisher = HttpContext.RequestServices.GetRequiredService<ISagaEventPublisher>();
            await publisher.PublishAsync("dogadjaj.kreiran", System.Text.Json.JsonSerializer.Serialize(ev), HttpContext.RequestAborted);

            Console.WriteLine($"[Saga Koreografija] DogadjajKreiranSagaEvent objavljen, SagaId={sagaId}, DogadjajId={dogadjaj.DogadjajId}");
            return Ok(new { Poruka = "Saga koreografija pokrenuta.", SagaId = sagaId, DogadjajId = dogadjaj.DogadjajId });
        }


    }
}