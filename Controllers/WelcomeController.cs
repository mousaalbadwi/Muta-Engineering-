using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MutaEngineering.Controllers
{
    [AllowAnonymous]
    public class WelcomeController : Controller
    {
        public IActionResult Index()
        {
            // لو المستخدم مسجّل (سيشن موجود) روّحه عالهوم
            var isLoggedIn = !string.IsNullOrEmpty(HttpContext.Session.GetString("UserPhone"));
            if (isLoggedIn) return RedirectToAction("Index", "Home");

            return View();
        }
    }
}
