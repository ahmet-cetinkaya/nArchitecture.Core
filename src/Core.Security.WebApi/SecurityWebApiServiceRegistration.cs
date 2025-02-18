using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NArchitecture.Core.Security.Abstractions.Authentication;

namespace NArchitecture.Core.Security.WebApi;

public static class SecurityWebApiServiceRegistration
{
    public static IServiceCollection AddSecurityWebAPIServices(
        this IServiceCollection services,
        IJwtAuthenticationConfiguration jwtConfiguration
    )
    {
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
}
