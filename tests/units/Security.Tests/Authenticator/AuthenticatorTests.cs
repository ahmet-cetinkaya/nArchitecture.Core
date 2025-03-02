using System.Linq.Expressions;
using Moq;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Mailing.Abstractions;
using NArchitecture.Core.Mailing.Abstractions.Models;
using NArchitecture.Core.Security.Abstractions.Authenticator;
using NArchitecture.Core.Security.Abstractions.Authenticator.Entities;
using NArchitecture.Core.Security.Abstractions.Authenticator.Enums;
using NArchitecture.Core.Security.Abstractions.Authenticator.Otp;
using NArchitecture.Core.Security.Abstractions.Cryptography.Generation;
using NArchitecture.Core.Security.Authenticator;
using NArchitecture.Core.Sms.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Security.Tests.Authenticator;

public class AuthenticatorTests
{
    private readonly Mock<IUserAuthenticatorRepository<Guid, Guid, Guid, Guid, Guid, Guid, Guid>> _mockRepository;
    private readonly Mock<ICodeGenerator> _mockCodeGenerator;
    private readonly Mock<IAuthenticatorConfiguration> _mockConfiguration;
    private readonly Mock<IMailService> _mockMailService;
    private readonly Mock<ISmsService> _mockSmsService;
    private readonly Mock<IOtpService> _mockOtpService;
    private readonly AuthenticatorService<Guid, Guid, Guid, Guid, Guid, Guid, Guid> _authenticator;
    private readonly CancellationToken _cancellationToken;

    private static readonly byte[] TestCodeSeed = [1, 2, 3, 4, 5, 6, 7, 8];

    public AuthenticatorTests()
    {
        _mockRepository = new Mock<IUserAuthenticatorRepository<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>();
        _mockCodeGenerator = new Mock<ICodeGenerator>();
        _mockConfiguration = new Mock<IAuthenticatorConfiguration>();
        _mockMailService = new Mock<IMailService>();
        _mockSmsService = new Mock<ISmsService>();
        _mockOtpService = new Mock<IOtpService>();
        _cancellationToken = CancellationToken.None;

        _ = _mockConfiguration
            .Setup(c => c.EnabledAuthenticatorTypes)
            .Returns([AuthenticatorType.Email, AuthenticatorType.Sms, AuthenticatorType.Otp]);
        _ = _mockConfiguration.Setup(c => c.CodeExpiration).Returns(TimeSpan.FromMinutes(5));
        _ = _mockConfiguration.Setup(c => c.CodeLength).Returns(6);
        _ = _mockConfiguration.Setup(c => c.CodeSeedLength).Returns(32);

        _authenticator = new(
            _mockRepository.Object,
            _mockCodeGenerator.Object,
            _mockConfiguration.Object,
            _mockMailService.Object,
            _mockSmsService.Object,
            _mockOtpService.Object
        );
    }

    private static Expression<Func<UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>, bool>> MatchUserId(Guid userId)
    {
        return authenticator => authenticator.UserId.Equals(userId);
    }

    private void SetupGetAsync(Guid userId, UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>? returnValue)
    {
        _ = _mockRepository.Setup(r => r.GetByIdAsync(userId, _cancellationToken)).ReturnsAsync(returnValue);
    }

    private void SetupRepositoryMocks(UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>? authenticator)
    {
        if (authenticator != null)
        {
            _ = _mockRepository.Setup(r => r.GetByIdAsync(authenticator.UserId, _cancellationToken)).ReturnsAsync(authenticator);

            _ = _mockCodeGenerator
                .Setup(g => g.GenerateNumeric(It.IsAny<int>(), It.IsAny<byte[]>()))
                .Returns(authenticator.Code ?? "123456");
        }
    }

