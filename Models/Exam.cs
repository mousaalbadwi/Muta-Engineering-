using System.ComponentModel.DataAnnotations;

namespace MutaEngineering.Models
{
    public class Exam
    {
        public int Id { get; set; } // DB identity

        [Required, MaxLength(50)]
        public string BusinessId { get; set; } = Guid.NewGuid().ToString("N"); // بديل الـ Id النصي المستخدم سابقًا

        // course
        [Required, MaxLength(20)]
        public string CourseCode { get; set; } = default!;

        [Required, MaxLength(200)]
        public string CourseNameAr { get; set; } = default!;

        [Required, MaxLength(200)]
        public string CourseNameEn { get; set; } = default!;

        public int Year { get; set; }

        // exam
        public DateTime DateTime { get; set; }
        public ExamMode Mode { get; set; } = ExamMode.InPerson;

        [MaxLength(120)]
        public string? Location { get; set; }

        [MaxLength(500)]
        public string? LmsUrl { get; set; }

        [MaxLength(500)]
        public string? LmsHowTo { get; set; }

        [MaxLength(1200)]
        public string? Instructions { get; set; }

        public bool HasStegoProtection { get; set; } = false;

        // FK
        public int DepartmentId { get; set; }
        public Department Department { get; set; } = default!;
    }
}
