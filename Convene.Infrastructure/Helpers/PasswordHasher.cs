using BCrypt.Net;

namespace Convene.Infrastructure.Helpers
{
    public class PasswordHasher
    {
        // Hash a plain password
        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Verify a plain password against a hash
        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
    }
}
