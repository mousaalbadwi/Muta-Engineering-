using System.ComponentModel.DataAnnotations;

namespace MutaEngineering.Models
{
    public class User
    {
        public int Id { get; set; }

        // نستخدمه لهواتف (التسجيل اليدوي) أو بريد (التسجيل الخارجي)
        [Required, MaxLength(120)]
        public string Username { get; set; } = default!;   // phone or email

        [MaxLength(200)]
        public string? FullName { get; set; }

        // للتسجيل اليدوي فقط
        public string? PasswordHash { get; set; }

        [MaxLength(50)]
        public string Role { get; set; } = "User";

        // حقول اختيارية للتسجيل الخارجي
        [MaxLength(50)]
        public string? Provider { get; set; }              // Google/GitHub/Facebook

        [MaxLength(200)]
        public string? ProviderKey { get; set; }           // sub/id من الموفّر

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