    [Theory(DisplayName = "Should create authenticator with valid type")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "Authenticator Creation")]
    public async Task CreateAsync_ShouldCreateAuthenticator_WhenValidTypeProvided(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        string destination = "test@example.com";
        string base64Seed = Convert.ToBase64String(TestCodeSeed);
        string code = "123456";

        _ = _mockCodeGenerator.Setup(g => g.GenerateBase64(32, It.IsAny<byte[]>())).Returns(base64Seed);
        _ = _mockCodeGenerator.Setup(g => g.GenerateNumeric(6, It.IsAny<byte[]>())).Returns(code);
        _ = _mockOtpService.Setup(o => o.GenerateSecretKey(It.IsAny<byte[]>())).Returns(TestCodeSeed);

        // Act
        UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid> result = await _authenticator.CreateAsync(
            userId: userId,
            type: type,
            destination: destination,
            cancellationToken: _cancellationToken
        );

        // Assert
        _ = result.ShouldNotBeNull();
        result.UserId.ShouldBe(userId);
        result.Type.ShouldBe(type);
        result.CodeSeed.ShouldBe(TestCodeSeed);
        if (type != AuthenticatorType.Otp)
            result.Code.ShouldBe(code);

        _mockRepository.Verify(
            r => r.AddAsync(It.IsAny<UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(), _cancellationToken),
            Times.Once
        );
    }

    [Theory(DisplayName = "Should send code to valid destination")]
    [InlineData(AuthenticatorType.Email, "test@example.com")]
    [InlineData(AuthenticatorType.Sms, "+1234567890")]
    [Trait("Category", "Code Delivery")]
    public async Task AttemptAsync_ShouldSendCode_WhenValidDestinationProvided(AuthenticatorType type, string destination)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeSeed = TestCodeSeed,
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
        };

        SetupRepositoryMocks(authenticator);

        _ = _mockConfiguration
            .Setup(c => c.GetEmailTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailTemplateConfiguration("Test Subject", "Test {0}", "Test {0}"));

        _ = _mockConfiguration
            .Setup(c => c.GetSmsTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsTemplateConfiguration("Code: {0}"));

        // Act
        await _authenticator.AttemptAsync(userId: userId, destination: destination, cancellationToken: _cancellationToken);

        // Assert
        if (type == AuthenticatorType.Email)
            _mockMailService.Verify(
                m =>
                    m.SendAsync(
                        It.Is<Mail>(mail => mail.ToList[0].Address == destination && mail.Priority == 1),
                        _cancellationToken
                    ),
                Times.Once
            );
        else if (type == AuthenticatorType.Sms)
            _mockSmsService.Verify(
                s =>
                    s.SendAsync(
                        It.Is<Sms.Abstractions.Sms>(sms =>
                            sms.PhoneNumber == destination
                            && sms.Priority == 1
                            && sms.CustomParameters != null
                            && sms.CustomParameters.ContainsKey("type")
                            && sms.CustomParameters["type"] == "authentication"
                        ),
                        _cancellationToken
                    ),
                Times.Once
            );
    }

    [Fact(DisplayName = "Should reject expired code")]
    [Trait("Category", "Code Validation")]
    public async Task VerifyAsync_ShouldThrowException_WhenCodeExpired()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, AuthenticatorType.Email)
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(-5),
        };

        SetupRepositoryMocks(authenticator);

        _ = _mockConfiguration
            .Setup(c => c.GetCodeExpiredMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Code expired");

        // Act & Assert
        _ = await Should.ThrowAsync<BusinessException>(
            async () => await _authenticator.VerifyAsync(userId, "123456", _cancellationToken)
        );
    }

    [Theory(DisplayName = "Should verify valid code")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "Code Validation")]
    public async Task VerifyAsync_ShouldVerifyAuthenticator_WhenValidCodeProvided(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        string code = "123456";
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = type != AuthenticatorType.Otp ? code : null,
            CodeSeed = TestCodeSeed,
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsVerified = false,
            UserId = userId,
            Id = userId,
        };

        // Setup repository mock
        _ = _mockRepository.Setup(r => r.GetByIdAsync(userId, _cancellationToken)).ReturnsAsync(authenticator);

        // Setup OTP service mock specifically for OTP type
        if (type == AuthenticatorType.Otp)
            _ = _mockOtpService.Setup(o => o.ComputeOtp(TestCodeSeed, It.IsAny<DateTime?>())).Returns(code);

        // Setup repository update mock
        _ = _mockRepository.Setup(r => r.UpdateAsync(authenticator, _cancellationToken)).ReturnsAsync(authenticator);

        // Act
        await _authenticator.VerifyAsync(userId, code, _cancellationToken);

        // Assert
        authenticator.IsVerified.ShouldBeTrue();
        _mockRepository.Verify(
            r =>
                r.UpdateAsync(
                    It.Is<UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(a => a.IsVerified && a.Id.Equals(userId)),
                    _cancellationToken
                ),
            Times.Once
        );
    }

    [Theory(DisplayName = "Should reject invalid code")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "Code Validation")]
    public async Task VerifyAsync_ShouldThrowException_WhenInvalidCodeProvided(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeSeed = TestCodeSeed,
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
        };

        SetupGetAsync(userId, authenticator);
        _ = _mockConfiguration
            .Setup(c => c.GetInvalidCodeMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Invalid code");

        if (type == AuthenticatorType.Otp)
            _ = _mockOtpService.Setup(o => o.ComputeOtp(It.IsAny<byte[]>(), null)).Returns("654321");

        // Act & Assert
        _ = await Should.ThrowAsync<BusinessException>(
            async () => await _authenticator.VerifyAsync(userId, "wrong-code", _cancellationToken)
        );
    }

    [Fact(DisplayName = "Should delete existing authenticator")]
    [Trait("Category", "Authenticator Management")]
    public async Task DeleteAsync_ShouldDeleteAuthenticator_WhenExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, AuthenticatorType.Email);

        SetupRepositoryMocks(authenticator);

        // Act
        await _authenticator.DeleteAsync(userId: userId, cancellationToken: _cancellationToken);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(authenticator, _cancellationToken), Times.Once);
    }

    [Fact(DisplayName = "Should handle missing authenticator delete")]
    [Trait("Category", "Authenticator Management")]
    public async Task DeleteAsync_ShouldNotThrow_WhenAuthenticatorNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        SetupRepositoryMocks(null);

        // Act & Assert
        await Should.NotThrowAsync(
            async () => await _authenticator.DeleteAsync(userId: userId, cancellationToken: _cancellationToken)
        );
    }

    [Fact(DisplayName = "Should verify successful deletion")]
    [Trait("Category", "Authenticator Management")]
    public async Task DeleteAsync_ShouldVerifyDeletion_WhenAuthenticatorExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, AuthenticatorType.Email);

        _ = _mockRepository.Setup(r => r.GetByIdAsync(userId, _cancellationToken)).ReturnsAsync(authenticator);

        _ = _mockRepository.Setup(r => r.DeleteAsync(authenticator, _cancellationToken)).ReturnsAsync(authenticator);

        // Act
        await _authenticator.DeleteAsync(userId, _cancellationToken);

        // Assert
        _mockRepository.Verify(
            r =>
                r.DeleteAsync(
                    It.Is<UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(a => a == authenticator),
                    _cancellationToken
                ),
            Times.Once
        );
    }

    [Theory(DisplayName = "Should require mail service for email auth")]
    [InlineData(AuthenticatorType.Email)]
    [Trait("Category", "Service Validation")]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenServiceNotConfigured(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new AuthenticatorService<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(
            _mockRepository.Object,
            _mockCodeGenerator.Object,
            _mockConfiguration.Object,
            mailService: null,
            smsService: _mockSmsService.Object,
            otpAuthenticator: _mockOtpService.Object
        );

        // Act & Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await authenticator.CreateAsync(userId, type, "test@example.com", _cancellationToken)
        );
        exception.Message.ShouldContain("Email authentication is enabled but no implementation of IMailService");
    }

    [Theory(DisplayName = "Should require SMS service for SMS auth")]
    [InlineData(AuthenticatorType.Sms)]
    [Trait("Category", "Service Validation")]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenSmsServiceNotConfigured(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new AuthenticatorService<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(
            _mockRepository.Object,
            _mockCodeGenerator.Object,
            _mockConfiguration.Object,
            mailService: _mockMailService.Object,
            smsService: null,
            otpAuthenticator: _mockOtpService.Object
        );

        // Act & Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await authenticator.CreateAsync(userId, type, "+1234567890", _cancellationToken)
        );
        exception.Message.ShouldContain("SMS authentication is enabled but no implementation of ISmsService");
    }

    [Theory(DisplayName = "Should require OTP service for OTP auth")]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "Service Validation")]
    public async Task CreateAsync_ShouldThrowInvalidOperationException_WhenOtpServiceNotConfigured(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new AuthenticatorService<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(
            _mockRepository.Object,
            _mockCodeGenerator.Object,
            _mockConfiguration.Object,
            mailService: _mockMailService.Object,
            smsService: _mockSmsService.Object,
            otpAuthenticator: null
        );

        // Act & Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await authenticator.CreateAsync(userId, type, null, _cancellationToken)
        );
        exception.Message.ShouldContain("OTP authentication is enabled but no implementation of IOtpService");
    }

    [Theory(DisplayName = "Should throw for unsupported authenticator type")]
    [InlineData((AuthenticatorType)99)] // Invalid enum value
    [Trait("Category", "Configuration Validation")]
    public async Task CreateAsync_ShouldThrowNotSupportedException_WhenUnsupportedType(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Enable the unsupported type in configuration to bypass initial validation
        _ = _mockConfiguration.Setup(c => c.EnabledAuthenticatorTypes).Returns([type]);

        _ = _mockConfiguration
            .Setup(c => c.GetUnsupportedTypeMessageAsync(type, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Unsupported type");

        // Set up code generator to allow reaching the unsupported type check
        _ = _mockCodeGenerator
            .Setup(g => g.GenerateBase64(It.IsAny<int>(), It.IsAny<byte[]>()))
            .Returns(Convert.ToBase64String(TestCodeSeed));

        // Act & Assert
        _ = await Should.ThrowAsync<NotSupportedException>(
            async () => await _authenticator.CreateAsync(userId, type, null, _cancellationToken)
        );
    }

    [Theory(DisplayName = "Should reject null destination for email auth")]
    [InlineData(AuthenticatorType.Email)]
    [Trait("Category", "Input Validation")]
    public async Task AttemptAsync_ShouldThrowException_WhenDestinationIsNullForEmail(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
        };

        SetupRepositoryMocks(authenticator);
        _ = _mockConfiguration
            .Setup(c => c.GetDestinationRequiredMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Destination is required");

        // Act & Assert
        _ = await Should.ThrowAsync<BusinessException>(
            async () => await _authenticator.AttemptAsync(userId, null, _cancellationToken)
        );
    }

    [Fact(DisplayName = "Should reject empty string destination")]
    [Trait("Category", "Input Validation")]
    public async Task AttemptAsync_ShouldThrowException_WhenDestinationIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, AuthenticatorType.Email)
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
        };

        SetupRepositoryMocks(authenticator);
        _ = _mockConfiguration
            .Setup(c => c.GetDestinationRequiredMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Destination is required");

        // Act & Assert
        _ = await Should.ThrowAsync<BusinessException>(
            async () => await _authenticator.AttemptAsync(userId, string.Empty, _cancellationToken)
        );
    }

    [Theory(DisplayName = "Should handle all authenticator types without destination for OTP")]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "OTP Validation")]
    public async Task AttemptAsync_ShouldSucceed_WhenNoDestinationForOtp(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            CodeSeed = TestCodeSeed,
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
        };

        SetupRepositoryMocks(authenticator);

        // Act & Assert
        await Should.NotThrowAsync(async () => await _authenticator.AttemptAsync(userId, null, _cancellationToken));
    }

    [Theory(DisplayName = "Should handle authenticator not found")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "Error Handling")]
    public async Task AttemptAsync_ShouldThrowException_WhenAuthenticatorNotFound(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        string? destination = type switch
        {
            AuthenticatorType.Email => "test@example.com",
            AuthenticatorType.Sms => "+1234567890",
            AuthenticatorType.Otp => null,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };

        SetupRepositoryMocks(null);

        _ = _mockConfiguration
            .Setup(c => c.GetAuthenticatorNotFoundMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Authenticator not found");

        // Act & Assert
        _ = await Should.ThrowAsync<BusinessException>(
            async () => await _authenticator.AttemptAsync(userId, destination, _cancellationToken)
        );
    }

    [Theory(DisplayName = "Should handle email template retrieval errors")]
    [InlineData(AuthenticatorType.Email)]
    [Trait("Category", "Error Handling")]
    public async Task AttemptAsync_ShouldHandleTemplateRetrievalErrors(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
        };

        SetupRepositoryMocks(authenticator);

        _ = _mockConfiguration
            .Setup(c => c.GetEmailTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Template not found"));

        // Act & Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _authenticator.AttemptAsync(userId, "test@example.com", _cancellationToken)
        );
    }

    [Theory(DisplayName = "Should handle service errors gracefully")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [Trait("Category", "Error Handling")]
    public async Task AttemptAsync_ShouldHandleServiceErrors(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
        };

        SetupRepositoryMocks(authenticator);

        if (type == AuthenticatorType.Email)
        {
            _ = _mockConfiguration
                .Setup(c => c.GetEmailTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EmailTemplateConfiguration("Test", "Test {0}", "Test {0}"));
            _ = _mockMailService
                .Setup(m => m.SendAsync(It.IsAny<Mail>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Mail service error"));
        }
        else
        {
            _ = _mockConfiguration
                .Setup(c => c.GetSmsTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SmsTemplateConfiguration("Test {0}"));
            _ = _mockSmsService
                .Setup(s => s.SendAsync(It.IsAny<Sms.Abstractions.Sms>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("SMS service error"));
        }

        // Act & Assert
        _ = await Should.ThrowAsync<Exception>(
            async () => await _authenticator.AttemptAsync(userId, "test@example.com", _cancellationToken)
        );
    }

    [Theory(DisplayName = "Should reject expired code attempt")]
    [InlineData(AuthenticatorType.Email, "test@example.com")]
    [InlineData(AuthenticatorType.Sms, "+1234567890")]
    [InlineData(AuthenticatorType.Otp, null)]
    [Trait("Category", "Code Validation")]
    public async Task AttemptAsync_ShouldThrowException_WhenCodeIsExpired(AuthenticatorType type, string? destination)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeSeed = TestCodeSeed,
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Expired code
            Id = userId,
        };

        SetupRepositoryMocks(authenticator);
        _ = _mockConfiguration
            .Setup(c => c.GetCodeExpiredMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Code expired");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(
            async () => await _authenticator.AttemptAsync(userId, destination, _cancellationToken)
        );
        exception.Message.ShouldBe("Code expired");
    }

    [Theory(DisplayName = "Should handle SMS template retrieval errors")]
    [InlineData(AuthenticatorType.Sms)]
    [Trait("Category", "Error Handling")]
    public async Task AttemptAsync_ShouldHandleSmsTemplateRetrievalErrors(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
        };

        SetupRepositoryMocks(authenticator);

        _ = _mockConfiguration
            .Setup(c => c.GetSmsTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMS template not found"));

        // Act & Assert
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _authenticator.AttemptAsync(userId, "+1234567890", _cancellationToken)
        );
    }

    [Theory(DisplayName = "Should verify authenticator with null expiration")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "Code Validation")]
    public async Task VerifyAsync_ShouldSucceed_WhenCodeExpirationIsNull(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        string code = "123456";
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = type != AuthenticatorType.Otp ? code : null,
            CodeSeed = TestCodeSeed,
            CodeExpiresAt = null, // Null expiration
            IsVerified = false,
            UserId = userId,
            Id = userId,
        };

        _ = _mockRepository.Setup(r => r.GetByIdAsync(userId, _cancellationToken)).ReturnsAsync(authenticator);

        if (type == AuthenticatorType.Otp)
            _ = _mockOtpService.Setup(o => o.ComputeOtp(TestCodeSeed, It.IsAny<DateTime?>())).Returns(code);

        _ = _mockRepository.Setup(r => r.UpdateAsync(authenticator, _cancellationToken)).ReturnsAsync(authenticator);

        // Act
        await _authenticator.VerifyAsync(userId, code, _cancellationToken);

        // Assert
        authenticator.IsVerified.ShouldBeTrue();
    }

    [Theory(DisplayName = "Should verify already verified authenticator")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "Code Validation")]
    public async Task VerifyAsync_ShouldNotUpdateRepository_WhenAlreadyVerified(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        string code = "123456";
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = type != AuthenticatorType.Otp ? code : null,
            CodeSeed = TestCodeSeed,
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsVerified = true, // Already verified
            UserId = userId,
            Id = userId,
        };

        _ = _mockRepository.Setup(r => r.GetByIdAsync(userId, _cancellationToken)).ReturnsAsync(authenticator);

        if (type == AuthenticatorType.Otp)
            _ = _mockOtpService.Setup(o => o.ComputeOtp(TestCodeSeed, It.IsAny<DateTime?>())).Returns(code);

        // Act
        await _authenticator.VerifyAsync(userId, code, _cancellationToken);

        // Assert
        _mockRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(), _cancellationToken),
            Times.Never
        );
    }

    [Theory(DisplayName = "Should handle unsupported type in verification")]
    [InlineData((AuthenticatorType)99)]
    [Trait("Category", "Error Handling")]
    public async Task VerifyAsync_ShouldThrowNotSupportedException_WhenUnsupportedType(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeSeed = TestCodeSeed,
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
            IsVerified = false,
        };

        // Add unsupported type to enabled types to bypass initial validation
        _ = _mockConfiguration.Setup(c => c.EnabledAuthenticatorTypes).Returns([type]);

        _ = _mockRepository.Setup(r => r.GetByIdAsync(userId, _cancellationToken)).ReturnsAsync(authenticator);

        // Act & Assert
        _ = await Should.ThrowAsync<NotSupportedException>(
            async () => await _authenticator.VerifyAsync(userId, "123456", _cancellationToken)
        );
    }

    [Theory(DisplayName = "Should throw for expired code during attempt")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [Trait("Category", "Code Validation")]
    public async Task AttemptAsync_ShouldThrowException_WhenCodeExpired(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Expired
            CodeSeed = TestCodeSeed,
            Id = userId,
        };

        SetupRepositoryMocks(authenticator);
        _ = _mockConfiguration
            .Setup(c => c.GetCodeExpiredMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Code has expired");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(
            async () => await _authenticator.AttemptAsync(userId, "test@example.com", _cancellationToken)
        );
        exception.Message.ShouldBe("Code has expired");
    }

    [Theory(DisplayName = "Should set correct SMS parameters")]
    [InlineData(null)]
    [InlineData("2024-01-01T00:00:00Z")]
    [Trait("Category", "SMS Configuration")]
    public async Task AttemptAsync_ShouldSetCorrectSmsParameters(string? expirationDate)
    {
        // Arrange
        var userId = Guid.NewGuid();
        string phoneNumber = "+1234567890";
        string code = "123456";

        // If expirationDate is null or in the past, assign a future time.
        DateTime parsedDate = expirationDate != null ? DateTime.Parse(expirationDate) : DateTime.MinValue;
        DateTime expiresAt = expirationDate == null || parsedDate < DateTime.UtcNow ? DateTime.UtcNow.AddMinutes(10) : parsedDate;

        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, AuthenticatorType.Sms)
        {
            Code = code,
            CodeSeed = TestCodeSeed,
            CodeExpiresAt = expiresAt,
            Id = userId,
            UserId = userId,
        };

        // Setup repository returning our authenticator
        _ = _mockRepository.Setup(r => r.GetByIdAsync(userId, _cancellationToken)).ReturnsAsync(authenticator);

        // Enable SMS authentication type
        _ = _mockConfiguration.Setup(c => c.EnabledAuthenticatorTypes).Returns([AuthenticatorType.Sms]);

        // Setup SMS template
        _ = _mockConfiguration
            .Setup(c => c.GetSmsTemplateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsTemplateConfiguration("Code: {0}"));

        // Setup code generator expecting the known seed
        _ = _mockCodeGenerator
            .Setup(g => g.GenerateNumeric(It.IsAny<int>(), It.Is<byte[]>(s => s.SequenceEqual(TestCodeSeed))))
            .Returns(code);

        // Setup SMS service to capture the sent SMS and return CompletedTask
        Sms.Abstractions.Sms? capturedSms = null;
        _ = _mockSmsService
            .Setup(s => s.SendAsync(It.IsAny<Sms.Abstractions.Sms>(), It.IsAny<CancellationToken>()))
            .Callback<Sms.Abstractions.Sms, CancellationToken>((sms, _) => capturedSms = sms)
            .Returns(Task.CompletedTask);

        // Act
        await _authenticator.AttemptAsync(userId, phoneNumber, _cancellationToken);

        // Assert
        _ = capturedSms.ShouldNotBeNull();
        capturedSms!.Value.PhoneNumber.ShouldBe(phoneNumber);
        capturedSms!.Value.Priority.ShouldBe(1);
        capturedSms!.Value.CustomParameters!.ShouldContainKey("type");
        capturedSms!.Value.CustomParameters!["type"].ShouldBe("authentication");
        capturedSms!.Value.CustomParameters.ShouldContainKey("expiresAt");
        capturedSms!.Value.CustomParameters["expiresAt"].ShouldBe(expiresAt.ToString("O"));

        // Verify repository update was called once
        _mockRepository.Verify(
            r =>
                r.UpdateAsync(
                    It.Is<UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(a =>
                        a.Id.Equals(userId) && a.Code == code && a.Type == AuthenticatorType.Sms
                    ),
                    _cancellationToken
                ),
            Times.Once
        );
    }

    [Fact(DisplayName = "Should throw for unsupported type during attempt")]
    [Trait("Category", "Error Handling")]
    public async Task AttemptAsync_ShouldThrowException_WhenUnsupportedType()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var unsupportedType = (AuthenticatorType)byte.MaxValue;
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, unsupportedType)
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
        };

        SetupRepositoryMocks(authenticator);
        // Include unsupportedType in EnabledAuthenticatorTypes to bypass the "not enabled" check.
        _ = _mockConfiguration.Setup(c => c.EnabledAuthenticatorTypes).Returns([unsupportedType]);

        _ = _mockConfiguration
            .Setup(c => c.GetUnsupportedTypeMessageAsync(unsupportedType, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Unsupported authenticator type");

        // Act & Assert
        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _authenticator.AttemptAsync(userId, "test@example.com", _cancellationToken)
        );
        exception.Message.ShouldBe("Unsupported authenticator type");
    }

    [Theory(DisplayName = "Should throw when authenticator type not enabled")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "Configuration Validation")]
    public async Task AttemptAsync_ShouldThrowException_WhenAuthenticatorTypeNotEnabled(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
            UserId = userId,
        };

        // Setup repository mock
        _ = _mockRepository.Setup(r => r.GetByIdAsync(userId, _cancellationToken)).ReturnsAsync(authenticator);

        // Clear enabled types
        _ = _mockConfiguration.Setup(c => c.EnabledAuthenticatorTypes).Returns([]); // Empty set means no types are enabled

        // Setup error message
        _ = _mockConfiguration
            .Setup(c => c.GetAuthenticatorTypeNotEnabledMessageAsync(type, It.IsAny<CancellationToken>()))
            .ReturnsAsync($"Authenticator type {type} is not enabled");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(
            async () => await _authenticator.AttemptAsync(userId, "test@example.com", _cancellationToken)
        );

        exception.Message.ShouldBe($"Authenticator type {type} is not enabled");

        // Verify that GetAuthenticatorTypeNotEnabledMessageAsync was called
        _mockConfiguration.Verify(
            c => c.GetAuthenticatorTypeNotEnabledMessageAsync(type, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    // Rename this method to be more specific about the validation step
    [Theory(DisplayName = "Should validate enabled types during attempt")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "Configuration Validation")]
    public async Task AttemptAsync_ShouldValidateEnabledAuthenticatorTypes(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
            UserId = userId,
        };

        // Setup repository mock
        _ = _mockRepository.Setup(r => r.GetByIdAsync(userId, _cancellationToken)).ReturnsAsync(authenticator);

        // Clear enabled types
        _ = _mockConfiguration.Setup(c => c.EnabledAuthenticatorTypes).Returns([]); // Empty set means no types are enabled

        // Setup error message
        _ = _mockConfiguration
            .Setup(c => c.GetAuthenticatorTypeNotEnabledMessageAsync(type, It.IsAny<CancellationToken>()))
            .ReturnsAsync($"Authenticator type {type} is not enabled");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(
            async () => await _authenticator.AttemptAsync(userId, "test@example.com", _cancellationToken)
        );

        exception.Message.ShouldBe($"Authenticator type {type} is not enabled");

        // Verify that GetAuthenticatorTypeNotEnabledMessageAsync was called
        _mockConfiguration.Verify(
            c => c.GetAuthenticatorTypeNotEnabledMessageAsync(type, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Theory(DisplayName = "Should throw for invalid code")]
    [InlineData(AuthenticatorType.Email)]
    [InlineData(AuthenticatorType.Sms)]
    [InlineData(AuthenticatorType.Otp)]
    [Trait("Category", "Code Validation")]
    public async Task VerifyAsync_ShouldThrowException_WhenInvalidCode(AuthenticatorType type)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authenticator = new UserAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(userId, type)
        {
            Code = "123456",
            CodeSeed = TestCodeSeed,
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            Id = userId,
        };

        SetupRepositoryMocks(authenticator);

        if (type == AuthenticatorType.Otp)
            _ = _mockOtpService.Setup(o => o.ComputeOtp(It.IsAny<byte[]>(), It.IsAny<DateTime?>())).Returns("654321"); // Different from input code

        _ = _mockConfiguration
            .Setup(c => c.GetInvalidCodeMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Invalid verification code");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(
            async () => await _authenticator.VerifyAsync(userId, "wrong-code", _cancellationToken)
        );
        exception.Message.ShouldBe("Invalid verification code");
    }
}
