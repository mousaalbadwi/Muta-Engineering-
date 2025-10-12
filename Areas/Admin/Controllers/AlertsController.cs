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
    public class AlertsController : Controller
    {
        private readonly AppDbContext _context;
        public AlertsController(AppDbContext context) => _context = context;

        private void FillLookups(int? selectedDeptId = null)
        {
            ViewBag.Departments = new SelectList(
                _context.Departments.OrderBy(d => d.NameEn).ToList(),
                "Id", "NameEn", selectedDeptId
            );
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.AcademicAlerts
                .Include(a => a.Department)
                .OrderByDescending(a => a.Date)
                .ToListAsync();
            return View(data);
        }

        public IActionResult Create()
        {
            FillLookups();
            return View(new AcademicAlert { Date = DateTime.Now });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AcademicAlert model)
        {
            if (string.IsNullOrWhiteSpace(model.TitleAr))
                model.TitleAr = model.TitleEn;

            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                FillLookups(model.DepartmentId);
                return View(model);
            }

            _context.AcademicAlerts.Add(model);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Alert created.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var entity = await _context.AcademicAlerts.FindAsync(id);
            if (entity is null) return NotFound();
            FillLookups(entity.DepartmentId);
            return View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AcademicAlert model)
        {
            if (id != model.Id) return NotFound();

            if (string.IsNullOrWhiteSpace(model.TitleAr))
                model.TitleAr = model.TitleEn;

            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                FillLookups(model.DepartmentId);
                return View(model);
            }

            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Alert updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            var entity = await _context.AcademicAlerts
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.AcademicAlerts.FindAsync(id);
            if (entity is null) return NotFound();
            _context.AcademicAlerts.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Alert deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
