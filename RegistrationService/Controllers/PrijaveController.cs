using Microsoft.AspNetCore.Mvc;
using RegistrationService.Data;
using RegistrationService.Domains;

namespace RegistrationService.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class PrijaveController : Controller
    {
        public PrijaveController(RegistrationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public RegistrationDbContext DbContext { get; }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok(DbContext.Prijave.ToList());
        }

        [HttpPost]
        public IActionResult Create(Prijava prijava)
        {
            prijava.DatumPrijave = DateTime.Now;
            DbContext.Prijave.Add(prijava);
            DbContext.SaveChanges();
            return Ok(prijava);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var prijava = DbContext.Prijave.Find(id);
            if (prijava == null) return NotFound();
            DbContext.Prijave.Remove(prijava);
            DbContext.SaveChanges();
            return Ok("Obrisano");
        }
    }
}