using System.ComponentModel.DataAnnotations;
using MutaEngineering.Models;

namespace MutaEngineering.ViewModels
{
    public class SupportTicketInput
    {
        [Required, MaxLength(200)] public string FullName { get; set; } = default!;
        [MaxLength(50)] public string? UniversityId { get; set; }
        [Required, EmailAddress, MaxLength(320)] public string Email { get; set; } = default!;
        [MaxLength(200)] public string? CourseExam { get; set; }
        public SupportIssueType IssueType { get; set; } = SupportIssueType.CannotAccessExam;
        [Required, MaxLength(2000)] public string Description { get; set; } = default!;
        public IFormFile? Screenshot { get; set; }
    }
}
