using System.Security.Cryptography;
using System.Text;

public class PasswordHelper
{
    public static void CreatePasswordHash(string password, out string hash, out byte[] salt)
    {
        using var hmac = new HMACSHA512();

        salt = hmac.Key;  // byte[]

        hash = Convert.ToBase64String(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(password))
        );
    }

    public static bool VerifyPassword(string password, string storedHash, byte[] storedSalt)
    {
        using var hmac = new HMACSHA512(storedSalt);

        var computedHash = Convert.ToBase64String(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(password))
        );

        return computedHash == storedHash;
    }
}