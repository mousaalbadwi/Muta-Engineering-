using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;
using System.Diagnostics;

namespace MutaEngineering.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _db;

        public HomeController(ILogger<HomeController> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        // نحمّل أحدث التنبيهات (لو الجدول فاضي يرجع قائمة فاضية)
        public async Task<IActionResult> Index()
        {
            var alerts = await _db.AcademicAlerts
                                  .OrderByDescending(a => a.Id)   // آمن بغض النظر عن الحقول الأخرى
                                  .Take(6)
                                  .ToListAsync();

            return View(alerts); // <-- مرّرنا القائمة كـ Model
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
