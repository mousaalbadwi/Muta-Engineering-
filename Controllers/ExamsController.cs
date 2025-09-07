using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;
using System.Globalization;

namespace MutaEngineering.Controllers
{
    public class ExamsController : Controller
    {
        private readonly AppDbContext _db;

        public ExamsController(AppDbContext db)
        {
            _db = db;
        }

        // ✅ الصلاحية الآن من الـSession
        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        // ===== Helpers =====
        private async Task LoadDepartmentsAsync()
        {
            var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            var list = await _db.Departments.AsNoTracking()
                .OrderBy(d => d.NameEn)
                .Select(d => new { d.Id, Name = isRtl ? d.NameAr : d.NameEn })
                .ToListAsync();

            ViewBag.Departments = new SelectList(list, "Id", "Name");
        }

        // =========================================================
        //                       Exams (Upcoming / List)
        // =========================================================

        // GET: /Exams
        public async Task<IActionResult> Index(int? depId, int? year, string? q, bool upcomingOnly = true)
        {
            var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? "الامتحانات" : "Exams";
            ViewBag.IsAdmin = IsAdmin();

            var query = _db.Exams
                .AsNoTracking()
                .Include(e => e.Department)
                .AsQueryable();

            if (depId.HasValue)
                query = query.Where(e => e.DepartmentId == depId.Value);

            if (year.HasValue && year.Value > 0)
                query = query.Where(e => e.Year == year.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim().ToLower();
                query = query.Where(e =>
                    e.CourseCode.ToLower().Contains(t) ||
                    e.CourseNameAr.ToLower().Contains(t) ||
                    e.CourseNameEn.ToLower().Contains(t) ||
                    (e.Location != null && e.Location.ToLower().Contains(t)) ||
                    (e.Department != null && (
                        e.Department.NameAr.ToLower().Contains(t) ||
                        e.Department.NameEn.ToLower().Contains(t) ||
                        (e.Department.Code != null && e.Department.Code.ToLower().Contains(t))
                    )));
            }

            if (upcomingOnly)
                query = query.Where(e => e.DateTime >= DateTime.UtcNow.AddDays(-1));

            var items = await query
                .OrderBy(e => e.DateTime)
                .ThenBy(e => e.CourseCode)
                .ToListAsync();

            await LoadDepartmentsAsync();
            ViewBag.Filters = new { depId, year, q, upcomingOnly };

            return View(items);
        }

        // GET: /Exams/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var e = await _db.Exams
                .AsNoTracking()
                .Include(x => x.Department)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (e == null) return NotFound();

            var isRtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? e.CourseNameAr : e.CourseNameEn;
            return View(e);
        }

        // ثابتات واجهة: /Exams/Policies , /Exams/Support
        public IActionResult Policies()
        {
            ViewData["Title"] = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? "السياسات" : "Policies";
            ViewBag.IsAdmin = IsAdmin();
            return View();
        }

        public IActionResult Support()
        {
            ViewData["Title"] = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? "الدعم" : "Support";
            ViewBag.IsAdmin = IsAdmin();
            return View();
        }

        // ===== Exams CRUD =====

        // GET: /Exams/Create
        [HttpGet]
        public async Task<IActionResult> Create(string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            await LoadDepartmentsAsync();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");

            // افتراضيًا أول قسم
            var firstDepId = await _db.Departments.OrderBy(d => d.NameEn).Select(d => d.Id).FirstOrDefaultAsync();
            return View(new Exam
            {
                DepartmentId = firstDepId,
                DateTime = DateTime.UtcNow.AddDays(7),
                Mode = ExamMode.InPerson
            });
        }

        // POST: /Exams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exam model, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            // الملاحة تأتي من DB
            ModelState.Remove(nameof(Exam.Department));

