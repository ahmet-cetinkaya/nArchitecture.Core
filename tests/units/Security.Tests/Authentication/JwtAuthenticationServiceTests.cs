using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Moq;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Security.Abstractions.Authentication;
using NArchitecture.Core.Security.Abstractions.Authentication.Entities;
using NArchitecture.Core.Security.Abstractions.Authentication.Models;
using NArchitecture.Core.Security.Abstractions.Authorization;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;
using NArchitecture.Core.Security.Authentication;
using Shouldly;

namespace NArchitecture.Core.Security.Tests.Authentication;

public class JwtAuthenticationServiceTests
{
    private readonly Mock<IRefreshTokenRepository<Guid, Guid, Guid, Guid, Guid, Guid, Guid, Guid>> _mockRefreshTokenRepository;
    private readonly Mock<IUserRepository<Guid, Guid, Guid, Guid, Guid, Guid, Guid>> _mockUserRepository;
    private readonly Mock<IAuthorizationService<Guid, Guid>> _mockAuthorizationService;
    private readonly Mock<IJwtAuthenticationConfiguration> _mockConfiguration;
    private readonly JwtAuthenticationService<Guid, Guid, Guid, Guid, Guid, Guid, Guid> _sut;

    public JwtAuthenticationServiceTests()
    {
        _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository<Guid, Guid, Guid, Guid, Guid, Guid, Guid, Guid>>();
        _mockUserRepository = new Mock<IUserRepository<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>();
        _mockAuthorizationService = new Mock<IAuthorizationService<Guid, Guid>>();
        _mockConfiguration = new Mock<IJwtAuthenticationConfiguration>();

        SetupDefaultConfiguration();

        _sut = new JwtAuthenticationService<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(
            _mockRefreshTokenRepository.Object,
            _mockUserRepository.Object,
            _mockAuthorizationService.Object,
            _mockConfiguration.Object
        );
    }

    private void SetupDefaultConfiguration()
    {
        byte[] keyBytes = new byte[64]; // 64 bytes = 512 bits
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            rng.GetBytes(keyBytes);
        string securityKey = Convert.ToBase64String(keyBytes);

        _ = _mockConfiguration.Setup(c => c.SecurityKey).Returns(securityKey);
        _ = _mockConfiguration.Setup(c => c.AccessTokenExpiration).Returns(TimeSpan.FromMinutes(10));
        _ = _mockConfiguration.Setup(c => c.RefreshTokenTTL).Returns(TimeSpan.FromDays(7));
        _ = _mockConfiguration.Setup(c => c.Issuer).Returns("test-issuer");
        _ = _mockConfiguration.Setup(c => c.Audience).Returns("test-audience");
    }

    [Fact(DisplayName = "LoginAsync should create access and refresh tokens for valid credentials")]
    [Trait("Category", "Authentication")]
    public async Task LoginAsync_WithValidCredentials_ShouldCreateTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        User<Guid, Guid, Guid, Guid, Guid, Guid, Guid> user = CreateTestUser(userId);
        var claimId = Guid.NewGuid();
        var operationClaims = new List<OperationClaim<Guid>> { new("TestClaim") { Id = claimId } };
        var loginRequest = new LoginRequest<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(user, "correct-password", "127.0.0.1");

        _ = _mockAuthorizationService
            .Setup(x => x.GetUserOperationClaimsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationClaims);

        // Act
        AuthenticationResponse result = await _sut.LoginAsync(loginRequest);

        // Assert
        result.ShouldBe(result);
        result.AccessToken.ShouldBe(result.AccessToken);
        result.RefreshToken.ShouldBe(result.RefreshToken);

        var jwtHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwtToken = jwtHandler.ReadJwtToken(result.AccessToken.Content);

        Claim? userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        userIdClaim.ShouldNotBe(null);
        userIdClaim!.Value.ShouldBe(userId.ToString());

