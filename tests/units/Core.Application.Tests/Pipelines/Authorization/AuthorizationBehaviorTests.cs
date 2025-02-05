using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Moq;
using NArchitecture.Core.Application.Pipelines.Authorization;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Security.Constants;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Authorization;

public sealed class MockSecuredRequest : ISecuredRequest, IRequest<int>
{
    private string[] _roles;

    public string[] Roles => _roles;

    public MockSecuredRequest()
    {
        _roles = Array.Empty<string>();
    }

    public MockSecuredRequest SetRoles(string[] roles)
    {
        _roles = roles;
        return this;
    }
}

public class AuthorizationBehaviorTests
{
    private readonly RequestHandlerDelegate<int> _next;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AuthorizationBehavior<MockSecuredRequest, int> _behavior;

    public AuthorizationBehaviorTests()
    {
        _next = () => Task.FromResult(1);
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _behavior = new(_httpContextAccessorMock.Object);
    }

    private void SetupUserClaims(string[] roles)
    {
        var claims = roles.Select(role => new Claim(ClaimTypes.Role, role.Trim())).ToList();
        var claimsIdentity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
    }

    [Fact]
    public async Task Handle_WhenNoUserClaims_ShouldThrowAuthenticationException()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        var request = new MockSecuredRequest().SetRoles(new string[] { "editor" });

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthorizationException>(
            async () => await _behavior.Handle(request, _next, CancellationToken.None)
        );
    }

    /// <summary>
    /// Tests that authorization succeeds when no roles are required
    /// </summary>
    [Fact]
    public async Task Handle_WhenNoRequiredRoles_ShouldSucceed()
    {
        // Arrange
        SetupUserClaims(new string[] { "user" });
        var request = new MockSecuredRequest().SetRoles(Array.Empty<string>());

        // Act
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull();
    }

    /// <summary>
    /// Tests that admin role bypasses all role requirements
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserHasAdminRole_ShouldSucceedRegardlessOfRequiredRoles()
    {
        // Arrange
        SetupUserClaims(new string[] { "admin" });
        var request = new MockSecuredRequest().SetRoles(new string[] { "editor", "manager" });

        // Act
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull("Admin role should bypass all role checks");
    }

    /// <summary>
    /// Tests that editor role access is granted when required
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserHasEditorRole_ShouldSucceed()
    {
        await AssertRoleAccess(new string[] { "editor" }, new string[] { "editor" }, true);
    }

    /// <summary>
    /// Tests that multiple role combinations are properly validated
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserHasMultipleValidRoles_ShouldSucceed()
    {
        await AssertRoleAccess(new string[] { "editor", "user" }, new string[] { "editor", "user" }, true);
        await AssertRoleAccess(new string[] { "manager", "editor" }, new string[] { "editor", "manager" }, true);
    }

    /// <summary>
    /// Tests that role matching is case-insensitive
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserRolesHaveDifferentCases_ShouldSucceed()
    {
        await AssertRoleAccess(new string[] { "EDITOR" }, new string[] { "editor" }, true);
        await AssertRoleAccess(new string[] { "Editor" }, new string[] { "editor" }, true);
        await AssertRoleAccess(new string[] { "editor" }, new string[] { "EDITOR" }, true);
    }

    /// <summary>
    /// Tests that admin role is recognized regardless of case
    /// </summary>
    [Fact]
    public async Task Handle_WhenAdminRoleHasDifferentCases_ShouldSucceed()
    {
        await AssertRoleAccess(new string[] { "ADMIN" }, new string[] { "editor", "manager" }, true);
        await AssertRoleAccess(new string[] { "Admin" }, new string[] { "editor", "manager" }, true);
        await AssertRoleAccess(new string[] { "admin" }, new string[] { "editor", "manager" }, true);
    }

    /// <summary>
    /// Tests that role matching ignores leading and trailing whitespace
    /// </summary>
    [Fact]
    public async Task Handle_WhenRolesHaveWhitespace_ShouldSucceed()
    {
        await AssertRoleAccess(new string[] { " editor " }, new string[] { "" }, true);
        await AssertRoleAccess(new string[] { "editor" }, Array.Empty<string>(), true);
        await AssertRoleAccess(new string[] { " editor " }, new string[] { " " }, true);
    }

    /// <summary>
    /// Tests that authorization fails when user lacks any of the required roles
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserLacksRequiredRoles_ShouldThrowAuthorizationException()
    {
        await AssertRoleAccess(new string[] { "user" }, new string[] { "admin", "editor" }, false);
        await AssertRoleAccess(new string[] { "guest" }, new string[] { "editor" }, false);
        await AssertRoleAccess(new string[] { "viewer" }, new string[] { "manager", "editor" }, false);
    }

    /// <summary>
    /// Helper method to test role-based access scenarios.
    /// </summary>
    /// <param name="userRoles">Array of roles assigned to the user</param>
    /// <param name="requiredRoles">Array of required roles</param>
    /// <param name="shouldSucceed">Whether the authorization should succeed</param>
    private async Task AssertRoleAccess(string[] userRoles, string[] requiredRoles, bool shouldSucceed)
    {
        // Arrange
        SetupUserClaims(userRoles);
        var request = new MockSecuredRequest().SetRoles(requiredRoles);

        if (shouldSucceed)
        {
            // Act
            var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

            // Assert
            exception.ShouldBeNull(
                $"User with roles [{string.Join(", ", userRoles)}] should have access with required roles [{string.Join(", ", requiredRoles)}]"
            );
        }
        else
        {
            // Act & Assert
            var exception = await Should.ThrowAsync<AuthorizationException>(
                async () => await _behavior.Handle(request, _next, CancellationToken.None)
            );
            exception.Message.ShouldBe("You are not authorized.");
        }
    }

    /// <summary>
    /// Tests that authorization fails when user has no roles
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserHasNoRoles_ShouldThrowAuthorizationException()
    {
        // Arrange
        SetupUserClaims(Array.Empty<string>());
        var request = new MockSecuredRequest().SetRoles(new string[] { "editor" });

        // Act & Assert
        var exception = await Should.ThrowAsync<AuthorizationException>(
            async () => await _behavior.Handle(request, _next, CancellationToken.None)
        );

        exception.Message.ShouldBe("You are not authorized.");
    }

    /// <summary>
    /// Tests that authorization succeeds with empty required roles
    /// </summary>
    [Fact]
    public async Task Handle_WhenRequiredRolesIsEmpty_ShouldSucceed()
    {
        // Arrange
        SetupUserClaims(new string[] { "user" });
        var request = new MockSecuredRequest().SetRoles(Array.Empty<string>());

        // Act
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull("Empty required roles should allow access");
    }

    /// <summary>
    /// Tests that authorization succeeds with whitespace required roles
    /// </summary>
    [Fact]
    public async Task Handle_WhenRequiredRolesIsWhitespace_ShouldSucceed()
    {
        // Arrange
        SetupUserClaims(new string[] { "user" });
        var request = new MockSecuredRequest().SetRoles(new string[] { " " });

        // Act
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull("Whitespace required roles should allow access");
    }

    /// <summary>
    /// Tests that authorization succeeds with null required roles
    /// </summary>
    [Fact]
    public async Task Handle_WhenRequiredRolesIsNull_ShouldSucceed()
    {
        // Arrange
        SetupUserClaims(new string[] { "user" });
        var request = new MockSecuredRequest().SetRoles(null!);

        // Act
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull("Null required roles should allow access");
    }

    /// <summary>
    /// Tests that roles with special characters are matched exactly
    /// </summary>
    [Fact]
    public async Task Handle_WhenRolesContainSpecialCharacters_ShouldMatchExactly()
    {
        // Arrange
        SetupUserClaims(new string[] { "editor@domain.com" });
        var request = new MockSecuredRequest().SetRoles(new string[] { "editor@domain.com" });

        // Act
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull("Roles with special characters should match exactly");
    }
}
