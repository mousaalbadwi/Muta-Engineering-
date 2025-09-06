using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace MutaEngineering.Models
{
    public class FacultyMember
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string FullNameAr { get; set; } = default!;

        [Required, MaxLength(200)]
        public string FullNameEn { get; set; } = default!;

        [MaxLength(120)]
        public string? TitleAr { get; set; } // أستاذ، أستاذ مشارك...
        [MaxLength(120)]
        public string? TitleEn { get; set; } // Professor, Associate Professor...

        [EmailAddress, MaxLength(320)]
        public string? Email { get; set; }

        [MaxLength(100)]
        public string? Office { get; set; }

        [MaxLength(260)]
        public string? PhotoPath { get; set; } // relative path under wwwroot

        // FK
        public int DepartmentId { get; set; }

        [ValidateNever]
        public Department Department { get; set; } = default!;
    }
}
