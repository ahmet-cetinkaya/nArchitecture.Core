using NArchitecture.Core.Security.Abstractions.Authenticator.Otp;
using OtpNet;

namespace NArchitecture.Core.Security.Authenticator.Otp.OtpNet;

public class OtpNetOtpService : IOtpService
{
    public byte[] GenerateSecretKey(byte[] secretKey)
    {
        byte[] key = KeyGeneration.GenerateRandomKey(20);
        string base32String = Base32Encoding.ToString(key);
        byte[] base32Bytes = Base32Encoding.ToBytes(base32String);
        return base32Bytes;
    }

    public string ConvertSecretKeyToString(byte[] secretKey)
    {
        if (secretKey is null)
            throw new ArgumentNullException(nameof(secretKey));
        if (secretKey.Length == 0)
            return string.Empty;

        string base32String = Base32Encoding.ToString(secretKey);
        return base32String;
    }

    public string ComputeOtp(byte[] secretKey, DateTime? time = null)
    {
        if (secretKey is null)
            throw new ArgumentNullException(nameof(secretKey));

        Totp totp = new(secretKey);
        string totpCode = totp.ComputeTotp(time ?? DateTime.UtcNow);
        return totpCode;
    }
}
