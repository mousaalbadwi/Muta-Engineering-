using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.Models;
using MutaEngineering.ViewModels;

namespace MutaEngineering.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SupportTicketsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IEmailSender _email;

        public SupportTicketsController(AppDbContext db, IEmailSender email)
        {
            _db = db; _email = email;
        }

        // /Admin/SupportTickets?page=1&pageSize=10&q=keyword
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? q = null)
        {
            if (page < 1) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var query = _db.SupportTickets.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(t =>
                    t.FullName.Contains(q) ||
                    t.Email!.Contains(q) ||
                    (t.CourseExam ?? "").Contains(q) ||
                    (t.Description ?? "").Contains(q));
            }

            query = query.OrderByDescending(t => t.CreatedAt);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var model = new PagedResult<SupportTicket>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Query = q
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var t = await _db.SupportTickets.FirstOrDefaultAsync(x => x.Id == id);
            if (t is null) return NotFound();
            return View(t);
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int page = 1, int pageSize = 10, string? q = null)
        {
            var t = await _db.SupportTickets.FindAsync(id);
            if (t == null)
                return NotFound();

            _db.SupportTickets.Remove(t);
            await _db.SaveChangesAsync();

            TempData["SwalIcon"] = "success";
            TempData["SwalTitle"] = "تم الحذف";
            TempData["SwalText"] = $"تم حذف التذكرة #{id}.";

            // ارجع لنفس الصفحة ومع نفس البحث
            return RedirectToAction(nameof(Index), new { page, pageSize, q });
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string reply, bool markResolved = true)
        {
            var t = await _db.SupportTickets.FirstOrDefaultAsync(x => x.Id == id);
            if (t is null) return NotFound();

            t.AdminReply = reply;
            t.RepliedAt = DateTime.UtcNow;
            if (markResolved) t.IsResolved = true;
            await _db.SaveChangesAsync();

            await _email.SendAsync(
                to: t.Email!,
                subject: "Mutah Engineering Support – الرد على بلاغك",
                htmlBody: $@"<p>مرحباً {t.FullName},</p>
<p>بخصوص بلاغك: <strong>{t.IssueType}</strong></p>
<p>{System.Net.WebUtility.HtmlEncode(reply).Replace("\n", "<br/>")}</p>
<hr/><p>جامعة مؤتة – كلية الهندسة</p>"
            );

            TempData["Ok"] = "تم إرسال الردّ إلى البريد.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
