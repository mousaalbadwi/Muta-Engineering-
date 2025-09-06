using System.ComponentModel.DataAnnotations;

namespace MutaEngineering.Models
{
    public class AcademicAlert
    {
        public int Id { get; set; }

        [Required, MaxLength(300)]
        public string TitleAr { get; set; } = default!;

        [Required, MaxLength(300)]
        public string TitleEn { get; set; } = default!;

        [MaxLength(150)]
        public string? Location { get; set; }

        public DateTime? Date { get; set; }
        public bool IsImportant { get; set; } = false;

        // اختياري ربط مع قسم
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
    }
}
