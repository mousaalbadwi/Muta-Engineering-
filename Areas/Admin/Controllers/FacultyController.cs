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
    public class FacultyController : Controller
    {
        private readonly AppDbContext _context;
        public FacultyController(AppDbContext context) => _context = context;

        private void FillLookups(int? selectedDeptId = null)
        {
            ViewBag.Departments = new SelectList(
                _context.Departments.OrderBy(d => d.NameEn).ToList(),
                "Id", "NameEn", selectedDeptId
            );
        }

        public async Task<IActionResult> Index()
        {
            var data = await _context.FacultyMembers
                .Include(f => f.Department)
                .OrderBy(f => f.FullNameEn)
                .ToListAsync();
            return View(data);
        }

        public IActionResult Create()
        {
            FillLookups();
            return View(new FacultyMember());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FacultyMember model)
        {
            if (string.IsNullOrWhiteSpace(model.FullNameAr))
                model.FullNameAr = model.FullNameEn; // fallback

            // re-run validation after fixing fields
            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                FillLookups(model.DepartmentId);
                return View(model);
            }

            _context.FacultyMembers.Add(model);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Faculty member created.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var entity = await _context.FacultyMembers.FindAsync(id);
            if (entity is null) return NotFound();
            FillLookups(entity.DepartmentId);
            return View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FacultyMember model)
        {
            if (id != model.Id) return NotFound();

            if (string.IsNullOrWhiteSpace(model.FullNameAr))
                model.FullNameAr = model.FullNameEn;

            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                FillLookups(model.DepartmentId);
                return View(model);
            }

            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Faculty member updated.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            var entity = await _context.FacultyMembers
                .Include(f => f.Department)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.FacultyMembers.FindAsync(id);
            if (entity is null) return NotFound();
            _context.FacultyMembers.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Faculty member deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
