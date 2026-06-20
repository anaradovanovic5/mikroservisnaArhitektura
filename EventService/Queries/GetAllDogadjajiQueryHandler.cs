using EventService.Data;
using EventService.Queries.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EventService.Queries
{
    public class GetAllDogadjajiQueryHandler
    {
        private readonly EventDbContext _db;

        public GetAllDogadjajiQueryHandler(EventDbContext db)
        {
            _db = db;
        }

        public async Task<List<DogadjajListItemDto>> Handle(GetAllDogadjajiQuery query)
        {
            return await _db.Dogadjaji
                .Select(d => new DogadjajListItemDto
                {
                    DogadjajId = d.DogadjajId,
                    NazivDogadjaja = d.NazivDogadjaja,
                    Datum = d.Datum,
                    Cena = d.Cena,
                    Otkazan = d.Otkazan
                })
                .ToListAsync();
        }
    }
}