        Claim? operationClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        operationClaim.ShouldNotBe(null);
        operationClaim!.Value.ShouldBe("TestClaim");
    }

    [Fact(DisplayName = "LoginAsync should throw for invalid password")]
    [Trait("Category", "Authentication")]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowException()
    {
        // Arrange
        User<Guid, Guid, Guid, Guid, Guid, Guid, Guid> user = CreateTestUser(Guid.NewGuid(), "different-password");
        var loginRequest = new LoginRequest<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(user, "wrong-password", "127.0.0.1");

        _ = _mockConfiguration
            .Setup(x => x.GetInvalidPasswordMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Invalid password");

        // Act & Assert
        _ = await Should.ThrowAsync<BusinessException>(async () => await _sut.LoginAsync(loginRequest));
    }

    [Fact(DisplayName = "RefreshTokenAsync should create new tokens for valid refresh token")]
    [Trait("Category", "Authentication")]
    public async Task RefreshTokenAsync_WithValidToken_ShouldCreateNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        User<Guid, Guid, Guid, Guid, Guid, Guid, Guid> user = CreateTestUser(userId);
        RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid> refreshToken = CreateTestRefreshToken(userId);
        var claimId = Guid.NewGuid();
        var operationClaims = new List<OperationClaim<Guid>>
        {
            new("TestClaim") { Id = claimId }, // Using constructor instead of object initializer
        };

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(refreshToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        _ = _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        _ = _mockAuthorizationService
            .Setup(x => x.GetUserOperationClaimsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationClaims);

        // Act
        RefreshTokenResponse result = await _sut.RefreshTokenAsync(refreshToken.Token, "127.0.0.1");

        // Assert
        result.ShouldBe(result); // Assert not null by comparing to itself
        result.AccessToken.ShouldBe(result.AccessToken); // Assert not null by comparing to itself
        result.RefreshToken.ShouldBe(result.RefreshToken); // Assert not null by comparing to itself
        result.RefreshToken.Content.ShouldNotBe(refreshToken.Token);
    }

    [Fact(DisplayName = "RefreshTokenAsync should throw when refresh token is expired")]
    [Trait("Category", "Authentication")]
    public async Task RefreshTokenAsync_WithExpiredToken_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiredToken = new RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(
            userId,
            Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            DateTime.UtcNow.AddDays(-1), // Expired
            "127.0.0.1"
        );

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(expiredToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredToken);

        _ = _mockConfiguration
            .Setup(x => x.GetTokenExpiredMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Token has expired");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(async () =>
            await _sut.RefreshTokenAsync(expiredToken.Token, "127.0.0.1")
        );
        exception.Message.ShouldBe("Token has expired");
    }

    [Fact(DisplayName = "RefreshTokenAsync should throw when refresh token is revoked")]
    [Trait("Category", "Authentication")]
    public async Task RefreshTokenAsync_WithRevokedToken_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var revokedToken = new RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(
            userId,
            Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            DateTime.UtcNow.AddDays(7),
            "127.0.0.1"
        )
        {
            RevokedAt = DateTime.UtcNow.AddMinutes(-5),
            RevokedByIp = "127.0.0.1",
            ReasonRevoked = "Test revocation",
        };

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(revokedToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(revokedToken);

        _ = _mockConfiguration
            .Setup(x => x.GetTokenRevokedMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Token has been revoked");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(async () =>
            await _sut.RefreshTokenAsync(revokedToken.Token, "127.0.0.1")
        );
        exception.Message.ShouldBe("Token has been revoked");
    }

    [Fact(DisplayName = "RefreshTokenAsync should throw when user is not found")]
    [Trait("Category", "Authentication")]
    public async Task RefreshTokenAsync_WithNonExistentUser_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid> refreshToken = CreateTestRefreshToken(userId);

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(refreshToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        _ = _mockUserRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User<Guid, Guid, Guid, Guid, Guid, Guid, Guid>?)null);

        _ = _mockConfiguration
            .Setup(x => x.GetUserNotFoundMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("User not found");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(async () =>
            await _sut.RefreshTokenAsync(refreshToken.Token, "127.0.0.1")
        );
        exception.Message.ShouldBe("User not found");
    }

    [Fact(DisplayName = "RevokeRefreshTokenAsync should revoke valid token")]
    [Trait("Category", "Authentication")]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldRevokeToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid> refreshToken = CreateTestRefreshToken(userId);
        string ipAddress = "127.0.0.1";
        string reason = "Test revocation";

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(refreshToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        // Act
        await _sut.RevokeRefreshTokenAsync(refreshToken.Token, ipAddress, reason);

        // Assert
        _mockRefreshTokenRepository.Verify(
            x =>
                x.UpdateAsync(
                    It.Is<RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(t =>
                        t.Token == refreshToken.Token
                        && t.RevokedAt.HasValue
                        && t.RevokedByIp == ipAddress
                        && t.ReasonRevoked == reason
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact(DisplayName = "RevokeAllRefreshTokensAsync should revoke all active tokens for user")]
    [Trait("Category", "Authentication")]
    public async Task RevokeAllRefreshTokensAsync_WithActiveTokens_ShouldRevokeAllTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activeTokens = new List<RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>
        {
            CreateTestRefreshToken(userId),
            CreateTestRefreshToken(userId),
            CreateTestRefreshToken(userId),
        };
        string reason = "Bulk revocation test";

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetAllActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeTokens);

        // Act
        await _sut.RevokeAllRefreshTokensAsync(userId, reason);

        // Assert
        _mockRefreshTokenRepository.Verify(
            x =>
                x.UpdateAsync(
                    It.Is<RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(t =>
                        t.RevokedAt.HasValue && t.ReasonRevoked == reason
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Exactly(activeTokens.Count)
        );
    }

    [Fact(DisplayName = "RevokeRefreshTokenAsync should throw when token is already revoked")]
    [Trait("Category", "Authentication")]
    public async Task RevokeRefreshTokenAsync_WithAlreadyRevokedToken_ShouldThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var alreadyRevokedToken = new RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(
            userId,
            Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            DateTime.UtcNow.AddDays(7),
            "127.0.0.1"
        )
        {
            RevokedAt = DateTime.UtcNow.AddMinutes(-30),
            RevokedByIp = "127.0.0.1",
            ReasonRevoked = "Previously revoked",
        };

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(alreadyRevokedToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyRevokedToken);

        _ = _mockConfiguration
            .Setup(x => x.GetTokenAlreadyRevokedMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("Token is already revoked");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(async () =>
            await _sut.RevokeRefreshTokenAsync(alreadyRevokedToken.Token, "127.0.0.1", "New revocation attempt")
        );
        exception.Message.ShouldBe("Token is already revoked");

        // Verify that UpdateAsync was not called
        _mockRefreshTokenRepository.Verify(
            x => x.UpdateAsync(It.IsAny<RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Theory(DisplayName = "LoginAsync should handle different password scenarios")]
    [Trait("Category", "Authentication")]
    [InlineData("", "Empty password")]
    [InlineData("   ", "Whitespace password")]
    [InlineData("short", "Short password")]
    public async Task LoginAsync_WithVariousPasswords_ShouldHandleAppropriately(string password, string scenario)
    {
        // Arrange
        User<Guid, Guid, Guid, Guid, Guid, Guid, Guid> user = CreateTestUser(Guid.NewGuid());
        var loginRequest = new LoginRequest<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(user, password, "127.0.0.1");

        _ = _mockConfiguration
            .Setup(x => x.GetInvalidPasswordMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync($"Invalid password: {scenario}");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(async () => await _sut.LoginAsync(loginRequest));
        exception.Message.ShouldStartWith("Invalid password");
    }

    [Fact(DisplayName = "AccessToken should contain correct JWT properties")]
    [Trait("Category", "Authentication")]
    [Trait("Category", "JWT Validation")]
    public async Task LoginAsync_ShouldCreateValidJwtToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        User<Guid, Guid, Guid, Guid, Guid, Guid, Guid> user = CreateTestUser(userId);
        var loginRequest = new LoginRequest<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(user, "correct-password", "127.0.0.1");
        var operationClaims = new List<OperationClaim<Guid>> { new("TestClaim") { Id = Guid.NewGuid() } };

        DateTime testStartTime = DateTime.UtcNow.AddSeconds(-1); // Add buffer for test execution
        DateTime expectedExpiration = testStartTime.Add(_mockConfiguration.Object.AccessTokenExpiration);

        _ = _mockAuthorizationService
            .Setup(x => x.GetUserOperationClaimsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationClaims);

        // Act
        AuthenticationResponse result = await _sut.LoginAsync(loginRequest);

        // Assert
        var jwtHandler = new JwtSecurityTokenHandler();
        JwtSecurityToken jwtToken = jwtHandler.ReadJwtToken(result.AccessToken.Content);

        // Basic JWT properties
        jwtToken.Issuer.ShouldBe("test-issuer");
        jwtToken.Audiences.ShouldContain("test-audience");

        // Truncate milliseconds for comparison since JWT uses second precision
        var truncatedTestStartTime = new DateTime(
            testStartTime.Year,
            testStartTime.Month,
            testStartTime.Day,
            testStartTime.Hour,
            testStartTime.Minute,
            testStartTime.Second,
            DateTimeKind.Utc
        );
        var truncatedNow = new DateTime(
            DateTime.UtcNow.Year,
            DateTime.UtcNow.Month,
            DateTime.UtcNow.Day,
            DateTime.UtcNow.Hour,
            DateTime.UtcNow.Minute,
            DateTime.UtcNow.Second,
            DateTimeKind.Utc
        );

        // Time-based assertions with second precision
        jwtToken.ValidFrom.ShouldBeGreaterThanOrEqualTo(truncatedTestStartTime);
        jwtToken.ValidFrom.ShouldBeLessThanOrEqualTo(truncatedNow);
        jwtToken.ValidTo.ShouldBe(
            expectedExpiration.AddTicks(-(expectedExpiration.Ticks % TimeSpan.TicksPerSecond)),
            TimeSpan.FromSeconds(1)
        );

        // Claims validation
        var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);
        claims.ShouldContainKey(ClaimTypes.NameIdentifier);
        claims[ClaimTypes.NameIdentifier].ShouldBe(userId.ToString());
        claims.ShouldContainKey(ClaimTypes.Role);
        claims[ClaimTypes.Role].ShouldBe("TestClaim");

        // Token content validation
        result.AccessToken.Content.ShouldNotBeNullOrWhiteSpace();
        jwtHandler.CanReadToken(result.AccessToken.Content).ShouldBeTrue();
    }

    [Fact(DisplayName = "RefreshToken should be revoked with correct replacement token")]
    [Trait("Category", "Authentication")]
    [Trait("Category", "Token Replacement")]
    public async Task RefreshTokenAsync_ShouldRevokeOldTokenWithCorrectReplacement()
    {
        // Arrange
        var userId = Guid.NewGuid();
        User<Guid, Guid, Guid, Guid, Guid, Guid, Guid> user = CreateTestUser(userId);
        RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid> oldToken = CreateTestRefreshToken(userId);
        var operationClaims = new List<OperationClaim<Guid>> { new("TestClaim") { Id = Guid.NewGuid() } };

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(oldToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldToken);
        _ = _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _ = _mockAuthorizationService
            .Setup(x => x.GetUserOperationClaimsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationClaims);

        // Act
        RefreshTokenResponse result = await _sut.RefreshTokenAsync(oldToken.Token, "127.0.0.1");

        // Assert
        _mockRefreshTokenRepository.Verify(
            x =>
                x.UpdateAsync(
                    It.Is<RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(t =>
                        t.Token == oldToken.Token && t.RevokedAt.HasValue && t.ReplacedByToken == result.RefreshToken.Content
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact(DisplayName = "RevokeAllRefreshTokensAsync should handle empty token list")]
    [Trait("Category", "Authentication")]
    [Trait("Category", "Bulk Operations")]
    public async Task RevokeAllRefreshTokensAsync_WithNoActiveTokens_ShouldNotUpdateRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var emptyTokenList = new List<RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>();

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetAllActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyTokenList);

        // Act
        await _sut.RevokeAllRefreshTokensAsync(userId, "Bulk revocation test");

        // Assert
        _mockRefreshTokenRepository.Verify(
            x => x.UpdateAsync(It.IsAny<RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Theory(DisplayName = "LoginAsync should validate password characteristics")]
    [Trait("Category", "Authentication")]
    [Trait("Category", "Password Validation")]
    [InlineData("", "Empty password")]
    [InlineData("   ", "Whitespace password")]
    [InlineData("short", "Short password")]
    [InlineData("verylongpasswordthatexceedsmaximumlength", "Long password")]
    [InlineData("password123", "Common password")]
    public async Task LoginAsync_WithInvalidPasswordCharacteristics_ShouldThrowBusinessException(string password, string scenario)
    {
        // Arrange
        User<Guid, Guid, Guid, Guid, Guid, Guid, Guid> user = CreateTestUser(Guid.NewGuid());
        var loginRequest = new LoginRequest<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(user, password, "127.0.0.1");

        _ = _mockConfiguration
            .Setup(x => x.GetInvalidPasswordMessageAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync($"Invalid password: {scenario}");

        // Act & Assert
        BusinessException exception = await Should.ThrowAsync<BusinessException>(async () => await _sut.LoginAsync(loginRequest));
        exception.Message.ShouldBe($"Invalid password: {scenario}");
    }

    [Fact(DisplayName = "RefreshToken should maintain correct token chain")]
    [Trait("Category", "Authentication")]
    [Trait("Category", "Token Chain")]
    public async Task RefreshTokenAsync_ShouldMaintainTokenChain()
    {
        // Arrange
        var userId = Guid.NewGuid();
        User<Guid, Guid, Guid, Guid, Guid, Guid, Guid> user = CreateTestUser(userId);
        RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid> initialToken = CreateTestRefreshToken(userId);
        var operationClaims = new List<OperationClaim<Guid>> { new("TestClaim") { Id = Guid.NewGuid() } };

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(initialToken.Token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(initialToken);
        _ = _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _ = _mockAuthorizationService
            .Setup(x => x.GetUserOperationClaimsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(operationClaims);

        // Act
        RefreshTokenResponse firstRefresh = await _sut.RefreshTokenAsync(initialToken.Token, "127.0.0.1");

        _ = _mockRefreshTokenRepository
            .Setup(x => x.GetByTokenAsync(firstRefresh.RefreshToken.Content, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(
                    userId,
                    firstRefresh.RefreshToken.Content,
                    firstRefresh.RefreshToken.ExpiresAt,
                    "127.0.0.1"
                )
            );

        RefreshTokenResponse secondRefresh = await _sut.RefreshTokenAsync(firstRefresh.RefreshToken.Content, "127.0.0.1");

        // Assert
        _mockRefreshTokenRepository.Verify(
            x =>
                x.UpdateAsync(
                    It.Is<RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(t =>
                        t.Token == initialToken.Token && t.ReplacedByToken == firstRefresh.RefreshToken.Content
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );

        _mockRefreshTokenRepository.Verify(
            x =>
                x.UpdateAsync(
                    It.Is<RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>(t =>
                        t.Token == firstRefresh.RefreshToken.Content && t.ReplacedByToken == secondRefresh.RefreshToken.Content
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    private static User<Guid, Guid, Guid, Guid, Guid, Guid, Guid> CreateTestUser(
        Guid userId,
        string password = "correct-password"
    )
    {
        using var hmac = new System.Security.Cryptography.HMACSHA512();
        byte[] passwordSalt = hmac.Key;
        byte[] passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

        return new User<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(passwordSalt: passwordSalt, passwordHash: passwordHash)
        {
            Id = userId,
        };
    }

    private static RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid> CreateTestRefreshToken(Guid userId)
    {
        return new RefreshToken<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(
            userId,
            Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            DateTime.UtcNow.AddDays(7),
            "127.0.0.1"
        );
    }
}
