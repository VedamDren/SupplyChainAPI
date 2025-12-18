using System.ComponentModel.DataAnnotations;

namespace SupplyChainAPI.Models.Auth
{
    public class RegisterRequestModel
    {
        [Required(ErrorMessage = "Логин обязателен")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        public string Password { get; set; }

        public string Name { get; set; }
    }
}