            if (model.DepartmentId <= 0)
                ModelState.AddModelError("DepartmentId", "Select a department.");

            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync();
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                return View(model);
            }

            _db.Exams.Add(model);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Exam created.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // GET: /Exams/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var e = await _db.Exams.FindAsync(id);
            if (e == null) return NotFound();

            await LoadDepartmentsAsync();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
            return View(e);
        }

        // POST: /Exams/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Exam model, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            if (id != model.Id) return BadRequest();

            var existing = await _db.Exams.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return NotFound();

            ModelState.Remove(nameof(Exam.Department));

            if (model.DepartmentId <= 0)
                ModelState.AddModelError("DepartmentId", "Select a department.");

            // نقل القيم
            existing.BusinessId = string.IsNullOrWhiteSpace(model.BusinessId) ? existing.BusinessId : model.BusinessId;
            existing.CourseCode = model.CourseCode;
            existing.CourseNameAr = model.CourseNameAr;
            existing.CourseNameEn = model.CourseNameEn;
            existing.Year = model.Year;
            existing.DateTime = model.DateTime;
            existing.Mode = model.Mode;
            existing.Location = model.Location;
            existing.LmsUrl = model.LmsUrl;
            existing.LmsHowTo = model.LmsHowTo;
            existing.Instructions = model.Instructions;
            existing.HasStegoProtection = model.HasStegoProtection;
            existing.DepartmentId = model.DepartmentId;

            if (!ModelState.IsValid)
            {
                await LoadDepartmentsAsync();
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                return View(existing);
            }

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Exam updated.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // POST: /Exams/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var e = await _db.Exams.FindAsync(id);
            if (e == null) return NotFound();

            _db.Exams.Remove(e);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Exam deleted.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // =========================================================
        //                        Archive (Old Exams)
        // =========================================================

        // GET: /Exams/Archive
        public async Task<IActionResult> Archive(string? course, string? term)
        {
            ViewData["Title"] = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? "أرشيف الامتحانات" : "Exam Archive";
            ViewBag.IsAdmin = IsAdmin();

            var query = _db.ExamArchiveItems.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(course))
            {
                var t = course.Trim().ToLower();
                query = query.Where(a =>
                    a.CourseCode.ToLower().Contains(t) ||
                    a.CourseNameAr.ToLower().Contains(t) ||
                    a.CourseNameEn.ToLower().Contains(t));
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                var tt = term.Trim().ToLower();
                query = query.Where(a => a.Term != null && a.Term.ToLower().Contains(tt));
            }

            var items = await query
                .OrderByDescending(a => a.Term)
                .ThenBy(a => a.CourseCode)
                .ToListAsync();

            ViewBag.Filters = new { course, term };
            return View(items);
        }

        // GET: /Exams/ArchiveCreate
        [HttpGet]
        public IActionResult ArchiveCreate(string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Archive");
            return View(new ExamArchiveItem());
        }

        // POST: /Exams/ArchiveCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveCreate(ExamArchiveItem model, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Archive");
                return View(model);
            }

            _db.ExamArchiveItems.Add(model);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Archive item added.";
            return Redirect(returnUrl ?? Url.Action("Archive")!);
        }

        // GET: /Exams/ArchiveEdit/{id}
        [HttpGet]
        public async Task<IActionResult> ArchiveEdit(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var item = await _db.ExamArchiveItems.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Archive");
            return View(item);
        }

        // POST: /Exams/ArchiveEdit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveEdit(int id, ExamArchiveItem model, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            if (id != model.Id) return BadRequest();

            var existing = await _db.ExamArchiveItems.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return NotFound();

            existing.CourseCode = model.CourseCode;
            existing.CourseNameAr = model.CourseNameAr;
            existing.CourseNameEn = model.CourseNameEn;
            existing.Term = model.Term;
            existing.PdfUrl = model.PdfUrl;
            existing.SolutionUrl = model.SolutionUrl;

            if (!TryValidateModel(existing))
            {
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Archive");
                return View(existing);
            }

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Archive item updated.";
            return Redirect(returnUrl ?? Url.Action("Archive")!);
        }

        // POST: /Exams/ArchiveDelete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveDelete(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var item = await _db.ExamArchiveItems.FindAsync(id);
            if (item == null) return NotFound();

            _db.ExamArchiveItems.Remove(item);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Archive item deleted.";
            return Redirect(returnUrl ?? Url.Action("Archive")!);
        }
    }
}
