using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Moq;
using NArchitecture.Core.Security.WebApi.Swagger;
using Shouldly;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Core.Security.WebApi.Swagger.Tests;

[Trait("Category", "Security")]
public class BearerTokenSecurityOperationFilterTests
{
    private static OperationFilterContext CreateOperationContext()
    {
        var methodInfo = typeof(BearerTokenSecurityOperationFilterTests).GetMethod(nameof(DummyEndpoint))!;
        var controllerActionDescriptor = new ControllerActionDescriptor { MethodInfo = methodInfo };
        var apiDescription = new ApiDescription { ActionDescriptor = controllerActionDescriptor };
        var schemaRegistry = new Mock<ISchemaGenerator>();
        var schemaRepository = new SchemaRepository();

        return new OperationFilterContext(apiDescription, schemaRegistry.Object, schemaRepository, methodInfo);
    }

    private void DummyEndpoint() { }

    [Fact(DisplayName = "Apply should add Bearer token security requirement to operation")]
    public void Apply_ShouldAddBearerSecurityRequirement()
    {
        // Arrange
        var filter = new BearerTokenSecurityOperationFilter();
        var operation = new OpenApiOperation { Security = new List<OpenApiSecurityRequirement>() };
        var context = CreateOperationContext();

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Security.Count.ShouldBe(1);
        OpenApiSecurityRequirement requirement = operation.Security.First();
        requirement.Count.ShouldBe(1);

        OpenApiSecurityScheme securityScheme = requirement.Keys.First();
        securityScheme.Reference.Id.ShouldBe("Bearer");
        securityScheme.Reference.Type.ShouldBe(ReferenceType.SecurityScheme);
        securityScheme.Scheme.ShouldBe("oauth2");
        securityScheme.Name.ShouldBe("Bearer");
        securityScheme.In.ShouldBe(ParameterLocation.Header);
    }

    [Fact(DisplayName = "Apply should handle existing security requirements")]
    public void Apply_ShouldPreserveExistingSecurityRequirements()
    {
        // Arrange
        var filter = new BearerTokenSecurityOperationFilter();
        var existingRequirement = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Id = "Existing" } },
                new string[] { }
            },
        };
        var operation = new OpenApiOperation { Security = new List<OpenApiSecurityRequirement> { existingRequirement } };
        var context = CreateOperationContext();

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Security.Count.ShouldBe(2);
        operation.Security.First().Keys.First().Reference.Id.ShouldBe("Existing");
        operation.Security.Last().Keys.First().Reference.Id.ShouldBe("Bearer");
    }

    [Fact(DisplayName = "Apply should handle null security list")]
    public void Apply_ShouldHandleNullSecurityList()
    {
        // Arrange
        var filter = new BearerTokenSecurityOperationFilter();
        var operation = new OpenApiOperation { Security = null! };
        var context = CreateOperationContext();

        // Act
        filter.Apply(operation, context);

        // Assert
        _ = operation.Security.ShouldNotBeNull();
        operation.Security.Count.ShouldBe(1);
        operation.Security.First().Keys.First().Reference.Id.ShouldBe("Bearer");
    }

    [Theory(DisplayName = "Apply should handle different operation types")]
    [InlineData("get")]
    [InlineData("post")]
    [InlineData("put")]
    [InlineData("delete")]
    public void Apply_ShouldHandleDifferentOperationTypes(string operationType)
    {
        // Arrange
        var filter = new BearerTokenSecurityOperationFilter();
        var operation = new OpenApiOperation
        {
            Security = new List<OpenApiSecurityRequirement>(),
            OperationId = $"{operationType}Test",
        };
        var context = CreateOperationContext();

        // Act
        filter.Apply(operation, context);

        // Assert
        operation.Security.Count.ShouldBe(1);
        OpenApiSecurityRequirement requirement = operation.Security.First();
        requirement.Count.ShouldBe(1);
        requirement.Keys.First().Reference.Id.ShouldBe("Bearer");
    }
}
