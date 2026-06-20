using EventService.Data;
using EventService.Queries.Dtos;
using Microsoft.EntityFrameworkCore;

namespace EventService.Queries
{
    public class FilterDogadjajiByVrstaQueryHandler
    {
        private readonly EventDbContext _db;

        public FilterDogadjajiByVrstaQueryHandler(EventDbContext db)
        {
            _db = db;
        }

        public async Task<List<DogadjajListItemDto>> Handle(FilterDogadjajiByVrstaQuery query)
        {
            return await _db.Dogadjaji
                .Where(d => d.VrstaId == query.VrstaId)
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