using EventService.Data;
using EventService.Domains;
using EventService.EventSourcing;
using EventService.EventSourcing.Events;

namespace EventService.Commands
{
    public class CreateDogadjajCommandHandler
    {
        private readonly EventDbContext _db;
        private readonly IEventStoreService _eventStore;

        public CreateDogadjajCommandHandler(EventDbContext db, IEventStoreService eventStore)
        {
            _db = db;
            _eventStore = eventStore;
        }

        public async Task<CommandResult> Handle(CreateDogadjajCommand command)
        {
            // Validacija PRE izmene stanja
            if (string.IsNullOrWhiteSpace(command.NazivDogadjaja))
                return CommandResult.Fail("Naziv dogadjaja je obavezan.");
            if (command.Cena < 0)
                return CommandResult.Fail("Cena ne moze biti negativna.");

            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var dogadjaj = new Dogadjaj
                {
                    NazivDogadjaja = command.NazivDogadjaja,
                    Agenda = command.Agenda,
                    Datum = command.Datum,
                    Trajanje = command.Trajanje,
                    Cena = command.Cena,
                    LokacijaId = command.LokacijaId,
                    VrstaId = command.VrstaId
                };
                _db.Dogadjaji.Add(dogadjaj);
                await _db.SaveChangesAsync();

                _db.OutboxMessages.Add(new OutboxMessage
                {
                    EventType = "DogadjajCreated",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new Shared.Events.DogadjajCreatedEvent
                    {
                        DogadjajId = dogadjaj.DogadjajId,
                        NazivDogadjaja = dogadjaj.NazivDogadjaja,
                        Datum = dogadjaj.Datum
                    }),
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                await _eventStore.AppendAsync(dogadjaj.DogadjajId, "DogadjajKreiran", new DogadjajKreiranEvent
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

                return CommandResult.Ok("Dogadjaj uspesno kreiran.", dogadjaj.DogadjajId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return CommandResult.Fail($"Greska prilikom kreiranja: {ex.Message}");
            }
        }
    }
}