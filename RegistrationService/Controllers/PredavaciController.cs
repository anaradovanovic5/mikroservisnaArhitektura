using Microsoft.AspNetCore.Mvc;
using RegistrationService.Data;
using RegistrationService.Domains;

namespace RegistrationService.Controllers
{
    [Route("[controller]/[action]/{id?}")]
    public class PredavaciController : Controller
    {
        public PredavaciController(RegistrationDbContext dbContext)
        {
            DbContext = dbContext;
        }

        public RegistrationDbContext DbContext { get; }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok(DbContext.Predavaci.ToList());
        }

        [HttpPost]
        public IActionResult Create(Predavac predavac)
        {
            DbContext.Predavaci.Add(predavac);
            DbContext.SaveChanges();
            return Ok(predavac);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var predavac = DbContext.Predavaci.Find(id);
            if (predavac == null) return NotFound();
            DbContext.Predavaci.Remove(predavac);
            DbContext.SaveChanges();
            return Ok("Obrisano");
        }
    }
}