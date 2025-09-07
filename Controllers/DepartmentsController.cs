using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;

namespace MutaEngineering.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public DepartmentsController(AppDbContext db, IWebHostEnvironment env)
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

        // حفظ صورة القسم تحت wwwroot/img/departments
        private async Task<string?> SavePhotoAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("ImagePath", "Unsupported file type.");
                return null;
            }

            var folder = Path.Combine(_env.WebRootPath, "img", "departments");
            Directory.CreateDirectory(folder);

            var filename = $"{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(folder, filename);
            using var stream = System.IO.File.Create(full);
            await file.CopyToAsync(stream);

            return $"~/img/departments/{filename}";
        }

        // حذف ملف قديم (نقيّده بمجلد departments فقط)
        private void TryDeleteOldImage(string? relPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relPath)) return;
                var rel = relPath.Replace("~", "").Replace('/', Path.DirectorySeparatorChar);
                var full = Path.Combine(_env.WebRootPath, rel.TrimStart(Path.DirectorySeparatorChar));

                var safeRoot = Path.Combine(_env.WebRootPath, "img", "departments");
                if (!full.StartsWith(safeRoot)) return;

                if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            }
            catch { /* ignore */ }
        }

        // ===== Index =====
        public async Task<IActionResult> Index(string? q = null)
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? "الأقسام الأكاديمية" : "Academic Departments";
            ViewBag.IsAdmin = IsAdmin();
            ViewBag.Query = q ?? "";

            var query = _db.Departments.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim().ToLower();
                query = query.Where(d =>
                    (d.Code ?? "").ToLower().Contains(t) ||
                    d.NameAr.ToLower().Contains(t) ||
                    d.NameEn.ToLower().Contains(t) ||
                    (d.DescriptionAr ?? "").ToLower().Contains(t) ||
                    (d.DescriptionEn ?? "").ToLower().Contains(t));
            }

            var list = await query.OrderBy(d => d.NameEn).ToListAsync();
            return View(list);
        }

        // ===== Details =====
        public async Task<IActionResult> Details(int id)
        {
            var d = await _db.Departments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (d == null) return NotFound();

            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? d.NameAr : d.NameEn;
            return View(d);
        }

        // ===== Create =====
        [HttpGet]
        public IActionResult Create(string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
            return View(new Department());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create(Department model, IFormFile? photo, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            if (photo != null)
                model.ImagePath = await SavePhotoAsync(photo) ?? model.ImagePath;

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                return View(model);
            }

            _db.Departments.Add(model);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Department added.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // ===== Edit =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var d = await _db.Departments.FindAsync(id);
            if (d == null) return NotFound();

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
            return View(d);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Edit(int id, Department model, IFormFile? photo, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            if (id != model.Id) return BadRequest();

            var existing = await _db.Departments.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return NotFound();

            if (photo != null)
            {
                var newPath = await SavePhotoAsync(photo);
                if (newPath != null)
                {
                    TryDeleteOldImage(existing.ImagePath);
                    existing.ImagePath = newPath;
                }
            }

            existing.Code = model.Code;
            existing.NameAr = model.NameAr;
            existing.NameEn = model.NameEn;
            existing.DescriptionAr = model.DescriptionAr;
            existing.DescriptionEn = model.DescriptionEn;

            if (!TryValidateModel(existing))
            {
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                return View(existing);
            }

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Department updated.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // ===== Delete (آمن مع فحوصات FK) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var hasExams = await _db.Exams.AnyAsync(e => e.DepartmentId == id);
            var hasFaculty = await _db.FacultyMembers.AnyAsync(f => f.DepartmentId == id);

            if (hasExams || hasFaculty)
            {
                TempData["Err"] = "لا يمكن حذف القسم لوجود عناصر مرتبطة به (مواد/امتحانات أو أعضاء هيئة تدريس). احذف أو انقل العناصر أولاً.";
                return Redirect(returnUrl ?? Url.Action("Index")!);
            }

            var d = await _db.Departments.FindAsync(id);
            if (d == null) return NotFound();

            TryDeleteOldImage(d.ImagePath);
            _db.Departments.Remove(d);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Department deleted.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }
    }
}
