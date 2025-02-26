using Microsoft.Extensions.DependencyInjection;
using NArchitecture.Core.Mailing.Abstractions;
using NArchitecture.Core.Security.Abstractions.Authentication;
using NArchitecture.Core.Security.Abstractions.Authenticator;
using NArchitecture.Core.Security.Abstractions.Authenticator.Enums;

namespace NArchitecture.Core.Security.DependencyInjection.Tests;

public class SecurityServiceRegistrationTests
{
    [Fact(DisplayName = "Should register core services with minimal configuration")]
    [Trait("Category", "Integration")]
    public void AddSecurityServices_ShouldRegisterCoreServices_WithMinimalConfiguration()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var jwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        var authConfig = new Mock<IAuthenticatorConfiguration>();
        _ = authConfig.Setup(x => x.EnabledAuthenticatorTypes).Returns([]);
        SetupRequiredRepositories(services);

        // Act
        _ = services.AddSecurityServices<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(jwtConfig.Object, authConfig.Object);

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetService<IAuthenticator<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>().ShouldNotBeNull();
        _ = provider.GetService<IAuthenticationService<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>().ShouldNotBeNull();
    }

    [Fact(DisplayName = "Should throw when required repositories are missing")]
    [Trait("Category", "Validation")]
    public void AddSecurityServices_ShouldThrow_WhenRequiredRepositoriesAreMissing()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var jwtConfig = new Mock<IJwtAuthenticationConfiguration>();

        // Act & Assert
        _ = Should.Throw<InvalidOperationException>(
            () => services.AddSecurityServices<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(jwtConfig.Object)
        );
    }

    [Theory(DisplayName = "Should validate email service when email authenticator is enabled")]
    [Trait("Category", "Validation")]
    [InlineData(AuthenticatorType.Email)]
    public void AddSecurityServices_ShouldValidateEmailService_WhenEmailAuthenticatorEnabled(AuthenticatorType authenticatorType)
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var jwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        var authConfig = new Mock<IAuthenticatorConfiguration>();
        _ = authConfig.Setup(x => x.EnabledAuthenticatorTypes).Returns([authenticatorType]);
        SetupRequiredRepositories(services);

        // Act & Assert
        _ = Should.Throw<InvalidOperationException>(
            () => services.AddSecurityServices<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(jwtConfig.Object, authConfig.Object)
        );
    }

    [Theory(DisplayName = "Should validate SMS service when SMS authenticator is enabled")]
    [Trait("Category", "Validation")]
    [InlineData(AuthenticatorType.Sms)]
    public void AddSecurityServices_ShouldValidateSmsService_WhenSmsAuthenticatorEnabled(AuthenticatorType authenticatorType)
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var jwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        var authConfig = new Mock<IAuthenticatorConfiguration>();
        _ = authConfig.Setup(x => x.EnabledAuthenticatorTypes).Returns([authenticatorType]);
        SetupRequiredRepositories(services);

        // Act & Assert
        _ = Should.Throw<InvalidOperationException>(
            () => services.AddSecurityServices<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(jwtConfig.Object, authConfig.Object)
        );
    }

    [Fact(DisplayName = "Should register email services when email authenticator is enabled with valid configuration")]
    [Trait("Category", "Integration")]
    public void AddSecurityServices_ShouldRegisterEmailServices_WhenEmailAuthenticatorEnabledWithValidConfig()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();
        var jwtConfig = new Mock<IJwtAuthenticationConfiguration>();
        var authConfig = new Mock<IAuthenticatorConfiguration>();
        _ = authConfig.Setup(x => x.EnabledAuthenticatorTypes).Returns([AuthenticatorType.Email]);
        SetupRequiredRepositories(services);
        _ = services.AddScoped(_ => new Mock<IMailService>().Object);

        // Act
        _ = services.AddSecurityServices<Guid, Guid, Guid, Guid, Guid, Guid, Guid>(jwtConfig.Object, authConfig.Object);

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetService<IMailService>().ShouldNotBeNull();
    }

    private static void SetupRequiredRepositories(IServiceCollection services)
    {
        _ = services.AddScoped(_ => new Mock<IUserRepository<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>().Object);

        _ = services.AddScoped(_ => new Mock<IRefreshTokenRepository<Guid, Guid, Guid, Guid, Guid, Guid, Guid, Guid>>().Object);

        _ = services.AddScoped(_ => new Mock<IUserAuthenticatorRepository<Guid, Guid, Guid, Guid, Guid, Guid, Guid>>().Object);
    }
}
