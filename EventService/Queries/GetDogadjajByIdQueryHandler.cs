using EventService.Data;
using EventService.Queries.Dtos;

namespace EventService.Queries
{
    public class GetDogadjajByIdQueryHandler
    {
        private readonly EventDbContext _db;

        public GetDogadjajByIdQueryHandler(EventDbContext db)
        {
            _db = db;
        }

        public async Task<DogadjajDetailsDto?> Handle(GetDogadjajByIdQuery query)
        {
            var d = await _db.Dogadjaji.FindAsync(query.DogadjajId);
            if (d == null) return null;

            return new DogadjajDetailsDto
            {
                DogadjajId = d.DogadjajId,
                NazivDogadjaja = d.NazivDogadjaja,
                Agenda = d.Agenda,
                Datum = d.Datum,
                Trajanje = d.Trajanje,
                Cena = d.Cena,
                LokacijaId = d.LokacijaId,
                VrstaId = d.VrstaId,
                Otkazan = d.Otkazan
            };
        }
    }
}