using System.Security.Authentication;
using MediatR;
using Moq;
using NArchitecture.Core.Application.Pipelines.Authorization;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Authorization;

/// <summary>
/// Mock request class implementing ISecuredRequest for testing.
/// Uses ReadOnlySpan for efficient role claim handling.
/// </summary>
public sealed class MockSecuredRequest : ISecuredRequest, IRequest<int>
{
    private string[]? _identityRoles;
    private string[]? _requiredRoles;

    public RoleClaims RoleClaims => new(_identityRoles, _requiredRoles);

    public MockSecuredRequest()
    {
        _identityRoles = Array.Empty<string>();
        _requiredRoles = Array.Empty<string>();
    }

    public MockSecuredRequest SetRoles(string[]? identityRoles, string[]? requiredRoles)
    {
        return new MockSecuredRequest { _identityRoles = identityRoles, _requiredRoles = requiredRoles };
    }
}

public class AuthorizationBehaviorTests
{
    private readonly RequestHandlerDelegate<int> _next;
    private readonly AuthorizationBehavior<MockSecuredRequest, int> _behavior;
    private readonly IRequestHandler<MockSecuredRequest, int> _handler;

    public AuthorizationBehaviorTests()
    {
        _next = Mock.Of<RequestHandlerDelegate<int>>();
        _handler = Mock.Of<IRequestHandler<MockSecuredRequest, int>>();
        _behavior = new();
    }

    /// <summary>
    /// Tests that authentication exception is thrown when identity roles are null
    /// </summary>
    [Fact]
    public async Task Handle_WhenIdentityRolesIsNull_ShouldThrowAuthenticationException()
    {
        // Arrange
        var request = new MockSecuredRequest().SetRoles(null!, Array.Empty<string>());

        // Act & Assert
        await Should.ThrowAsync<AuthenticationException>(
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
        var request = new MockSecuredRequest().SetRoles(["user"], Array.Empty<string>());

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
        var request = new MockSecuredRequest().SetRoles(["admin"], ["editor", "manager"]);

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
        await AssertRoleAccess(["editor"], ["editor"], true);
    }

    /// <summary>
    /// Tests that multiple role combinations are properly validated
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserHasMultipleValidRoles_ShouldSucceed()
    {
        await AssertRoleAccess(["editor", "user"], ["editor", "user"], true);
        await AssertRoleAccess(["manager", "editor"], ["editor", "manager"], true);
    }

    /// <summary>
    /// Tests that role matching is case-insensitive
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserRolesHaveDifferentCases_ShouldSucceed()
    {
        await AssertRoleAccess(["EDITOR"], ["editor"], true);
        await AssertRoleAccess(["Editor"], ["editor"], true);
        await AssertRoleAccess(["editor"], ["EDITOR"], true);
    }

    /// <summary>
    /// Tests that admin role is recognized regardless of case
    /// </summary>
    [Fact]
    public async Task Handle_WhenAdminRoleHasDifferentCases_ShouldSucceed()
    {
        await AssertRoleAccess(["ADMIN"], ["editor", "manager"], true);
        await AssertRoleAccess(["Admin"], ["editor", "manager"], true);
        await AssertRoleAccess(["admin"], ["editor", "manager"], true);
    }

    /// <summary>
    /// Tests that role matching ignores leading and trailing whitespace
    /// </summary>
    [Fact]
    public async Task Handle_WhenRolesHaveWhitespace_ShouldSucceed()
    {
        await AssertRoleAccess([" editor "], [""], true);
        await AssertRoleAccess(["editor"], Array.Empty<string>(), true);
        await AssertRoleAccess([" editor "], [" "], true);
    }

    /// <summary>
    /// Tests that authorization fails when user lacks any of the required roles
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserLacksRequiredRoles_ShouldThrowAuthorizationException()
    {
        await AssertRoleAccess(["user"], ["admin", "editor"], false);
        await AssertRoleAccess(["guest"], ["editor"], false);
        await AssertRoleAccess(["viewer"], ["manager", "editor"], false);
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
        var request = new MockSecuredRequest().SetRoles(userRoles, requiredRoles);

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
            await Should.ThrowAsync<AuthorizationException>(
                async () => await _behavior.Handle(request, _next, CancellationToken.None)
            );
        }
    }

    /// <summary>
    /// Tests that authorization fails when user has no roles
    /// </summary>
    [Fact]
    public async Task Handle_WhenUserHasNoRoles_ShouldThrowAuthorizationException()
    {
        // Arrange
        var request = new MockSecuredRequest().SetRoles(Array.Empty<string>(), ["editor"]);

        // Act & Assert
        await Should.ThrowAsync<AuthorizationException>(
            async () => await _behavior.Handle(request, _next, CancellationToken.None)
        );
    }

