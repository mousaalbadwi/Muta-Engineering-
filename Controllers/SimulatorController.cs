using Microsoft.AspNetCore.Mvc;

namespace MutaEngineering.Controllers
{
    public class SimulatorController : Controller
    {
        public IActionResult Circuit()
        {
            return View();
        }
    }
}
