using EventService.Data;
using EventService.EventSourcing;
using EventService.EventSourcing.Events;

namespace EventService.Commands
{
    public class EditDogadjajCommandHandler
    {
        private readonly EventDbContext _db;
        private readonly IEventStoreService _eventStore;

        public EditDogadjajCommandHandler(EventDbContext db, IEventStoreService eventStore)
        {
            _db = db;
            _eventStore = eventStore;
        }

        public async Task<CommandResult> Handle(EditDogadjajCommand command)
        {
            var existing = await _db.Dogadjaji.FindAsync(command.DogadjajId);
            if (existing == null)
                return CommandResult.Fail("Dogadjaj ne postoji.");
            if (existing.Otkazan)
                return CommandResult.Fail("Otkazan dogadjaj ne moze biti izmenjen.");
            if (string.IsNullOrWhiteSpace(command.NazivDogadjaja))
                return CommandResult.Fail("Naziv dogadjaja je obavezan.");
            if (command.Cena < 0)
                return CommandResult.Fail("Cena ne moze biti negativna.");

            // Generisem event SAMO za polje koje se stvarno promenilo
            if (existing.NazivDogadjaja != command.NazivDogadjaja)
            {
                await _eventStore.AppendAsync(existing.DogadjajId, "DogadjajNazivIzmenjen",
                    new DogadjajNazivIzmenjenEvent { NoviNaziv = command.NazivDogadjaja });
                existing.NazivDogadjaja = command.NazivDogadjaja;
            }
            if (existing.Cena != command.Cena)
            {
                await _eventStore.AppendAsync(existing.DogadjajId, "DogadjajCenaPromenjena",
                    new DogadjajCenaPromenjenaEvent { NovaCena = command.Cena });
                existing.Cena = command.Cena;
            }
            if (existing.Datum != command.Datum)
            {
                await _eventStore.AppendAsync(existing.DogadjajId, "DogadjajTerminPromenjen",
                    new DogadjajTerminPromenjenEvent { NoviDatum = command.Datum });
                existing.Datum = command.Datum;
            }

            existing.Agenda = command.Agenda;
            existing.Trajanje = command.Trajanje;
            existing.LokacijaId = command.LokacijaId;
            existing.VrstaId = command.VrstaId;
            await _db.SaveChangesAsync();

            return CommandResult.Ok("Dogadjaj uspesno izmenjen.", existing.DogadjajId);
        }
    }
}