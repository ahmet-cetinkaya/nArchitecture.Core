namespace NArchitecture.Core.Security.Abstractions.Authenticator.Otp;

public interface IOtpService
{
    byte[] GenerateSecretKey(byte[] secretKey);
    string ConvertSecretKeyToString(byte[] secretKey);
    string ComputeOtp(byte[] secretKey, DateTime? time = null);
}
