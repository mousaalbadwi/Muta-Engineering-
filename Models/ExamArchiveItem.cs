using System.ComponentModel.DataAnnotations;

namespace MutaEngineering.Models
{
    public class ExamArchiveItem
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string CourseCode { get; set; } = default!;

        [Required, MaxLength(200)]
        public string CourseNameAr { get; set; } = default!;

        [Required, MaxLength(200)]
        public string CourseNameEn { get; set; } = default!;

        [MaxLength(50)]
        public string? Term { get; set; } // Spring 2024, Fall 2023 ...

        [MaxLength(260)]
        public string? PdfUrl { get; set; } // "~/docs/archive/xxx.pdf"

        [MaxLength(260)]
        public string? SolutionUrl { get; set; }
    }
}
