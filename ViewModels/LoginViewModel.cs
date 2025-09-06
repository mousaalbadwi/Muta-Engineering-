using System.ComponentModel.DataAnnotations;

namespace MutaEngineering.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "كلمة المرور مطلوبة")]
        public string? Password { get; set; }
    }
}
