using System.ComponentModel.DataAnnotations;

namespace MutaEngineering.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة"), MinLength(6)]
        public string? Password { get; set; }

        [Required, Compare(nameof(Password), ErrorMessage = "كلمتا المرور غير متطابقتين")]
        public string? ConfirmPassword { get; set; }
    }
}
