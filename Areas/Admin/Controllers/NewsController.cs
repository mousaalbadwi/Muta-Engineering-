using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;

namespace MutaEngineering.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NewsController : Controller
    {
        private readonly AppDbContext _context;
        public NewsController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var data = await _context.NewsItems
                .OrderByDescending(n => n.PublishDate)
                .ToListAsync();
            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new NewsItem { PublishDate = DateTime.Today, IsPublished = true });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsItem model)
        {
           
            if (string.IsNullOrWhiteSpace(model.TitleAr)) model.TitleAr = model.TitleEn;
            if (string.IsNullOrWhiteSpace(model.BodyAr)) model.BodyAr = model.BodyEn;

            if (!ModelState.IsValid) return View(model);

            _context.NewsItems.Add(model);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "News item created.";
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var entity = await _context.NewsItems.FindAsync(id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NewsItem model)
        {
            if (id != model.Id) return NotFound();

            if (string.IsNullOrWhiteSpace(model.TitleAr)) model.TitleAr = model.TitleEn;
            if (string.IsNullOrWhiteSpace(model.BodyAr)) model.BodyAr = model.BodyEn;

            if (!ModelState.IsValid) return View(model);

            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "News item updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            var entity = await _context.NewsItems.FirstOrDefaultAsync(n => n.Id == id);
            if (entity is null) return NotFound();
            return View(entity);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.NewsItems.FindAsync(id);
            if (entity is null) return NotFound();
            _context.NewsItems.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["Ok"] = "News item deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
