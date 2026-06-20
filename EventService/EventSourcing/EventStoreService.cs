using EventService.Data;
using EventService.Domains;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EventService.EventSourcing
{
    public interface IEventStoreService
    {
        Task AppendAsync(int aggregateId, string eventType, object payload);
        Task<List<EventStoreEntry>> GetHistoryAsync(int aggregateId);
        Task<DogadjajState> GetCurrentStateAsync(int aggregateId);
    }

    public class EventStoreService : IEventStoreService
    {
        private const int SnapshotSvakihNDogadjaja = 3;
        private readonly EventDbContext _db;

        public EventStoreService(EventDbContext db)
        {
            _db = db;
        }

        public async Task AppendAsync(int aggregateId, string eventType, object payload)
        {
            var lastVersion = await _db.EventStoreEntries
                .Where(e => e.AggregateId == aggregateId)
                .Select(e => e.Version)
                .DefaultIfEmpty(0)
                .MaxAsync();

            var entry = new EventStoreEntry
            {
                AggregateId = aggregateId,
                EventType = eventType,
                Payload = JsonSerializer.Serialize(payload),
                Version = lastVersion + 1,
                CreatedAt = DateTime.UtcNow
            };

            _db.EventStoreEntries.Add(entry);
            await _db.SaveChangesAsync();

            if (entry.Version % SnapshotSvakihNDogadjaja == 0)
                await SacuvajSnapshotAsync(aggregateId);
        }

        public async Task<List<EventStoreEntry>> GetHistoryAsync(int aggregateId)
        {
            return await _db.EventStoreEntries
                .Where(e => e.AggregateId == aggregateId)
                .OrderBy(e => e.Version)
                .ToListAsync();
        }

        // Rekonstrukcija: poslednji snapshot + samo dogadjaji nastali POSLE njega
        public async Task<DogadjajState> GetCurrentStateAsync(int aggregateId)
        {
            var state = new DogadjajState { DogadjajId = aggregateId };

            var poslednjiSnapshot = await _db.DogadjajSnapshots
                .Where(s => s.AggregateId == aggregateId)
                .OrderByDescending(s => s.Version)
                .FirstOrDefaultAsync();

            if (poslednjiSnapshot != null)
            {
                state.NazivDogadjaja = poslednjiSnapshot.NazivDogadjaja;
                state.Agenda = poslednjiSnapshot.Agenda;
                state.Datum = poslednjiSnapshot.Datum;
                state.Trajanje = poslednjiSnapshot.Trajanje;
                state.Cena = poslednjiSnapshot.Cena;
                state.LokacijaId = poslednjiSnapshot.LokacijaId;
                state.VrstaId = poslednjiSnapshot.VrstaId;
                state.Otkazan = poslednjiSnapshot.Otkazan;
                state.Version = poslednjiSnapshot.Version;
            }

            var noviDogadjaji = await _db.EventStoreEntries
                .Where(e => e.AggregateId == aggregateId && e.Version > state.Version)
                .OrderBy(e => e.Version)
                .ToListAsync();

            foreach (var ev in noviDogadjaji)
                state.Primeni(ev.EventType, ev.Payload);

            return state;
        }

        private async Task SacuvajSnapshotAsync(int aggregateId)
        {
            var state = await GetCurrentStateAsync(aggregateId);

            _db.DogadjajSnapshots.Add(new DogadjajSnapshot
            {
                AggregateId = aggregateId,
                NazivDogadjaja = state.NazivDogadjaja,
                Agenda = state.Agenda,
                Datum = state.Datum,
                Trajanje = state.Trajanje,
                Cena = state.Cena,
                LokacijaId = state.LokacijaId,
                VrstaId = state.VrstaId,
                Otkazan = state.Otkazan,
                Version = state.Version,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
    }
}