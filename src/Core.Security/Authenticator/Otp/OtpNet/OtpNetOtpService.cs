using NArchitecture.Core.Security.Abstractions.Authenticator.Otp;
using OtpNet;

namespace NArchitecture.Core.Security.Authenticator.Otp.OtpNet;

public class OtpNetOtpService : IOtpService
{
    public byte[] GenerateSecretKey(byte[] secretKey, CancellationToken cancellationToken = default)
    {
        byte[] key = KeyGeneration.GenerateRandomKey(20);
        string base32String = Base32Encoding.ToString(key);
        byte[] base32Bytes = Base32Encoding.ToBytes(base32String);
        return base32Bytes;
    }

    public string ConvertSecretKeyToString(byte[] secretKey, CancellationToken cancellationToken = default)
    {
        string base32String = Base32Encoding.ToString(secretKey);
        return base32String;
    }

    public string ComputeOtp(byte[] secretKey, CancellationToken cancellationToken = default)
    {
        if (secretKey is null)
            throw new ArgumentNullException(nameof(secretKey));

        Totp totp = new(secretKey);
        string totpCode = totp.ComputeTotp(DateTime.UtcNow);
        return totpCode;
    }
}
