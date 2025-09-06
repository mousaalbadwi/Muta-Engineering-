using System.ComponentModel.DataAnnotations;

namespace MutaEngineering.Models
{
    public class Department
    {
        public int Id { get; set; }

        [MaxLength(10)]
        public string? Code { get; set; }

        [Required, MaxLength(200)]
        public string NameAr { get; set; } = default!;

        [Required, MaxLength(200)]
        public string NameEn { get; set; } = default!;

        [MaxLength(1000)]
        public string? DescriptionAr { get; set; }

        [MaxLength(1000)]
        public string? DescriptionEn { get; set; }

        [MaxLength(260)]
        public string? ImagePath { get; set; }
        public ICollection<FacultyMember> FacultyMembers { get; set; } = new List<FacultyMember>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}
