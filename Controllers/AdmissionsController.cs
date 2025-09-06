using Microsoft.AspNetCore.Mvc;

namespace MutaEngineering.Controllers
{
    public class AdmissionsController : Controller
    {
        // /Admissions
        [HttpGet]
        public IActionResult Index() => View();

        // /Admissions/Requirements
        [HttpGet]
        public IActionResult Requirements() => View("Requirements");

        // /Admissions/HowToApply
        [HttpGet]
        public IActionResult HowToApply() => View("HowToApply");

        // /Admissions/Tuition
        [HttpGet]
        public IActionResult Tuition() => View("Tuition");
    }
}
