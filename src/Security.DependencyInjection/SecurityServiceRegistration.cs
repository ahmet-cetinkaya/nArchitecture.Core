using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NArchitecture.Core.Mailing.Abstractions;
using NArchitecture.Core.Security.Abstractions.Authentication;
using NArchitecture.Core.Security.Abstractions.Authenticator;
using NArchitecture.Core.Security.Abstractions.Authenticator.Enums;
using NArchitecture.Core.Security.Abstractions.Authenticator.Otp;
using NArchitecture.Core.Security.Abstractions.Authorization;
using NArchitecture.Core.Security.Abstractions.Cryptography.Generation;
using NArchitecture.Core.Security.Authentication;
using NArchitecture.Core.Security.Authenticator;
using NArchitecture.Core.Security.Authenticator.Otp.OtpNet;
using NArchitecture.Core.Security.Authorization;
using NArchitecture.Core.Security.Cryptography.Generation;
using NArchitecture.Core.Sms.Abstractions;

namespace NArchitecture.Core.Security.DependencyInjection;

public static class SecurityServiceRegistration
{
    public static IServiceCollection AddSecurityServices<
        TOperationClaimId,
        TRefreshTokenId,
        TUserAuthenticatorId,
        TUserGroupId,
        TUserId,
        TUserInGroupId,
        TUserOperationClaimId
    >(
        this IServiceCollection services,
        IJwtAuthenticationConfiguration jwtConfiguration,
        IAuthenticatorConfiguration? authenticatorConfiguration = null
    )
    {
        IAuthenticatorConfiguration config = authenticatorConfiguration ?? new DefaultAuthenticatorConfiguration();
        ValidateRequiredServices(services, config);

        _ = services
            .AddAuthenticatorServices<
                TOperationClaimId,
                TRefreshTokenId,
                TUserAuthenticatorId,
                TUserGroupId,
                TUserId,
                TUserInGroupId,
                TUserOperationClaimId
            >(config)
            .AddAuthenticationServices<
                TOperationClaimId,
                TRefreshTokenId,
                TUserAuthenticatorId,
                TUserGroupId,
                TUserId,
                TUserInGroupId,
                TUserOperationClaimId
            >(jwtConfiguration)
            .AddAuthorizationServices<
                TOperationClaimId,
                TRefreshTokenId,
                TUserAuthenticatorId,
                TUserGroupId,
                TUserId,
                TUserInGroupId,
                TUserOperationClaimId
            >();

        return services;
    }

    private static IServiceCollection AddAuthenticatorServices<
        TOperationClaimId,
        TRefreshTokenId,
        TUserAuthenticatorId,
        TUserGroupId,
        TUserId,
        TUserInGroupId,
        TUserOperationClaimId
    >(this IServiceCollection services, IAuthenticatorConfiguration configuration)
    {
        services.TryAddSingleton<ICodeGenerator, CodeGenerator>();
        services.TryAddSingleton<IAuthenticatorConfiguration>(configuration);

        // Register optional authenticator services based on configuration
        if (configuration.EnabledAuthenticatorTypes.Contains(AuthenticatorType.Email))
            ValidateMailService(services);

        if (configuration.EnabledAuthenticatorTypes.Contains(AuthenticatorType.Sms))
            ValidateSmsService(services);

        if (configuration.EnabledAuthenticatorTypes.Contains(AuthenticatorType.Otp))
            services.TryAddScoped<IOtpService, OtpNetOtpService>();

        services.TryAddScoped<
            IAuthenticatorService<
                TOperationClaimId,
                TRefreshTokenId,
                TUserAuthenticatorId,
                TUserGroupId,
                TUserId,
                TUserInGroupId,
                TUserOperationClaimId
            >,
            AuthenticatorService<
                TOperationClaimId,
                TRefreshTokenId,
                TUserAuthenticatorId,
                TUserGroupId,
                TUserId,
                TUserInGroupId,
                TUserOperationClaimId
            >
        >();

        return services;
    }

    private static IServiceCollection AddAuthenticationServices<
        TOperationClaimId,
        TRefreshTokenId,
        TUserAuthenticatorId,
        TUserGroupId,
        TUserId,
        TUserInGroupId,
        TUserOperationClaimId
    >(this IServiceCollection services, IJwtAuthenticationConfiguration jwtConfiguration)
    {
        services.TryAddSingleton(jwtConfiguration);
        services.TryAddSingleton<IJwtAuthenticationConfiguration>(jwtConfiguration);

        services.TryAddScoped<
            IAuthenticationService<
                TOperationClaimId,
                TRefreshTokenId,
                TUserAuthenticatorId,
                TUserGroupId,
                TUserId,
                TUserInGroupId,
                TUserOperationClaimId
            >,
            JwtAuthenticationService<
                TOperationClaimId,
                TRefreshTokenId,
                TUserAuthenticatorId,
                TUserGroupId,
                TUserId,
                TUserInGroupId,
                TUserOperationClaimId
            >
        >();

        return services;
    }

    private static IServiceCollection AddAuthorizationServices<
        TOperationClaimId,
        TRefreshTokenId,
        TUserAuthenticatorId,
        TUserGroupId,
        TUserId,
        TUserInGroupId,
        TUserOperationClaimId
    >(this IServiceCollection services)
    {
        services.TryAddScoped<
            IAuthorizationService<TUserId, TOperationClaimId>,
            JwtAuthorizationService<
                TOperationClaimId,
                TRefreshTokenId,
                TUserAuthenticatorId,
                TUserGroupId,
                TUserId,
                TUserInGroupId,
                TUserOperationClaimId
            >
        >();

        return services;
    }

    private static void ValidateRequiredServices(IServiceCollection services, IAuthenticatorConfiguration configuration)
    {
        List<(Type ServiceType, string ErrorMessage)> requiredServices = [];

        requiredServices.AddRange(
            new[]
            {
                (
                    typeof(IUserRepository<,,,,,,>),
                    "No implementation of IUserRepository<,,> has been registered in the service collection. Please register an implementation before adding security services."
                ),
                (
                    typeof(IRefreshTokenRepository<,,,,,,,>),
                    "No implementation of IRefreshTokenRepository<,,> has been registered in the service collection. Please register an implementation before adding security services."
                ),
                (
                    typeof(IUserAuthenticatorRepository<,,,,,,>),
                    "No implementation of IUserAuthenticatorRepository<,> has been registered in the service collection. Please register an implementation before adding security services."
                ),
            }
        );

        foreach ((Type serviceType, string errorMessage) in requiredServices)
        {
            if (
                !services.Any(s =>
                    s.ServiceType.IsGenericType
                        ? s.ServiceType.GetGenericTypeDefinition() == serviceType
                        : s.ServiceType == serviceType
                )
            )
            {
                throw new InvalidOperationException(errorMessage);
            }
        }
    }

    private static void ValidateMailService(IServiceCollection services)
    {
        if (!services.Any(s => s.ServiceType == typeof(IMailService)))
            throw new InvalidOperationException(
                "No implementation of IMailService has been registered in the service collection. "
                    + "Either disable email authentication in the configuration or register an implementation of IMailService."
            );
    }

    private static void ValidateSmsService(IServiceCollection services)
    {
        if (!services.Any(s => s.ServiceType == typeof(ISmsService)))
            throw new InvalidOperationException(
                "No implementation of ISmsService has been registered in the service collection. "
                    + "Either disable SMS authentication in the configuration or register an implementation of ISmsService."
            );
    }
}
