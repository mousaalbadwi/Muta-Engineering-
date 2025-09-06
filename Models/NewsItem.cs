using System.ComponentModel.DataAnnotations;

namespace MutaEngineering.Models
{
    public class NewsItem
    {
        public int Id { get; set; }

        [Required, MaxLength(250)]
        public string TitleAr { get; set; } = default!;

        [Required, MaxLength(250)]
        public string TitleEn { get; set; } = default!;

        public string? BodyAr { get; set; }
        public string? BodyEn { get; set; }

        public NewsCategory Category { get; set; } = NewsCategory.Announcement;
        public DateTime PublishDate { get; set; } = DateTime.UtcNow;

        [MaxLength(260)]
        public string? ImagePath { get; set; }

        public bool IsPublished { get; set; } = true;
    }
}
