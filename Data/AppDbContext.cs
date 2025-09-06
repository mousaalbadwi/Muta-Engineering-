using Microsoft.EntityFrameworkCore;
using MutaEngineering.Models;

namespace MutaEngineering.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<FacultyMember> FacultyMembers => Set<FacultyMember>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<ExamArchiveItem> ExamArchiveItems => Set<ExamArchiveItem>();
        public DbSet<AcademicAlert> AcademicAlerts => Set<AcademicAlert>();
        public DbSet<NewsItem> NewsItems => Set<NewsItem>();
        // ملاحظة: لا نضيف StudyPlan كـ DbSet إذا كان يعتمد على ملفات PDF فقط.
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // فهارس مفيدة
            modelBuilder.Entity<Exam>()
                .HasIndex(e => new { e.CourseCode, e.DateTime });

            modelBuilder.Entity<Department>()
                .HasIndex(d => d.Code)
                .IsUnique(false);

            // علاقات
            modelBuilder.Entity<Department>()
                .HasMany(d => d.FacultyMembers)
                .WithOne(f => f.Department)
                .HasForeignKey(f => f.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>()
                .HasMany(d => d.Exams)
                .WithOne(e => e.Department)
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
