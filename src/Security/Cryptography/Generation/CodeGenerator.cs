using System.Security.Cryptography;
using System.Text;
using NArchitecture.Core.Security.Abstractions.Cryptography.Generation;

namespace NArchitecture.Core.Security.Cryptography.Generation;

public class CodeGenerator : ICodeGenerator
{
    private static readonly char[] AlphanumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public string GenerateNumeric(int length, byte[]? seed = null)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be positive.", nameof(length));

        StringBuilder result = new(length);
        byte[] randomBytes = GenerateRandomBytes(length, seed);

        for (int i = 0; i < length; i++)
        {
            int digit = randomBytes[i] % 10;
            _ = result.Append(digit);
        }

        return result.ToString();
    }

    public string GenerateAlphanumeric(int length, byte[]? seed = null)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be positive.", nameof(length));

        byte[] randomBytes = GenerateRandomBytes(length, seed);
        char[] buffer = new char[length];

        for (int i = 0; i < length; i++)
            buffer[i] = AlphanumericChars[randomBytes[i] % AlphanumericChars.Length];

        return new string(buffer);
    }

    public string GenerateBase64(int byteLength, byte[]? seed = null)
    {
        if (byteLength <= 0)
            throw new ArgumentException("Byte length must be positive.", nameof(byteLength));

        byte[] randomBytes = GenerateRandomBytes(byteLength, seed);
        return Convert.ToBase64String(randomBytes);
    }

    private static byte[] GenerateRandomBytes(int length, byte[]? seed)
    {
        byte[] randomBytes = new byte[length];

        if (seed is null || seed.Length == 0)
        {
            RandomNumberGenerator.Fill(randomBytes);
            return randomBytes;
        }

        using var hmac = new HMACSHA256(seed);
        byte[] hash = hmac.ComputeHash(BitConverter.GetBytes(DateTime.UtcNow.Ticks));
        Array.Copy(hash, randomBytes, Math.Min(hash.Length, length));

        return randomBytes;
    }
}
