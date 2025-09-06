using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using MutaEngineering.Models;

namespace MutaEngineering.Controllers
{
    public class StudyPlansController : Controller
    {
        // Seed مؤقت — لاحقًا نستبدله بقاعدة بيانات أو قراءة من ملفات
        private static readonly Dictionary<string, string> Departments = new()
        {
            { "civil",  "الهندسة المدنية|Civil Engineering" },
            { "mech",   "الهندسة الميكانيكية|Mechanical Engineering" },
            { "elec",   "الهندسة الكهربائية|Electrical Engineering" },
            { "comp",   "هندسة الحاسوب|Computer Engineering" },
            { "indus",  "الهندسة الصناعية|Industrial Engineering" },
            { "chem",   "الهندسة الكيميائية|Chemical Engineering" }
        };

        private static readonly List<StudyPlan> Plans = new()
        {
            new() { DepartmentSlug="civil", Year=2024, TitleAr="الخطة الدراسية 2024", TitleEn="Study Plan 2024", PdfPath="~/docs/plans/civil/plan-2024.pdf" },
            new() { DepartmentSlug="civil", Year=2025, TitleAr="2025 الخطة الدراسية", TitleEn="Study Plan 2025", PdfPath="~/docs/plans/civil/plan-2025.pdf" },

            new() { DepartmentSlug="mech", Year=2024, TitleAr="الخطة الدراسية 2024", TitleEn="Study Plan 2024", PdfPath="~/docs/plans/mech/plan-2024.pdf" },
            new() { DepartmentSlug="mech", Year=2025, TitleAr="الخطة الدراسية 2025", TitleEn="Study Plan 2025", PdfPath="~/docs/plans/mech/plan-2025.pdf" },
            
            new() { DepartmentSlug="elec", Year=2025, TitleAr="الخطة الدراسية 2025", TitleEn="Study Plan 2025", PdfPath="~/docs/plans/elec/plan-2025.pdf" },
            new() { DepartmentSlug="elec", Year=2024, TitleAr="الخطة الدراسية 2024", TitleEn="Study Plan 2024", PdfPath="~/docs/plans/elec/plan-2024.pdf" },

            new() { DepartmentSlug="comp", Year=2024, TitleAr="الخطة الدراسية 2024", TitleEn="Study Plan 2024", PdfPath="~/docs/plans/comp/plan-2024.pdf" },
            new() { DepartmentSlug="comp", Year=2025, TitleAr="الخطة الدراسية 2025", TitleEn="Study Plan 2025", PdfPath="~/docs/plans/comp/plan-2025.pdf" },
          
            new() { DepartmentSlug="indus", Year=2024, TitleAr="الخطة الدراسية 2024", TitleEn="Study Plan 2024", PdfPath="~/docs/plans/indus/plan-2024.pdf" },
            new() { DepartmentSlug="indus", Year=2025, TitleAr="الخطة الدراسية 2025", TitleEn="Study Plan 2025", PdfPath="~/docs/plans/indus/plan-2025.pdf" },

            new() { DepartmentSlug="chem", Year=2024, TitleAr="الخطة الدراسية 2024", TitleEn="Study Plan 2024", PdfPath="~/docs/plans/chem/plan-2024.pdf" },
            new() { DepartmentSlug="chem", Year=2025, TitleAr="الخطة الدراسية 2025", TitleEn="Study Plan 2025", PdfPath="~/docs/plans/chem/plan-2025.pdf" },
        };

        public IActionResult Index()
        {
            ViewData["Title"] = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft
                ? "الخطط الدراسية"
                : "Study Plans";

            // نحضّر ViewModel بسيط: الأقسام + الخطط
            var vm = new StudyPlansVM
            {
                Departments = Departments,
                Plans = Plans.OrderByDescending(p => p.Year).ToList()
            };
            return View(vm);
        }

        // عرض PDF داخل صفحة خفيفة (embed)
        public IActionResult ViewPdf(string dep, int year)
        {
            var plan = Plans.FirstOrDefault(p => p.DepartmentSlug == dep && p.Year == year);
            if (plan is null) return NotFound();
            return View(plan);
        }
    }

    public class StudyPlansVM
    {
        public Dictionary<string, string> Departments { get; set; } = default!;
        public List<StudyPlan> Plans { get; set; } = default!;
    }
}
