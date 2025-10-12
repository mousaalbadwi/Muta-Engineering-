using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MutaEngineering.Data;
using MutaEngineering.ViewModels;

namespace MutaEngineering.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel
            {
                DepartmentsCount = await _context.Departments.CountAsync(),
                FacultyCount = await _context.FacultyMembers.CountAsync(),
                ExamsCount = await _context.Exams.CountAsync(),
                AlertsCount = await _context.AcademicAlerts.CountAsync(),
                NewsCount = await _context.NewsItems.CountAsync(),
                RecentActivities = new List<RecentActivity>()
            };

            // نجيب آخر نشاطات من الأخبار أو الامتحانات أو التنبيهات
            var recentNews = await _context.NewsItems
                .OrderByDescending(n => n.PublishDate)
                .Take(3)
                .Select(n => new RecentActivity
                {
                    Type = "News",
                    Title = n.TitleAr,
                    Date = n.PublishDate
                })
                .ToListAsync();

            var recentExams = await _context.Exams
                .OrderByDescending(e => e.DateTime)
                .Take(3)
                .Select(e => new RecentActivity
                {
                    Type = "Exam",
                    Title = e.CourseNameAr,
                    Date = e.DateTime
                })
                .ToListAsync();

            model.RecentActivities.AddRange(recentNews);
            model.RecentActivities.AddRange(recentExams);
            model.RecentActivities = model.RecentActivities
                .OrderByDescending(r => r.Date)
                .Take(5)
                .ToList();

            return View("~/Views/Admin/Dashboard.cshtml", model);
        }
    }
}
