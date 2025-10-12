using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;

namespace MutaEngineering.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly AppDbContext _context;
        public DepartmentsController(AppDbContext context) => _context = context;

        // GET: /Admin/Departments
        public async Task<IActionResult> Index()
        {
            var data = await _context.Departments
                .Include(d => d.FacultyMembers)
                .Include(d => d.Exams)
                .OrderBy(d => d.NameEn)
                .ToListAsync();

            return View(data);
        }

        // GET: /Admin/Departments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var d = await _context.Departments
                .Include(x => x.FacultyMembers)
                .Include(x => x.Exams)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (d is null) return NotFound();
            return View(d);
        }

        // GET: /Admin/Departments/Create
        public IActionResult Create()
        {
            return View(new Department());
        }

        // POST: /Admin/Departments/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department model)
        {
            if (!ModelState.IsValid) return View(model);

            // uniqueness guard (Code)
            var exists = await _context.Departments.AnyAsync(x => x.Code == model.Code);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Code), "Code is already in use.");
                return View(model);
            }

            _context.Departments.Add(model);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Department created.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Departments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var d = await _context.Departments.FindAsync(id);
            if (d is null) return NotFound();
            return View(d);
        }

        // POST: /Admin/Departments/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            // uniqueness guard on update
            var codeTaken = await _context.Departments
                .AnyAsync(x => x.Id != model.Id && x.Code == model.Code);
            if (codeTaken)
            {
                ModelState.AddModelError(nameof(model.Code), "Code is already in use.");
                return View(model);
            }

            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Ok"] = "Department updated.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Departments.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Departments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();
            var d = await _context.Departments.FirstOrDefaultAsync(x => x.Id == id);
            if (d is null) return NotFound();
            return View(d);
        }

        // POST: /Admin/Departments/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var d = await _context.Departments.FindAsync(id);
            if (d is null) return NotFound();

            _context.Departments.Remove(d);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "Department deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
