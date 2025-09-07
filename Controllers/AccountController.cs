using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;
using MutaEngineering.ViewModels; // LoginViewModel / RegisterViewModel
using BCrypt.Net;

namespace MutaEngineering.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;

        public AccountController(AppDbContext db, IConfiguration cfg)
        {
            _db = db; _cfg = cfg;
        }

        // ===== Helpers =====
        private void SignInToSession(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.Id);
            // عندك المعرّف الفعلي هو رقم الهاتف (مخزّن في Username هنا)
            HttpContext.Session.SetString("UserPhone", user.Username);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("UserFullName", user.FullName ?? user.Username);
            HttpContext.Session.SetString("UserRole", user.Role ?? "User");
        }

        private async Task CookieSignInAsync(User user)
        {
            // (اختياري) كوكي للمطالبة بالدور والاسم
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Username),
                new Claim(ClaimTypes.Role, user.Role ?? "User"),
                new Claim("phone", user.Username)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        private void ClearSession()
        {
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("UserPhone");
            HttpContext.Session.Remove("UserFullName");
            HttpContext.Session.Remove("UserRole");
        }

        private IActionResult SafeRedirect(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        // ===== Register =====
        [HttpGet]
        public IActionResult SignUp(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index", "Home");
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(RegisterViewModel vm, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index", "Home");

            if (!ModelState.IsValid) return View(vm);

            var phone = (vm.PhoneNumber ?? "").Trim();
            if (string.IsNullOrEmpty(phone))
            {
                ModelState.AddModelError(nameof(vm.PhoneNumber), "رقم الهاتف مطلوب.");
                return View(vm);
            }

            // رقم الهاتف فريد (نخزّنه في User.Username)
            var exists = await _db.Users.AnyAsync(u => u.Username == phone);
            if (exists)
            {
                ModelState.AddModelError(nameof(vm.PhoneNumber), "رقم الهاتف مستخدم مسبقًا.");
                return View(vm);
            }

            var user = new User
            {
                Username = phone,                 // نخزّن الهاتف كـ Username
                FullName = vm.FullName?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(vm.Password),
                Role = "User"
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // سجل بالجلسة + كوكي (اختياري)
            SignInToSession(user);
            await CookieSignInAsync(user);

            TempData["Ok"] = "مرحبًا بك!";
            return SafeRedirect(returnUrl);
        }

        // ===== Login =====
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index", "Home");
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index", "Home");
            if (!ModelState.IsValid) return View(vm);

            var phone = (vm.PhoneNumber ?? "").Trim();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == phone);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(vm.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "بيانات الدخول غير صحيحة.");
                return View(vm);
            }

            SignInToSession(user);
            await CookieSignInAsync(user);

            TempData["Ok"] = "تم تسجيل الدخول.";
            return SafeRedirect(returnUrl);
        }

        // ===== External Login (Google / GitHub / Facebook) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var props = new AuthenticationProperties { RedirectUri = redirectUrl, Items = { { "scheme", provider } } };
            return Challenge(props, provider);
        }

        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null)
        {
            returnUrl ??= Url.Action("Index", "Home");

            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var extUser = User?.Identities?.FirstOrDefault(i => i.IsAuthenticated) ??
                          result.Principal?.Identities?.FirstOrDefault(i => i.IsAuthenticated);
            if (extUser == null) return RedirectToAction(nameof(Login), new { returnUrl });

            var provider = extUser.AuthenticationType ?? "External";
            var providerKey = extUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? extUser.FindFirst("sub")?.Value
                              ?? Guid.NewGuid().ToString("N");
            var displayName = extUser.FindFirst(ClaimTypes.Name)?.Value ?? extUser.FindFirst("name")?.Value;
            var email = extUser.FindFirst(ClaimTypes.Email)?.Value;

            // بما إن النظام يعتمد هاتف للتسجيل المحلي، نربط الخارجي بالإيميل إن وُجد، وإلا نستخدم providerKey
            var username = !string.IsNullOrWhiteSpace(email) ? email : providerKey;

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Provider == provider && u.ProviderKey == providerKey);
            if (user == null)
            {
                user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                {
                    user = new User
                    {
                        Username = username,
                        FullName = displayName ?? username,
                        Role = "User",
                        Provider = provider,
                        ProviderKey = providerKey
                    };
                    _db.Users.Add(user);
                    await _db.SaveChangesAsync();
                }
                else
                {
                    user.Provider = provider;
                    user.ProviderKey = providerKey;
                    await _db.SaveChangesAsync();
                }
            }

            SignInToSession(user);
            await CookieSignInAsync(user);

            TempData["Ok"] = "تم تسجيل الدخول.";
            return SafeRedirect(returnUrl);
        }

        // ===== Logout =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAndLogout()
        {
            // استرجاع المستخدم الحالي من الـ Session
            var uid = HttpContext.Session.GetInt32("UserId");

            if (uid.HasValue)
            {
                var user = await _db.Users.FindAsync(uid.Value);
                if (user != null)
                {
                    _db.Users.Remove(user);
                    await _db.SaveChangesAsync();
                }
            }

            // خروج الكوكي + تنظيف الجلسة
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ClearSession();

            // التحويل لصفحة الترحيب
            return RedirectToAction("Index", "Welcome");
        }

    }
}
