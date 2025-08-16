using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NArchitecture.Core.Security.Abstractions.Authentication;

namespace NArchitecture.Core.Security.WebApi;

public static class ServiceCollectionJwtAuthenticationExtensions
{
    public static IServiceCollection ConfigureJwtAuthentication(
        this IServiceCollection services,
        IJwtAuthenticationConfiguration jwtConfiguration
    )
    {
        ValidateJwtConfiguration(jwtConfiguration);

        _ = services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuerSigningKey = jwtConfiguration.ValidateIssuerSigningKey,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfiguration.SecurityKey)),
                    ValidateAudience = jwtConfiguration.ValidateAudience,
                    ValidAudience = jwtConfiguration.Audience,
                    ValidateIssuer = jwtConfiguration.ValidateIssuer,
                    ValidIssuer = jwtConfiguration.Issuer,
                    ValidateLifetime = jwtConfiguration.ValidateLifetime,
                    ClockSkew = jwtConfiguration.ClockSkew,
                    RequireExpirationTime = jwtConfiguration.RequireExpirationTime,
                };
            });

        return services;
    }

    private static void ValidateJwtConfiguration(IJwtAuthenticationConfiguration jwtConfiguration)
    {
        ArgumentNullException.ThrowIfNull(jwtConfiguration);

        static string ConfigProperty(string propertyName) => $"{nameof(jwtConfiguration)}.{propertyName}";

        if (string.IsNullOrWhiteSpace(jwtConfiguration.SecurityKey))
            throw new ArgumentNullException(
                ConfigProperty(nameof(IJwtAuthenticationConfiguration.SecurityKey)),
                "Security key property cannot be null or empty."
            );

        if (jwtConfiguration.SecurityKey.Length < 16)
            throw new ArgumentException(
                "Security key property must be at least 16 characters long.",
                ConfigProperty(nameof(IJwtAuthenticationConfiguration.SecurityKey))
            );

        if (jwtConfiguration.ValidateIssuer && string.IsNullOrWhiteSpace(jwtConfiguration.Issuer))
            throw new ArgumentNullException(
                ConfigProperty(nameof(IJwtAuthenticationConfiguration.Issuer)),
                "Issuer property cannot be null or empty when ValidateIssuer is true."
            );

        if (jwtConfiguration.ValidateAudience && string.IsNullOrWhiteSpace(jwtConfiguration.Audience))
            throw new ArgumentNullException(
                ConfigProperty(nameof(IJwtAuthenticationConfiguration.Audience)),
                "Audience property cannot be null or empty when ValidateAudience is true."
            );

        if (jwtConfiguration.ValidateLifetime)
        {
            if (jwtConfiguration.AccessTokenExpiration <= TimeSpan.Zero)
                throw new ArgumentException(
                    "Access token expiration property must be greater than zero when ValidateLifetime is true.",
                    ConfigProperty(nameof(IJwtAuthenticationConfiguration.AccessTokenExpiration))
                );

            if (jwtConfiguration.RefreshTokenTTL <= TimeSpan.Zero)
                throw new ArgumentException(
                    "Refresh token TTL property must be greater than zero.",
                    ConfigProperty(nameof(IJwtAuthenticationConfiguration.RefreshTokenTTL))
                );

            if (jwtConfiguration.RefreshTokenTTL <= jwtConfiguration.AccessTokenExpiration)
                throw new ArgumentException(
                    "Refresh token TTL property must be greater than access token expiration.",
                    ConfigProperty(nameof(IJwtAuthenticationConfiguration.RefreshTokenTTL))
                );
        }

        if (jwtConfiguration.ClockSkew < TimeSpan.Zero)
            throw new ArgumentException(
                "Clock skew property cannot be negative.",
                ConfigProperty(nameof(IJwtAuthenticationConfiguration.ClockSkew))
            );
    }
}