    /// <summary>
    /// Tests that authorization succeeds with empty required roles
    /// </summary>
    [Fact]
    public async Task Handle_WhenRequiredRolesIsEmpty_ShouldSucceed()
    {
        // Arrange
        var request = new MockSecuredRequest().SetRoles(["user"], Array.Empty<string>());

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
        var request = new MockSecuredRequest().SetRoles(["user"], [" "]);

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
        var request = new MockSecuredRequest().SetRoles(["user"], null!);

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
        var request = new MockSecuredRequest().SetRoles(["editor@domain.com"], ["editor@domain.com"]);

        // Act
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull("Roles with special characters should match exactly");
    }

    /// <summary>
    /// Tests that authorization handles mixed roles with precise role matching
    /// </summary>
    [Fact]
    public async Task Handle_WhenMixedEmptyAndNonEmptyRoles_ShouldSucceed()
    {
        // Case 1: Empty required role should grant access
        await AssertRoleAccess(["user"], ["", "editor"], true);

        // Case 2: Non-empty required role should be matched
        await AssertRoleAccess(["editor"], ["", "editor"], true);

        // Case 3: Empty user role should not affect matching
        await AssertRoleAccess(["user", "", "editor"], ["editor"], true);
    }

    /// <summary>
    /// Tests boundary conditions for role matching
    /// </summary>
    [Fact]
    public async Task Handle_WhenBoundaryConditions_ShouldHandleCorrectly()
    {
        // Empty arrays
        await AssertRoleAccess(Array.Empty<string>(), Array.Empty<string>(), true);

        // Single empty string vs null
        await AssertRoleAccess([""], [null!], true);

        // Mix of empty and valid roles
        await AssertRoleAccess(["editor", ""], ["", "viewer"], true);
    }

    /// <summary>
    /// Tests that authorization handles null values in role arrays
    /// </summary>
    [Fact]
    public async Task Handle_WhenNullValuesInRoleArrays_ShouldHandle()
    {
        // Arrange
        string[] identityRoles = ["user", null!, "editor"];
        string[] requiredRoles = ["editor", null!];
        var request = new MockSecuredRequest().SetRoles(identityRoles, requiredRoles);

        // Act
        var exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull();
    }

    /// <summary>
    /// Tests that authorization succeeds with multiple whitespace variations
    /// </summary>
    [Fact]
    public async Task Handle_WhenMultipleWhitespaceVariations_ShouldSucceed()
    {
        await AssertRoleAccess(["  editor  ", "\teditor\t", "editor\n"], ["editor"], true);
        await AssertRoleAccess(["editor"], ["  editor  ", "\teditor\t"], true);
    }

    /// <summary>
    /// Tests that authorization handles empty strings in both identity and required roles
    /// </summary>
    [Fact]
    public async Task Handle_WhenAllEmptyStrings_ShouldSucceed()
    {
        await AssertRoleAccess(["", "", ""], ["", "", ""], true);
    }

    /// <summary>
    /// Tests admin role with various whitespace combinations
    /// </summary>
    [Fact]
    public async Task Handle_WhenAdminRoleHasVariousWhitespace_ShouldSucceed()
    {
        await AssertRoleAccess([" admin"], ["editor"], true);
        await AssertRoleAccess(["admin "], ["editor"], true);
        await AssertRoleAccess(["\tadmin\t"], ["editor"], true);
        await AssertRoleAccess(["admin\n"], ["editor"], true);
    }

    /// <summary>
    /// Tests that authorization handles single whitespace roles
    /// </summary>
    [Fact]
    public async Task Handle_WhenOnlyWhitespaceRoles_ShouldSucceed()
    {
        await AssertRoleAccess([" "], [" "], true);
        await AssertRoleAccess(["\t"], ["\t"], true);
        await AssertRoleAccess(["\n"], ["\n"], true);
    }

    /// <summary>
    /// Tests RoleClaims.IsAuthenticated property directly
    /// </summary>
    [Fact]
    public void RoleClaims_WhenIdentityRolesIsNull_IsAuthenticatedShouldBeFalse()
    {
        // Arrange
        var roleClaims = new RoleClaims(null, ["editor"]);

        // Assert
        roleClaims.IsAuthenticated.ShouldBeFalse();

        // Additional check with HasAnyRequiredRole
        roleClaims.HasAnyRequiredRole().ShouldBeFalse("Unauthenticated users should not have any roles");
    }
}
