using NArchitecture.Core.Security.Authenticator.Otp.OtpNet;
using Shouldly;

namespace NArchitecture.Core.Security.Tests.Authenticator.Otp.OtpNet;

public class OtpNetOtpServiceTests
{
    private readonly OtpNetOtpService _sut;

    public OtpNetOtpServiceTests()
    {
        _sut = new OtpNetOtpService();
    }

    [Fact(DisplayName = "Should Generate Valid Secret Key")]
    [Trait("Category", "Secret Key Generation")]
    public void GenerateSecretKey_ShouldReturnValidKey()
    {
        // Act
        byte[] result = _sut.GenerateSecretKey([]);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Length.ShouldBeGreaterThan(0);
    }

    [Theory(DisplayName = "Should Convert Secret Key To Valid Base32 String")]
    [Trait("Category", "Secret Key Conversion")]
    [InlineData(new byte[] { 65, 66, 67, 68 })] // "ABCD"
    public void ConvertSecretKeyToString_ShouldReturnValidBase32String(byte[] secretKey)
    {
        // Act
        string result = _sut.ConvertSecretKeyToString(secretKey);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.ShouldMatch(@"^[A-Z2-7]+=*$"); // Base32 format check
    }

    [Fact(DisplayName = "Should Throw When Secret Key Is Null For Conversion")]
    [Trait("Category", "Secret Key Conversion")]
    public void ConvertSecretKeyToString_ShouldThrowArgumentNullException_WhenSecretKeyIsNull()
    {
        // Act & Assert
        _ = Should.Throw<ArgumentNullException>(() => _sut.ConvertSecretKeyToString(null!));
    }

    [Theory(DisplayName = "Should Handle Empty Secret Key")]
    [Trait("Category", "Secret Key Conversion")]
    [InlineData(new byte[] { })]
    public void ConvertSecretKeyToString_ShouldReturnEmptyString_ForEmptySecretKey(byte[] emptyKey)
    {
        // Act
        string result = _sut.ConvertSecretKeyToString(emptyKey);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact(DisplayName = "Should Compute Valid OTP")]
    [Trait("Category", "OTP Computation")]
    public void ComputeOtp_ShouldReturnValidOtp()
    {
        // Arrange
        byte[] secretKey = _sut.GenerateSecretKey([]);

        // Act
        string result = _sut.ComputeOtp(secretKey);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        result.Length.ShouldBe(6);
        result.ShouldMatch(@"^\d{6}$");
    }

    [Fact(DisplayName = "Should Throw When Secret Key Is Null")]
    [Trait("Category", "OTP Computation")]
    public void ComputeOtp_ShouldThrowArgumentNullException_WhenSecretKeyIsNull()
    {
        // Act & Assert
        _ = Should.Throw<ArgumentNullException>(() => _sut.ComputeOtp(null!));
    }

    [Fact(DisplayName = "Should Generate Different OTPs For Different Time Windows")]
    [Trait("Category", "OTP Computation")]
    public void ComputeOtp_ShouldGenerateDifferentOtps_ForDifferentTimeWindows()
    {
        // Arrange
        byte[] secretKey = _sut.GenerateSecretKey([]);
        DateTime now = DateTime.UtcNow;

        // Act
        string firstOtp = _sut.ComputeOtp(secretKey, now);
        string secondOtp = _sut.ComputeOtp(secretKey, now.AddSeconds(31));

        // Assert
        firstOtp.ShouldNotBe(secondOtp);
    }

    [Fact(DisplayName = "Should Generate Different Secret Keys")]
    [Trait("Category", "Secret Key Generation")]
    public void GenerateSecretKey_ShouldGenerateDifferentKeys_ForMultipleCalls()
    {
        // Act
        byte[] firstKey = _sut.GenerateSecretKey([]);
        byte[] secondKey = _sut.GenerateSecretKey([]);

        // Assert
        firstKey.ShouldNotBe(secondKey);
    }

    [Theory(DisplayName = "Should Handle Empty Secret Key")]
    [Trait("Category", "Secret Key Conversion")]
    [InlineData(new byte[] { })]
    public void ConvertSecretKeyToString_ShouldHandleEmptySecretKey(byte[] emptyKey)
    {
        // Act
        string result = _sut.ConvertSecretKeyToString(emptyKey);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "Should Generate Same OTP For Same Time")]
    [Trait("Category", "OTP Computation")]
    public void ComputeOtp_ShouldGenerateSameOtp_ForSameTime()
    {
        // Arrange
        byte[] secretKey = _sut.GenerateSecretKey([]);
        DateTime specificTime = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        string firstOtp = _sut.ComputeOtp(secretKey, specificTime);
        string secondOtp = _sut.ComputeOtp(secretKey, specificTime);

        // Assert
        firstOtp.ShouldBe(secondOtp);
    }

    [Fact(DisplayName = "Should Generate Different OTPs For Different Times")]
    [Trait("Category", "OTP Computation")]
    public void ComputeOtp_ShouldGenerateDifferentOtps_ForDifferentTimes()
    {
        // Arrange
        byte[] secretKey = _sut.GenerateSecretKey([]);
        DateTime firstTime = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime secondTime = firstTime.AddSeconds(31); // Different 30-second window

        // Act
        string firstOtp = _sut.ComputeOtp(secretKey, firstTime);
        string secondOtp = _sut.ComputeOtp(secretKey, secondTime);

        // Assert
        firstOtp.ShouldNotBe(secondOtp);
    }
}
