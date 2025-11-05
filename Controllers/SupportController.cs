using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using MutaEngineering.Data;
using MutaEngineering.Models;
using MutaEngineering.ViewModels;

namespace MutaEngineering.Controllers
{
    public class SupportController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedScreenshotExt = { ".png", ".jpg", ".jpeg", ".pdf", ".webp" };
        private const long MaxScreenshotBytes = 5 * 1024 * 1024; // 5MB

        // مسار الفيو الحقيقي عندك
        private const string SupportViewPath = "~/Views/Exams/Support.cshtml";

        public SupportController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(SupportViewPath, new SupportTicketInput());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SupportTicketInput m)
        {
            if (!Enum.IsDefined(typeof(SupportIssueType), m.IssueType))
                m.IssueType = SupportIssueType.Other;

            if (!ModelState.IsValid)
            {
                TempData["SwalIcon"] = "error";
                TempData["SwalTitle"] = "تعذّر الإرسال";
                TempData["SwalText"] = "تحقّق من الحقول وحاول مجدداً.";
                return View(SupportViewPath, m);
            }

            string? screenshotPath = null;
            if (m.Screenshot is { Length: > 0 })
            {
                var ext = Path.GetExtension(m.Screenshot.FileName).ToLowerInvariant();
                if (!AllowedScreenshotExt.Contains(ext) || m.Screenshot.Length > MaxScreenshotBytes)
                {
                    ModelState.AddModelError(nameof(m.Screenshot), "ملف غير مدعوم أو حجمه كبير.");
                    TempData["SwalIcon"] = "error";
                    TempData["SwalTitle"] = "تعذّر الإرسال";
                    TempData["SwalText"] = "الصيغ المسموحة: PNG/JPG/PDF/WEBP وبحد أقصى 5MB.";
                    return View(SupportViewPath, m);
                }

                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsDir = Path.Combine(webRoot, "uploads", "support");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var fs = System.IO.File.Create(filePath))
                    await m.Screenshot.CopyToAsync(fs);

                screenshotPath = $"/uploads/support/{fileName}";
            }

            var entity = new SupportTicket
            {
                FullName = m.FullName,
                UniversityId = m.UniversityId,
                Email = m.Email,
                CourseExam = m.CourseExam,
                IssueType = m.IssueType,
                Description = m.Description,
                ScreenshotPath = screenshotPath
            };

            try
            {
                _db.SupportTickets.Add(entity);
                await _db.SaveChangesAsync();

                TempData["SwalIcon"] = "success";
                TempData["SwalTitle"] = "تم الإرسال";
                TempData["SwalText"] = "استلمنا بلاغك وسنردّ على بريدك قريبًا.";
                return RedirectToAction(nameof(Index)); // يرجع لنفس الفيو بمساره الصحيح
            }
            catch
            {
                TempData["SwalIcon"] = "error";
                TempData["SwalTitle"] = "خطأ غير متوقّع";
                TempData["SwalText"] = "حاول لاحقاً أو تواصل عبر البريد.";
                return View(SupportViewPath, m);
            }
        }

        // غير مستخدمة حالياً، فقط للاحتياط
        public IActionResult Thanks() => View();
    }
}
