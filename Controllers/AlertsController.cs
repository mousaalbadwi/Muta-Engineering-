using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;
using System.Globalization;

namespace MutaEngineering.Controllers
{
    public class AlertsController : Controller
    {
        private readonly AppDbContext _db;

        public AlertsController(AppDbContext db)
        {
            _db = db;
        }

        // ✅ الصلاحية الآن من الـSession
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        // ===== Index (List + Filters) =====
        public async Task<IActionResult> Index(int? depId, string? q, bool upcomingOnly = true)
        {
            var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? "التنبيهات الأكاديمية" : "Academic Alerts";
            ViewBag.IsAdmin = IsAdmin();

            var query = _db.AcademicAlerts
                .AsNoTracking()
                .Include(a => a.Department)
                .AsQueryable();

            if (depId.HasValue)
                query = query.Where(a => a.DepartmentId == depId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim().ToLower();
                query = query.Where(a =>
                    a.TitleAr.ToLower().Contains(t) ||
                    a.TitleEn.ToLower().Contains(t) ||
                    (a.Location != null && a.Location.ToLower().Contains(t)) ||
                    (a.Department != null && (
                        a.Department.NameAr.ToLower().Contains(t) ||
                        a.Department.NameEn.ToLower().Contains(t) ||
                        (a.Department.Code != null && a.Department.Code.ToLower().Contains(t))
                    )));
            }

            if (upcomingOnly)
                query = query.Where(a => !a.Date.HasValue || a.Date >= DateTime.UtcNow.AddDays(-1));

            // ترتيب: الأهم أولاً، ثم التاريخ
            query = query
                .OrderByDescending(a => a.IsImportant)
                .ThenBy(a => a.Date);

            await LoadDepartmentsAsync();
            ViewBag.Filters = new { depId, q, upcomingOnly };

            var data = await query.ToListAsync();
            return View(data);
        }

        // ===== Details (اختياري) =====
        public async Task<IActionResult> Details(int id)
        {
            var item = await _db.AcademicAlerts
                .AsNoTracking()
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (item == null) return NotFound();

            var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? item.TitleAr : item.TitleEn;
            return View(item);
        }

        // ===== Create =====
        [HttpGet]
        public async Task<IActionResult> Create(string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            await LoadDepartmentsAsync();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
            return View(new AcademicAlert { Date = DateTime.UtcNow.AddDays(1) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AcademicAlert model, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync();
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                return View(model);
            }

            _db.AcademicAlerts.Add(model);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Alert added.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // ===== Edit =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var a = await _db.AcademicAlerts.FindAsync(id);
            if (a == null) return NotFound();

            await LoadDepartmentsAsync();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
            return View(a);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AcademicAlert model, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            if (id != model.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync();
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                return View(model);
            }

            var existing = await _db.AcademicAlerts.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return NotFound();

            existing.TitleAr = model.TitleAr;
            existing.TitleEn = model.TitleEn;
            existing.Location = model.Location;
            existing.Date = model.Date;
            existing.IsImportant = model.IsImportant;
            existing.DepartmentId = model.DepartmentId;

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Alert updated.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // ===== Delete =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var a = await _db.AcademicAlerts.FindAsync(id);
            if (a == null) return NotFound();

            _db.AcademicAlerts.Remove(a);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Alert deleted.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        private async Task LoadDepartmentsAsync()
        {
            var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            var list = await _db.Departments.AsNoTracking()
                .OrderBy(d => d.NameEn)
                .Select(d => new { d.Id, Name = isRtl ? d.NameAr : d.NameEn })
                .ToListAsync();

            ViewBag.Departments = new SelectList(list, "Id", "Name");
        }
    }
}
