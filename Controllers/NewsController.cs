using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;
using System.Globalization;

namespace MutaEngineering.Controllers
{
    public class NewsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public NewsController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // ✅ الصلاحية الآن من الـSession
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        // حفظ الصورة تحت wwwroot/img/news
        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("ImagePath", "Unsupported file type.");
                return null;
            }

            var folder = Path.Combine(_env.WebRootPath, "img", "news");
            Directory.CreateDirectory(folder);

            var filename = $"{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(folder, filename);
            using var stream = System.IO.File.Create(full);
            await file.CopyToAsync(stream);

            return $"~/img/news/{filename}";
        }

        private IEnumerable<SelectListItem> CategorySelectItems(bool isRtl, NewsCategory? selected = null)
        {
            string L(NewsCategory c) => isRtl ? c switch
            {
                NewsCategory.Announcement => "إعلان",
                NewsCategory.Workshop => "ورشة",
                NewsCategory.Conference => "مؤتمر",
                _ => "أخرى"
            } : c.ToString();

            foreach (NewsCategory v in Enum.GetValues(typeof(NewsCategory)))
            {
                yield return new SelectListItem { Value = ((int)v).ToString(), Text = L(v), Selected = selected == v };
            }
        }

        // ===== Index =====
        public async Task<IActionResult> Index(int? category, string? q, bool publishedOnly = true)
        {
            var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? "الأخبار والفعاليات" : "News & Events";
            ViewBag.IsAdmin = IsAdmin();

            var query = _db.NewsItems.AsNoTracking().AsQueryable();

            if (publishedOnly) query = query.Where(n => n.IsPublished);

            if (category.HasValue)
            {
                var cat = (NewsCategory)category.Value;
                query = query.Where(n => n.Category == cat);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim().ToLower();
                query = query.Where(n =>
                    n.TitleAr.ToLower().Contains(t) ||
                    n.TitleEn.ToLower().Contains(t) ||
                    (n.BodyAr != null && n.BodyAr.ToLower().Contains(t)) ||
                    (n.BodyEn != null && n.BodyEn.ToLower().Contains(t)));
            }

            var items = await query
                .OrderByDescending(n => n.PublishDate)
                .ThenBy(n => n.Id)
                .ToListAsync();

            ViewBag.Categories = CategorySelectItems(isRtl, category.HasValue ? (NewsCategory)category : null);
            ViewBag.Filters = new { category, q, publishedOnly };

            return View(items);
        }

        // ===== Details =====
        public async Task<IActionResult> Details(int id)
        {
            var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            var item = await _db.NewsItems.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
            if (item == null) return NotFound();

            ViewData["Title"] = isRtl ? item.TitleAr : item.TitleEn;
            return View(item);
        }

        // ===== Create =====
        [HttpGet]
        public IActionResult Create(string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
            ViewBag.Categories = CategorySelectItems(isRtl);
            return View(new NewsItem { PublishDate = DateTime.UtcNow, IsPublished = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Create(NewsItem model, IFormFile? image, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            if (image != null) model.ImagePath = await SaveImageAsync(image) ?? model.ImagePath;

            if (!ModelState.IsValid)
            {
                var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                ViewBag.Categories = CategorySelectItems(isRtl, model.Category);
                return View(model);
            }

            _db.NewsItems.Add(model);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "News created.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // ===== Edit =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

            var item = await _db.NewsItems.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
            ViewBag.Categories = CategorySelectItems(isRtl, item.Category);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Edit(int id, NewsItem model, IFormFile? image, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            if (id != model.Id) return BadRequest();

            var existing = await _db.NewsItems.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return NotFound();

            if (image != null)
                existing.ImagePath = await SaveImageAsync(image) ?? existing.ImagePath;

            existing.TitleAr = model.TitleAr;
            existing.TitleEn = model.TitleEn;
            existing.BodyAr = model.BodyAr;
            existing.BodyEn = model.BodyEn;
            existing.Category = model.Category;
            existing.PublishDate = model.PublishDate;
            existing.IsPublished = model.IsPublished;

            if (!TryValidateModel(existing))
            {
                var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                ViewBag.Categories = CategorySelectItems(isRtl, existing.Category);
                return View(existing);
            }

            await _db.SaveChangesAsync();
            TempData["Ok"] = "News updated.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // ===== Delete =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            var item = await _db.NewsItems.FindAsync(id);
            if (item == null) return NotFound();

            _db.NewsItems.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "News deleted.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }
    }
}
