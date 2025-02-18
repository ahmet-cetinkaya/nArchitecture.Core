namespace NArchitecture.Core.Security.Abstractions.Authenticator.Otp;

public interface IOtpService
{
    byte[] GenerateSecretKey(byte[] secretKey, CancellationToken cancellationToken = default);
    string ConvertSecretKeyToString(byte[] secretKey, CancellationToken cancellationToken = default);
    string ComputeOtp(byte[] secretKey, CancellationToken cancellationToken = default);
}
