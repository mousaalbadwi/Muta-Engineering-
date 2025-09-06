using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;
using MutaEngineering.ViewModels;

namespace MutaEngineering.Controllers
{
    [AutoValidateAntiforgeryToken]
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        public AccountController(AppDbContext db) => _db = db;

        // ----- Login (بدون تغيير كبير) -----
        [HttpGet]
        public IActionResult Login(string? returnUrl = null) { ViewBag.ReturnUrl = returnUrl; return View(); }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == model.PhoneNumber);
            if (user == null || user.PasswordHash == null ||
                !BCrypt.Net.BCrypt.Verify(model.Password!, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty,
                    System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
                    ? "رقم الهاتف أو كلمة المرور غير صحيحة."
                    : "Invalid phone number or password.");
                return View(model);
            }

            // Session sign-in
            HttpContext.Session.SetString("UserPhone", user.Username);
            HttpContext.Session.SetString("UserName", user.FullName ?? "");
            HttpContext.Session.SetString("UserRole", user.Role);

            return RedirectToAction("Index", "Home");
        }

        // ----- SignUp اليدوي (تصريحًا ترجع للأصل) -----
        [HttpGet]
        public IActionResult SignUp(string? returnUrl = null) { ViewBag.ReturnUrl = returnUrl; return View(); }

        [HttpPost]
        public async Task<IActionResult> SignUp(RegisterViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _db.Users.AnyAsync(u => u.Username == model.PhoneNumber))
            {
                ModelState.AddModelError(nameof(RegisterViewModel.PhoneNumber),
                    System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
                    ? "هذا الرقم مسجّل مسبقًا."
                    : "This phone number is already registered.");
                return View(model);
            }

            var user = new User
            {
                Username = model.PhoneNumber!,
                FullName = model.FullName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password!)
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Auto-login -> Home
            HttpContext.Session.SetString("UserPhone", user.Username);
            HttpContext.Session.SetString("UserName", user.FullName ?? "");
            HttpContext.Session.SetString("UserRole", user.Role);

            return RedirectToAction("Index", "Home");
        }

        // ----- تسجيل خارجي: بدء التحدي -----
        [HttpPost]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var props = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(props, provider); // "Google" | "GitHub" | "Facebook"
        }

        // ----- تسجيل خارجي: العودة -----
        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            // الـ Claims من المزوّد
            var principal = HttpContext.User;
            if (principal?.Identity?.IsAuthenticated != true)
                return RedirectToAction(nameof(Login));

            var provider = principal.Identity!.AuthenticationType ?? "External";
            var providerKey = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? principal.FindFirst("sub")?.Value
                              ?? principal.FindFirst("id")?.Value
                              ?? Guid.NewGuid().ToString("N");

            // حاول الحصول على البريد أو الاسم
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var name = principal.FindFirst(ClaimTypes.Name)?.Value
                        ?? principal.FindFirst("name")?.Value;

            // أنشئ/حدّث المستخدم المحلي
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Provider == provider && u.ProviderKey == providerKey);
            if (user == null)
            {
                // جرّب التطابق بالبريد إن وجد
                if (!string.IsNullOrWhiteSpace(email))
                    user = await _db.Users.FirstOrDefaultAsync(u => u.Username == email);

                if (user == null)
                {
                    user = new User
                    {
                        Username = email ?? $"{provider}-{providerKey}",
                        FullName = name,
                        Provider = provider,
                        ProviderKey = providerKey
                    };
                    _db.Users.Add(user);
                }
                else
                {
                    user.Provider = provider;
                    user.ProviderKey = providerKey;
                    if (string.IsNullOrEmpty(user.FullName) && !string.IsNullOrEmpty(name))
                        user.FullName = name;
                }
                await _db.SaveChangesAsync();
            }

            // خزّن الجلسة (نفس أسلوبك)
            HttpContext.Session.SetString("UserPhone", user.Username);
            HttpContext.Session.SetString("UserName", user.FullName ?? "");
            HttpContext.Session.SetString("UserRole", user.Role);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
