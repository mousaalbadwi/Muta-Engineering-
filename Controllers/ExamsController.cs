using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;

namespace MutaEngineering.Controllers
{
    public class ExamsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public ExamsController(AppDbContext db, IWebHostEnvironment env, IConfiguration config)
        {
            _db = db; _env = env; _config = config;
        }

        private bool IsAdmin() => _config.GetValue<bool>("Admin:Enabled");

        // ===== Helpers =====
        private async Task LoadDepartmentsAsync()
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            var list = await _db.Departments.AsNoTracking()
                .OrderBy(d => d.NameEn)
                .Select(d => new { d.Id, d.Code, Name = isRtl ? d.NameAr : d.NameEn })
                .ToListAsync();

            ViewBag.DepartmentsSelect = new SelectList(list, "Id", "Name");
        }

        private async Task<string?> SavePdfAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
            {
                ModelState.AddModelError("Pdf", "Only PDF files are allowed.");
                return null;
            }

            var folder = Path.Combine(_env.WebRootPath, "docs", "archive");
            Directory.CreateDirectory(folder);

            var filename = $"{Guid.NewGuid():N}{ext}";
            var full = Path.Combine(folder, filename);
            using var stream = System.IO.File.Create(full);
            await file.CopyToAsync(stream);

            return $"~/docs/archive/{filename}";
        }

        // ===== Index (قائمة + فلترة) =====
        public async Task<IActionResult> Index(string? dept, int? year, string? q)
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? "اختبارات الجامعة" : "University Exams";
            ViewBag.IsAdmin = IsAdmin();

            var query = _db.Exams
                .Include(e => e.Department)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(dept))
            {
                var d = dept.Trim();
                query = query.Where(x =>
                    x.Department.Code == d ||
                    x.Department.NameAr == d ||
                    x.Department.NameEn == d);
            }

            if (year.HasValue)
                query = query.Where(x => x.Year == year.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim().ToLower();
                query = query.Where(x =>
                    x.CourseCode.ToLower().Contains(t) ||
                    (isRtl ? x.CourseNameAr : x.CourseNameEn).ToLower().Contains(t));
            }

            var data = await query.OrderBy(x => x.DateTime).ToListAsync();

            // لقوائم الفلترة
            ViewBag.Departments = await _db.Departments
                .AsNoTracking()
                .OrderBy(d => d.NameEn)
                .ToDictionaryAsync(
                    d => d.Code ?? d.Id.ToString(),
                    d => $"{d.NameAr}|{d.NameEn}");

            ViewBag.Years = await _db.Exams
                .AsNoTracking()
                .Select(e => e.Year)
                .Distinct()
                .OrderBy(y => y)
                .ToListAsync();

            return View(data);
        }

        // ===== Details =====
        public async Task<IActionResult> Details(int id)
        {
            var item = await _db.Exams
                .AsNoTracking()
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (item == null) return NotFound();

            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? item.CourseNameAr : item.CourseNameEn;
            return View(item);
        }

        // ===== سياسات/دعم =====
        public IActionResult Policies()
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? "سياسات وتعليمات الامتحانات" : "Exam Policies & Guidelines";
            return View();
        }

        public IActionResult Support()
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? "الدعم الفني للامتحانات" : "Exam Technical Support";
            return View();
        }

        // ===== CRUD: Exams =====
        [HttpGet]
        public async Task<IActionResult> Create(string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            await LoadDepartmentsAsync();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");

            // حط قسم افتراضي مبدئيًا (لو حبيت)
            var firstDepId = await _db.Departments.OrderBy(d => d.NameEn).Select(d => d.Id).FirstOrDefaultAsync();
            return View(new Exam { DepartmentId = firstDepId, DateTime = DateTime.UtcNow.AddDays(7) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exam model, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            // نتجاهل الملاحة لأننا بنرسل DepartmentId فقط
            ModelState.Remove(nameof(Exam.Department));

            if (model.DepartmentId <= 0)
                ModelState.AddModelError("DepartmentId", "Select a department.");

            if (!ModelState.IsValid)
            {
                ViewBag.FormErrors = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await LoadDepartmentsAsync();
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                return View(model);
            }

            _db.Exams.Add(model);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Exam added.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var exam = await _db.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            await LoadDepartmentsAsync();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
            return View(exam);
        }

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

            existing.BusinessId = string.IsNullOrWhiteSpace(model.BusinessId)
                ? existing.BusinessId
                : model.BusinessId.Trim();

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
                ViewBag.FormErrors = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                await LoadDepartmentsAsync();
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Index");
                return View(existing);
            }

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Exam updated.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var m = await _db.Exams.FindAsync(id);
            if (m == null) return NotFound();

            _db.Exams.Remove(m);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Exam deleted.";
            return Redirect(returnUrl ?? Url.Action("Index")!);
        }

        // ===== Archive =====
        public async Task<IActionResult> Archive()
        {
            var isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
            ViewData["Title"] = isRtl ? "نماذج امتحانات سابقة" : "Past Exam Papers";
            ViewBag.IsAdmin = IsAdmin();

            var list = await _db.ExamArchiveItems
                .AsNoTracking()
                .OrderByDescending(x => x.Term)
                .ToListAsync();

            return View(list);
        }

        [HttpGet]
        public IActionResult ArchiveCreate(string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Archive");
            return View(new ExamArchiveItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ArchiveCreate(ExamArchiveItem model, IFormFile? paper, IFormFile? solution, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            // نحفظ الملفات لو أُرفقت
            var paperPath = await SavePdfAsync(paper);
            var solutionPath = await SavePdfAsync(solution);

            if (paper != null && paperPath == null)
                ModelState.AddModelError("PdfUrl", "Invalid paper file.");
            if (solution != null && solutionPath == null)
                ModelState.AddModelError("SolutionUrl", "Invalid solution file.");

            model.PdfUrl = paperPath ?? model.PdfUrl;
            model.SolutionUrl = solutionPath ?? model.SolutionUrl;

            if (!ModelState.IsValid)
            {
                ViewBag.FormErrors = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Archive");
                return View(model);
            }

            _db.ExamArchiveItems.Add(model);
            await _db.SaveChangesAsync();
            TempData["Ok"] = "Archive entry added.";
            return Redirect(returnUrl ?? Url.Action("Archive")!);
        }

        [HttpGet]
        public async Task<IActionResult> ArchiveEdit(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var item = await _db.ExamArchiveItems.FindAsync(id);
            if (item == null) return NotFound();

            ViewBag.ReturnUrl = returnUrl ?? Url.Action("Archive");
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100_000_000)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ArchiveEdit(int id, ExamArchiveItem model, IFormFile? paper, IFormFile? solution, bool? clearSolution, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();
            if (id != model.Id) return BadRequest();

            var existing = await _db.ExamArchiveItems.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null) return NotFound();

            existing.CourseCode = model.CourseCode;
            existing.CourseNameAr = model.CourseNameAr;
            existing.CourseNameEn = model.CourseNameEn;
            existing.Term = model.Term;

            var paperPath = await SavePdfAsync(paper);
            var solutionPath = await SavePdfAsync(solution);

            if (paper != null && paperPath == null)
                ModelState.AddModelError("PdfUrl", "Invalid paper file.");
            if (solution != null && solutionPath == null)
                ModelState.AddModelError("SolutionUrl", "Invalid solution file.");

            if (paperPath != null) existing.PdfUrl = paperPath;
            if (solutionPath != null) existing.SolutionUrl = solutionPath;
            if (clearSolution == true) existing.SolutionUrl = null;

            if (!ModelState.IsValid)
            {
                ViewBag.FormErrors = string.Join(" | ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ViewBag.ReturnUrl = returnUrl ?? Url.Action("Archive");
                return View(existing);
            }

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Archive entry updated.";
            return Redirect(returnUrl ?? Url.Action("Archive")!);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveDelete(int id, string? returnUrl = null)
        {
            if (!IsAdmin()) return Forbid();

            var item = await _db.ExamArchiveItems.FindAsync(id);
            if (item == null) return NotFound();

            _db.ExamArchiveItems.Remove(item);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Archive entry deleted.";
            return Redirect(returnUrl ?? Url.Action("Archive")!);
        }
    }
}
