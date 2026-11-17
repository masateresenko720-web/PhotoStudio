using System;
using System.Security.Cryptography;

namespace PhotoStudio.Core;
public static class PasswordService
{
    private const int Iterations = 600000;
    public static string Hash(string password)
    {
        if (password.Length is < 12 or > 128)
            throw new ArgumentException("Пароль: 12–128 символов");
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Iterations}.{Convert.ToBase64String(salt)}." + Convert.ToBase64String(hash);
    }

    public static bool Verify(string password, string stored)
    {
        if (password.Length > 128)
            return false;
        try
        {
            var parts = stored.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out int count) || count < 100000 || count > 1000000)
                return false;
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] hash = Convert.FromBase64String(parts[2]);
            if (salt.Length != 16 || hash.Length != 32)
                return false;
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, count, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(hash, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
