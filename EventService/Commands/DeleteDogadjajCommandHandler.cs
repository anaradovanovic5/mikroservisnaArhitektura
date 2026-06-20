using EventService.Data;
using EventService.EventSourcing;
using EventService.EventSourcing.Events;

namespace EventService.Commands
{
    public class DeleteDogadjajCommandHandler
    {
        private readonly EventDbContext _db;
        private readonly IEventStoreService _eventStore;

        public DeleteDogadjajCommandHandler(EventDbContext db, IEventStoreService eventStore)
        {
            _db = db;
            _eventStore = eventStore;
        }

        public async Task<CommandResult> Handle(DeleteDogadjajCommand command)
        {
            var existing = await _db.Dogadjaji.FindAsync(command.DogadjajId);
            if (existing == null)
                return CommandResult.Fail("Dogadjaj ne postoji.");
            if (existing.Otkazan)
                return CommandResult.Fail("Dogadjaj je vec otkazan.");

            // Ne brisemo fizicki red — u Event Sourcing-u se stanje menja samo kroz dogadjaje (Nedelja 10)
            await _eventStore.AppendAsync(existing.DogadjajId, "DogadjajOtkazan",
                new DogadjajOtkazanEvent { Razlog = "Otkazano od strane korisnika" });
            existing.Otkazan = true;
            await _db.SaveChangesAsync();

            return CommandResult.Ok("Dogadjaj uspesno otkazan.", existing.DogadjajId);
        }
    }
}