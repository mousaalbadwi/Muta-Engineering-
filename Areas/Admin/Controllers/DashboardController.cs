using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.ViewModels;

namespace MutaEngineering.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        public DashboardController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                DepartmentsCount = await _context.Departments.CountAsync(),
                FacultyCount = await _context.FacultyMembers.CountAsync(),
                ExamsCount = await _context.Exams.CountAsync(),
                AlertsCount = await _context.AcademicAlerts.CountAsync(),
                NewsCount = await _context.NewsItems.CountAsync(),
                RecentActivities = new()
            };

            var recentNews = await _context.NewsItems
                .OrderByDescending(n => n.PublishDate)
                .Take(3)
                .Select(n => new RecentActivity { Type = "News", Title = n.TitleEn ?? n.TitleAr, Date = n.PublishDate })
                .ToListAsync();

            var recentExams = await _context.Exams
                .OrderByDescending(e => e.DateTime)
                .Take(3)
                .Select(e => new RecentActivity { Type = "Exam", Title = e.CourseNameEn ?? e.CourseNameAr, Date = e.DateTime })
                .ToListAsync();

            model.RecentActivities.AddRange(recentNews);
            model.RecentActivities.AddRange(recentExams);
            model.RecentActivities = model.RecentActivities
                .OrderByDescending(r => r.Date)
                .Take(5)
                .ToList();

            return View("~/Areas/Admin/Views/Dashboard/Index.cshtml", model);
        }
    }
}
