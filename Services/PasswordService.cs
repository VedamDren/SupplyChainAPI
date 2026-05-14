using BCrypt.Net;

namespace SupplyChainAPI.Services
{
    public class PasswordService : IPasswordService
    {
        public string HashPassword(string password)
        {
            // Используем стандартный HashPassword (по умолчанию workFactor = 11)
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}