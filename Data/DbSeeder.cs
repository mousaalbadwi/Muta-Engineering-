using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MutaEngineering.Models;

namespace MutaEngineering.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // طبّق المايغريشنز إن لزم
            if ((await db.Database.GetPendingMigrationsAsync()).Any())
                await db.Database.MigrateAsync();

            // ============================================================
            // 1) Admin Bootstrap (اختياري عبر appsettings.json)
            //    Admin:Bootstrap:Username / Password / FullName
            //    إن لم تتوفر، نستخدم قيماً افتراضية آمنة ويمكن تغييرها لاحقًا
            // ============================================================
            var adminUsername = cfg.GetValue<string>("Admin:Bootstrap:Username") ?? "admin@mutah.edu.jo";
            var adminPassword = cfg.GetValue<string>("Admin:Bootstrap:Password"); // إن كانت null سنولّد كلمة افتراضية
            var adminFullName = cfg.GetValue<string>("Admin:Bootstrap:FullName") ?? "Site Administrator";

            if (!await db.Users.AnyAsync(u => u.Username == adminUsername))
            {
                var pwd = string.IsNullOrWhiteSpace(adminPassword) ? "ChangeMe!123" : adminPassword!;
                var admin = new User
                {
                    Username = adminUsername,
                    FullName = adminFullName,
                    Role = "Admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd)
                };
                db.Users.Add(admin);
                await db.SaveChangesAsync();
            }

            // ============================================================
            // 2) الأقسام (Departments)
            // ============================================================
            if (!await db.Departments.AnyAsync())
            {
                var deps = new[]
                {
                    new Department { Code="CIV",  NameAr="الهندسة المدنية",     NameEn="Civil Engineering" },
                    new Department { Code="MECH", NameAr="الهندسة الميكانيكية", NameEn="Mechanical Engineering" },
                    new Department { Code="ELEC", NameAr="الهندسة الكهربائية",  NameEn="Electrical Engineering" },
                    new Department { Code="COMP", NameAr="هندسة الحاسوب",       NameEn="Computer Engineering" },
                    new Department { Code="IND",  NameAr="الهندسة الصناعية",    NameEn="Industrial Engineering" },
                    new Department { Code="CHEM", NameAr="الهندسة الكيميائية",  NameEn="Chemical Engineering" },
                };
                db.Departments.AddRange(deps);
                await db.SaveChangesAsync();
            }

            // حضّر قاموس الأقسام (Code -> Id) للاستخدام اللاحق
            var depCodes = await db.Departments
                                   .Select(d => new { d.Code, d.Id })
                                   .ToDictionaryAsync(x => x.Code!, x => x.Id);

            // ============================================================
            // 3) أعضاء هيئة التدريس (FacultyMembers) — عينات فقط إن الجدول فاضي
            // ============================================================
            if (!await db.FacultyMembers.AnyAsync())
            {
                db.FacultyMembers.AddRange(
                    new FacultyMember
                    {
                        FullNameAr = "د. أحمد علي",
                        FullNameEn = "Dr. Ahmad Ali",
                        TitleAr = "أستاذ مشارك",
                        TitleEn = "Associate Professor",
                        Email = "ahmad.ali@mutah.edu.jo",
                        Office = "C-115",
                        PhotoPath = "~/img/faculty/avatar-placeholder.svg",
                        DepartmentId = depCodes.GetValueOrDefault("ELEC")
                    },
                    new FacultyMember
                    {
                        FullNameAr = "د. هبة زيدان",
                        FullNameEn = "Dr. Heba Zeidan",
                        TitleAr = "أستاذ مساعد",
                        TitleEn = "Assistant Professor",
                        Email = "heba.zeidan@mutah.edu.jo",
                        Office = "A-210",
                        PhotoPath = "~/img/faculty/avatar-placeholder.svg",
                        DepartmentId = depCodes.GetValueOrDefault("COMP")
                    },
                    new FacultyMember
                    {
                        FullNameAr = "د. موسى البدوي",
                        FullNameEn = "Dr. Mousa Alabdwi",
                        TitleAr = "أستاذ مساعد",
                        TitleEn = "Assistant Professor",
                        Email = "moeyad2003@gmail.com",
                        Office = "A-10",
                        PhotoPath = "~/img/faculty/mousa.jpg",
                        DepartmentId = depCodes.GetValueOrDefault("COMP")
                    },
                    new FacultyMember
                    {
                        FullNameAr = "د. رنيم أسعد",
                        FullNameEn = "Dr. Raneem Asaad",
                        TitleAr = "أستاذ مساعد",
                        TitleEn = "Assistant Professor",
                        Email = "Raneem@mutah.edu.jo",
                        Office = "A-210",
                        PhotoPath = "~/img/faculty/avatar-placeholder.svg",
                        DepartmentId = depCodes.GetValueOrDefault("COMP")
                    }
                );
                await db.SaveChangesAsync();
            }

            // ============================================================
            // 4) الامتحانات القادمة (Exams) — عينات فقط إن الجدول فاضي
            // ============================================================
            if (!await db.Exams.AnyAsync())
            {
                db.Exams.AddRange(
                    new Exam
                    {
                        BusinessId = "ee-201-circuits-fall-2025",
                        DepartmentId = depCodes.GetValueOrDefault("ELEC"),
                        Year = 2,
                        CourseCode = "EE201",
                        CourseNameAr = "دوائر كهربائية (1)",
                        CourseNameEn = "Electric Circuits I",
                        DateTime = DateTime.UtcNow.AddDays(20).Date.AddHours(9),
                        Mode = ExamMode.InPerson,
                        Location = "C-115",
                        Instructions = "احضر قبل الامتحان بـ 15 دقيقة، الهوية الجامعية مطلوبة.",
                        HasStegoProtection = true
                    },
                    new Exam
                    {
                        BusinessId = "cs-101-prog-online-fall-2025",
                        DepartmentId = depCodes.GetValueOrDefault("COMP"),
                        Year = 1,
                        CourseCode = "CS101",
                        CourseNameAr = "البرمجة (1)",
                        CourseNameEn = "Programming I",
                        DateTime = DateTime.UtcNow.AddDays(12).Date.AddHours(11),
                        Mode = ExamMode.Online,
                        LmsUrl = "https://lms.mutah.edu.jo/course/CS101/exam",
                        LmsHowTo = "سجّل دخولك بحساب الجامعة > اختر المقرر > تبويب الامتحانات.",
                        Instructions = "ممنوع فتح تبويبات أخرى؛ الوقت محسوب تلقائيًا."
                    },
                    new Exam
                    {
                        BusinessId = "ce-210-materials-fall-2025",
                        DepartmentId = depCodes.GetValueOrDefault("CIV"),
                        Year = 2,
                        CourseCode = "CE210",
                        CourseNameAr = "مواد إنشائية",
                        CourseNameEn = "Construction Materials",
                        DateTime = DateTime.UtcNow.AddDays(16).Date.AddHours(13),
                        Mode = ExamMode.InPerson,
                        Location = "A-310"
                    }
                );
                await db.SaveChangesAsync();
            }

            // ============================================================
            // 5) أرشيف الامتحانات (ExamArchiveItems) — عينات إن الجدول فاضي
            // ============================================================
            if (!await db.ExamArchiveItems.AnyAsync())
            {
                db.ExamArchiveItems.AddRange(
                    new ExamArchiveItem
                    {
                        CourseCode = "EE201",
                        CourseNameAr = "دوائر كهربائية (1)",
                        CourseNameEn = "Electric Circuits I",
                        Term = "Spring 2024",
                        PdfUrl = "~/docs/archive/ee201-f25-mid.pdf",
                        SolutionUrl = "~/docs/archive/ee201-f25-mid-sol.pdf"
                    },
                    new ExamArchiveItem
                    {
                        CourseCode = "CS101",
                        CourseNameAr = "البرمجة (1)",
                        CourseNameEn = "Programming I",
                        Term = "Fall 2023",
                        PdfUrl = "~/docs/archive/cs101-f23.pdf"
                    }
                );
                await db.SaveChangesAsync();
            }

            // ============================================================
            // 6) التنبيهات الأكاديمية (AcademicAlerts) — عينات إن الجدول فاضي
            // ============================================================
            if (!await db.AcademicAlerts.AnyAsync())
            {
                db.AcademicAlerts.AddRange(
                    new AcademicAlert
                    {
                        TitleAr = "إعلان موعد امتحان مختبر الدوائر",
                        TitleEn = "Circuit Lab Exam Date",
                        Date = DateTime.UtcNow.AddDays(12),
                        Location = "Hall 202",
                        IsImportant = true,
                        DepartmentId = depCodes.GetValueOrDefault("ELEC")
                    },
                    new AcademicAlert
                    {
                        TitleAr = "ورشة: كتابة تقرير مشروع التخرج",
                        TitleEn = "Workshop: Graduation Project Report",
                        Date = DateTime.UtcNow.AddDays(5).Date.AddHours(11),
                        Location = "Hall 105",
                        DepartmentId = depCodes.GetValueOrDefault("IND")
                    }
                );
                await db.SaveChangesAsync();
            }

            // ============================================================
            // 7) الأخبار (NewsItems) — عينات إن الجدول فاضي
            // ============================================================
            if (!await db.NewsItems.AnyAsync())
            {
                db.NewsItems.AddRange(
                    new NewsItem
                    {
                        TitleAr = "افتتاح مختبر النظم الذكية",
                        TitleEn = "Opening of the Smart Systems Lab",
                        BodyAr = "تم افتتاح مختبر النظم الذكية في الكلية...",
                        BodyEn = "The Smart Systems Lab has been opened...",
                        Category = NewsCategory.Announcement,
                        PublishDate = DateTime.UtcNow.AddDays(-2),
                        ImagePath = "~/img/news/mutahbox.jpg",
                        IsPublished = true
                    },
                    new NewsItem
                    {
                        TitleAr = "ورشة تعلم الآلة",
                        TitleEn = "Machine Learning Workshop",
                        BodyAr = "تنظم الكلية ورشة حول تعلم الآلة...",
                        BodyEn = "The Faculty will host a workshop on Machine Learning...",
                        Category = NewsCategory.Workshop,
                        PublishDate = DateTime.UtcNow.AddDays(-7),
                        ImagePath = "~/img/news/mutahbox2.jpg",
                        IsPublished = true
                    }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
