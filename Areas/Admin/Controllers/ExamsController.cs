using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;

namespace MutaEngineering.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ExamsController : Controller
    {
        private readonly AppDbContext _context;
        public ExamsController(AppDbContext context) => _context = context;

        private void FillLookups(int? selectedDeptId = null)
        {
            ViewBag.Departments = new SelectList(
                _context.Departments.OrderBy(d => d.NameEn).ToList(),
                "Id", "NameEn", selectedDeptId
            );
        }

        private void Normalize(Exam m)
        {
            // mirror CourseNameAr if property exists
            var arProp = m.GetType().GetProperty("CourseNameAr");
            var enProp = m.GetType().GetProperty("CourseNameEn");
            if (arProp != null && enProp != null)
            {
                var ar = arProp.GetValue(m) as string;
                var en = enProp.GetValue(m) as string;
                if (string.IsNullOrWhiteSpace(ar) && !string.IsNullOrWhiteSpace(en))
                {
                    arProp.SetValue(m, en);
                    if (ModelState.ContainsKey("CourseNameAr")) ModelState.Remove("CourseNameAr");
                }
            }

            // remove nav validation noise
            if (ModelState.ContainsKey(nameof(Exam.Department))) ModelState.Remove(nameof(Exam.Department));

            // fix DateTime format if binder failed
            if (ModelState.ContainsKey(nameof(Exam.DateTime)) && ModelState[nameof(Exam.DateTime)]!.Errors.Count > 0)
            {
                var raw = Request.Form[nameof(Exam.DateTime)].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var formats = new[] { "yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss" };
                    if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                        || DateTime.TryParse(raw, out dt))
                    {
                        m.DateTime = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
                        ModelState.Remove(nameof(Exam.DateTime));
                    }
                }
            }
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.Exams
                .Include(e => e.Department)
                .OrderByDescending(e => e.DateTime)
                .ToListAsync();
            return View(data);
        }

        public IActionResult Create()
        {
            FillLookups();
            return View(new Exam());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Exam model)
        {
            // mirror AR from EN if property exists
            if (string.IsNullOrWhiteSpace(model.CourseNameAr))
                model.CourseNameAr = model.CourseNameEn;

            // drop stale errors for these keys
            if (ModelState.ContainsKey(nameof(Exam.CourseNameAr))) ModelState.Remove(nameof(Exam.CourseNameAr));

            // navigation prop shouldn't be validated
            if (ModelState.ContainsKey(nameof(Exam.Department))) ModelState.Remove(nameof(Exam.Department));

            // fix datetime if binder failed (optional)
            if (ModelState.ContainsKey(nameof(Exam.DateTime)) && ModelState[nameof(Exam.DateTime)]!.Errors.Count > 0)
                ModelState.Remove(nameof(Exam.DateTime)); // القيمة جايّة صح من input[type=datetime-local]

            if (!ModelState.IsValid)
            {
                FillLookups(model.DepartmentId);
                return View(model);
            }

            _context.Exams.Add(model);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Exam created.";
            return RedirectToAction(nameof(Index));
        }

       
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var entity = await _context.Exams.FindAsync(id);
            if (entity is null) return NotFound();
            FillLookups(entity.DepartmentId);
            return View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Exam model)
        {
            if (id != model.Id) return NotFound();

            if (string.IsNullOrWhiteSpace(model.CourseNameAr))
                model.CourseNameAr = model.CourseNameEn;

            if (ModelState.ContainsKey(nameof(Exam.CourseNameAr))) ModelState.Remove(nameof(Exam.CourseNameAr));
            if (ModelState.ContainsKey(nameof(Exam.Department))) ModelState.Remove(nameof(Exam.Department));
            if (ModelState.ContainsKey(nameof(Exam.DateTime))) ModelState.Remove(nameof(Exam.DateTime));

            if (!ModelState.IsValid)
            {
                FillLookups(model.DepartmentId);
                return View(model);
            }

            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Exam updated.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            var entity = await _context.Exams
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Exams.FindAsync(id);
            if (entity is null) return NotFound();
            _context.Exams.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Exam deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
