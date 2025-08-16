using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NArchitecture.Core.Security.WebApi.Swagger;

public class BearerTokenSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        const string openApiSecurityScheme = "oauth2",
            openApiSecurityName = "Bearer";

        operation.Security ??= [];

        OpenApiSecurityRequirement openApiSecurityRequirement = new()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = openApiSecurityName },
                    Scheme = openApiSecurityScheme,
                    Name = openApiSecurityName,
                    In = ParameterLocation.Header,
                },
                Array.Empty<string>()
            },
        };
        operation.Security.Add(openApiSecurityRequirement);
    }
}
