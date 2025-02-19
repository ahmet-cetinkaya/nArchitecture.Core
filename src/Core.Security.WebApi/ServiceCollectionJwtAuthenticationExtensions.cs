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

        if (string.IsNullOrWhiteSpace(jwtConfiguration.SecurityKey))
            throw new ArgumentNullException(nameof(IJwtAuthenticationConfiguration.SecurityKey), "Security key cannot be null or empty.");
        
        if (jwtConfiguration.SecurityKey.Length < 16)
            throw new ArgumentException("Security key must be at least 16 characters long.", nameof(IJwtAuthenticationConfiguration.SecurityKey));

        if (jwtConfiguration.ValidateIssuer && string.IsNullOrWhiteSpace(jwtConfiguration.Issuer))
            throw new ArgumentNullException(nameof(IJwtAuthenticationConfiguration.Issuer), "Issuer cannot be null or empty when ValidateIssuer is true.");

        if (jwtConfiguration.ValidateAudience && string.IsNullOrWhiteSpace(jwtConfiguration.Audience))
            throw new ArgumentNullException(nameof(IJwtAuthenticationConfiguration.Audience), "Audience cannot be null or empty when ValidateAudience is true.");

        if (jwtConfiguration.ValidateLifetime)
        {
            if (jwtConfiguration.AccessTokenExpiration <= TimeSpan.Zero)
                throw new ArgumentException("Access token expiration must be greater than zero when ValidateLifetime is true.", 
                    nameof(IJwtAuthenticationConfiguration.AccessTokenExpiration));

            if (jwtConfiguration.RefreshTokenTTL <= TimeSpan.Zero)
                throw new ArgumentException("Refresh token TTL must be greater than zero.", 
                    nameof(IJwtAuthenticationConfiguration.RefreshTokenTTL));

            if (jwtConfiguration.RefreshTokenTTL <= jwtConfiguration.AccessTokenExpiration)
                throw new ArgumentException("Refresh token TTL must be greater than access token expiration.", 
                    nameof(IJwtAuthenticationConfiguration.RefreshTokenTTL));
        }

        if (jwtConfiguration.ClockSkew < TimeSpan.Zero)
            throw new ArgumentException("Clock skew cannot be negative.", nameof(IJwtAuthenticationConfiguration.ClockSkew));
    }
}
