using System.ComponentModel.DataAnnotations;

namespace MutaEngineering.Models
{
    public enum SupportIssueType { CannotAccessExam = 0, AccountProblem = 1, ContentError = 2, Other = 9 }

    public class SupportTicket
    {
        public int Id { get; set; }

        // من نموذج الطالب
        [Required, MaxLength(200)] public string FullName { get; set; } = default!;
        [MaxLength(50)] public string? UniversityId { get; set; }
        [Required, EmailAddress, MaxLength(320)] public string Email { get; set; } = default!;
        [MaxLength(200)] public string? CourseExam { get; set; }
        public SupportIssueType IssueType { get; set; } = SupportIssueType.CannotAccessExam;
        [Required, MaxLength(2000)] public string Description { get; set; } = default!;
        [MaxLength(260)] public string? ScreenshotPath { get; set; }

        // إدارة
        [MaxLength(4000)] public string? AdminReply { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RepliedAt { get; set; }
        public bool IsResolved { get; set; } = false;
    }
}
