
using System;
using System.Security.Cryptography;

namespace RussianCheckers.Data.Security
{
    public static class PasswordHasher
    {
        private const int Iterations = 600_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                HashSize
            );

            return $"PBKDF2-SHA256${Iterations}$" +
                   $"{Convert.ToBase64String(salt)}$" +
                   $"{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(
            string password,
            string storedPasswordHash)
        {
            try
            {
                string[] parts = storedPasswordHash.Split('$');

                if (parts.Length != 4 ||
                    parts[0] != "PBKDF2-SHA256")
                {
                    return false;
                }

                if (!int.TryParse(parts[1], out int iterations) ||
                    iterations < 1 ||
                    iterations > 2_000_000)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] savedHash = Convert.FromBase64String(parts[3]);

                if (salt.Length != SaltSize ||
                    savedHash.Length != HashSize)
                {
                    return false;
                }

                byte[] enteredHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    savedHash.Length
                );

                return CryptographicOperations.FixedTimeEquals(
                    enteredHash,
                    savedHash
                );
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
