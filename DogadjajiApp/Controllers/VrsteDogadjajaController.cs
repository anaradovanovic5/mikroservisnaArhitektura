using DogadjajiApp.Data;
using DogadjajiApp.Domains;
using Microsoft.AspNetCore.Mvc;

namespace DogadjajiApp.Controllers
{
    public class VrsteDogadjajaController : Controller
    {
        public VrsteDogadjajaController(AppDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public AppDbContext DbContext { get; }

        public IActionResult Index()
        {
            var vrste = DbContext.VrsteDogadjaja.ToList();
            return Ok(vrste);
        }

        [HttpPost]
        public IActionResult Create(VrstaDogadjaja vrsta)
        {
            DbContext.VrsteDogadjaja.Add(vrsta);
            DbContext.SaveChanges();
            return Ok(vrsta);
        }

        public IActionResult Delete(int id)
        {
            var vrsta = DbContext.VrsteDogadjaja.Find(id);
            if (vrsta == null) return NotFound();
            DbContext.VrsteDogadjaja.Remove(vrsta);
            DbContext.SaveChanges();
            return Ok("Obrisano");
        }
    }
}
