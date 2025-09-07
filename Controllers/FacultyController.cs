using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;

namespace MutaEngineering.Controllers
{
    public class FacultyController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public FacultyController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db; _env = env;
        }

        // ✅ الصلاحية الآن من الـSession
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        // ========= Helpers =========

        // رفع الصورة تحت wwwroot/img/faculty وإرجاع مسارها النسبي
        private async Task<string?> SavePhotoAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                ModelState.AddModelError("PhotoPath", "Unsupported file type.");
                return null;
            }

            var folder = Path.Combine(_env.WebRootPath, "img", "faculty");
            Directory.CreateDirectory(folder);

            var filename = $"{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(folder, filename);
            await using var stream = System.IO.File.Create(full);
            await file.CopyToAsync(stream);

            return $"~/img/faculty/{filename}";
        }

        private async Task LoadDepartmentsAsync()
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            var list = await _db.Departments.AsNoTracking()
                .OrderBy(d => d.NameEn)
                .Select(d => new { d.Id, Name = isRtl ? d.NameAr : d.NameEn })
                .ToListAsync();

            ViewBag.Departments = new SelectList(list, "Id", "Name");
        }

        // ========= Index =========
        public async Task<IActionResult> Index(string? dep = null, string? q = null)
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? "أعضاء هيئة التدريس" : "Faculty Members";
            ViewBag.IsAdmin = IsAdmin();

            var query = _db.FacultyMembers
                .AsNoTracking()
                .Include(m => m.Department)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(dep))
            {
                query = query.Where(m =>
                    m.Department.Code == dep ||
                    m.Department.NameAr == dep ||
                    m.Department.NameEn == dep);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim().ToLower();
                query = query.Where(m =>
                    m.FullNameAr.ToLower().Contains(t) ||
                    m.FullNameEn.ToLower().Contains(t) ||
                    (m.TitleAr != null && m.TitleAr.ToLower().Contains(t)) ||
                    (m.TitleEn != null && m.TitleEn.ToLower().Contains(t)) ||
                    (m.Email != null && m.Email.ToLower().Contains(t)));
            }

            query = query
                .OrderBy(m => m.Department.Code)
                .ThenBy(m => isRtl ? m.FullNameAr : m.FullNameEn);

            var departmentsDict = await _db.Departments
                .AsNoTracking()
                .OrderBy(d => d.NameEn)
                .ToDictionaryAsync(
                    d => d.Code ?? d.Id.ToString(),
                    d => $"{d.NameAr}|{d.NameEn}");

            var vm = new FacultyVM
            {
                Departments = departmentsDict,
                SelectedDep = dep,
                Query = q,
                Members = await query.ToListAsync()
            };
            return View(vm);
        }

        // ========= Details =========
        public async Task<IActionResult> Details(string id)
        {
            if (!int.TryParse(id, out var fid)) return NotFound();

            var m = await _db.FacultyMembers
                .AsNoTracking()
                .Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.Id == fid);

            if (m is null) return NotFound();

            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? m.FullNameAr : m.FullNameEn;
            return View(m);
        }

        // ========= Create =========
        [HttpGet]
        public async Task<IActionResult> Create(string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            await LoadDepartmentsAsync();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");

            // اجعل أول قسم افتراضي لتفادي DepartmentId = 0
            var firstDepId = await _db.Departments
                .OrderBy(d => d.NameEn)
                .Select(d => d.Id)
                .FirstOrDefaultAsync();

            return View(new FacultyMember { DepartmentId = firstDepId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Create(FacultyMember model, IFormFile? photo, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            // تجاهل التحقق على الملاحة (Department) لأنها لا تأتي من الفورم
            ModelState.Remove(nameof(FacultyMember.Department));

            if (model.DepartmentId <= 0)
                ModelState.AddModelError("DepartmentId", "Select a department.");

            if (photo != null)
                model.PhotoPath = await SavePhotoAsync(photo) ?? model.PhotoPath;

            if (!ModelState.IsValid)
            {
                ViewBag.FormErrors = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await LoadDepartmentsAsync();
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                return View(model);
            }

            _db.FacultyMembers.Add(model);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Member added.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // ========= Edit =========
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var m = await _db.FacultyMembers.FindAsync(id);
            if (m == null) return NotFound();

            await LoadDepartmentsAsync();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
            return View(m);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> Edit(int id, FacultyMember model, IFormFile? photo, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            if (id != model.Id) return BadRequest();

            var existing = await _db.FacultyMembers.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return NotFound();

            // تجاهل التحقق على الملاحة
            ModelState.Remove(nameof(FacultyMember.Department));

            if (model.DepartmentId <= 0)
                ModelState.AddModelError("DepartmentId", "Select a department.");

            if (photo != null)
                existing.PhotoPath = await SavePhotoAsync(photo) ?? existing.PhotoPath;

            // نقل القيم المرسلة
            existing.FullNameAr = model.FullNameAr;
            existing.FullNameEn = model.FullNameEn;
            existing.TitleAr = model.TitleAr;
            existing.TitleEn = model.TitleEn;
            existing.Email = model.Email;
            existing.Office = model.Office;
            existing.DepartmentId = model.DepartmentId;

            if (!ModelState.IsValid)
            {
                ViewBag.FormErrors = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await LoadDepartmentsAsync();
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                return View(existing);
            }

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Member updated.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // ========= Delete =========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var m = await _db.FacultyMembers.FindAsync(id);
            if (m == null) return NotFound();

            _db.FacultyMembers.Remove(m);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Member deleted.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }
    }

    // ========= ViewModel للفهرس =========
    public class FacultyVM
    {
        public Dictionary<string, string> Departments { get; set; } = new();
        public string? SelectedDep { get; set; }
        public string? Query { get; set; }
        public List<FacultyMember> Members { get; set; } = new();
    }
}
