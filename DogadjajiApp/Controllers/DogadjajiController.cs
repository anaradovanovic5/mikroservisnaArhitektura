using DogadjajiApp.Data;
using DogadjajiApp.Domains;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogadjajiApp.Controllers
{
    public class DogadjajiController : Controller
    {
        public DogadjajiController(AppDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public AppDbContext DbContext { get; }

        public IActionResult Index()
        {
            var dogadjaji = DbContext.Dogadjaji
                .Include(d => d.Lokacija)
                .Include(d => d.VrstaDogadjaja)
                .Include(d => d.DogadjajPredavaci)
                    .ThenInclude(dp => dp.Predavac)
                .ToList();

            return Ok(dogadjaji);
        }

        [HttpPost]
        public IActionResult Create(Dogadjaj dogadjaj)
        {
            DbContext.Dogadjaji.Add(dogadjaj);
            DbContext.SaveChanges();
            return Ok(dogadjaj);
        }

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
