using System.Security.Authentication;
using Moq;
using NArchitecture.Core.Application.Pipelines.Auth;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Mediator.Abstractions;
using Shouldly;

namespace NArchitecture.Core.Application.Tests.Pipelines.Auth;

public sealed class MockSecuredRequest : ISecuredRequest, IRequest<int>
{
    private string[]? _identityRoles;
    private string[]? _requiredRoles;

    public RoleClaims RoleClaims => new(_identityRoles, _requiredRoles);

    public MockSecuredRequest()
    {
        _identityRoles = [];
        _requiredRoles = [];
    }

    public MockSecuredRequest SetRoles(string[]? identityRoles, string[]? requiredRoles)
    {
        return new MockSecuredRequest { _identityRoles = identityRoles, _requiredRoles = requiredRoles };
    }
}

[Trait("Category", "Authorization")]
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

    [Fact(DisplayName = "Handle should throw authentication exception when IdentityRoles is null")]
    public async Task Handle_WhenIdentityRolesIsNull_ShouldThrowAuthenticationException()
    {
        // Arrange: Create a request with null IdentityRoles.
        MockSecuredRequest request = new MockSecuredRequest().SetRoles(null!, []);

        // Act & Assert: Verify the correct exception is thrown.
        _ = await Should.ThrowAsync<AuthenticationException>(async () =>
            await _behavior.Handle(request, _next, CancellationToken.None)
        );
    }

    [Fact(DisplayName = "Handle should succeed when no required roles are provided")]
    public async Task Handle_WhenNoRequiredRoles_ShouldSucceed()
    {
        // Arrange: Create a request with no required roles.
        MockSecuredRequest request = new MockSecuredRequest().SetRoles(["user"], []);

        // Act: Execute authorization behavior.
        Exception exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert: Verify the operation succeeds.
        exception.ShouldBeNull();
    }

    [Fact(DisplayName = "Handle should succeed regardless of required roles when user has admin role")]
    public async Task Handle_WhenUserHasAdminRole_ShouldSucceedRegardlessOfRequiredRoles()
    {
        // Arrange: Create a request with admin role.
        MockSecuredRequest request = new MockSecuredRequest().SetRoles(["admin"], ["editor", "manager"]);

        // Act: Execute authorization behavior.
        Exception exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert: Verify access is granted.
        exception.ShouldBeNull("Admin role should bypass all role checks");
    }

    [Fact(DisplayName = "Handle should succeed when user has editor role")]
    public async Task Handle_WhenUserHasEditorRole_ShouldSucceed()
    {
        // Arrange: Create a request with editor role.
        await AssertRoleAccess(["editor"], ["editor"], true);
    }

    [Fact(DisplayName = "Handle should succeed when user has multiple valid roles")]
    public async Task Handle_WhenUserHasMultipleValidRoles_ShouldSucceed()
    {
        // Arrange: Create requests with multiple valid roles.
        await AssertRoleAccess(["editor", "user"], ["editor", "user"], true);
        await AssertRoleAccess(["manager", "editor"], ["editor", "manager"], true);
    }

    [Fact(DisplayName = "Handle should succeed when user roles have different cases")]
    public async Task Handle_WhenUserRolesHaveDifferentCases_ShouldSucceed()
    {
        // Arrange: Create requests with variant casing.
        await AssertRoleAccess(["EDITOR"], ["editor"], true);
        await AssertRoleAccess(["Editor"], ["editor"], true);
        await AssertRoleAccess(["editor"], ["EDITOR"], true);
    }

    [Fact(DisplayName = "Handle should succeed when admin role has different cases")]
    public async Task Handle_WhenAdminRoleHasDifferentCases_ShouldSucceed()
    {
        await AssertRoleAccess(["ADMIN"], ["editor", "manager"], true);
        await AssertRoleAccess(["Admin"], ["editor", "manager"], true);
        await AssertRoleAccess(["admin"], ["editor", "manager"], true);
    }

    [Fact(DisplayName = "Handle should succeed when roles have whitespace")]
    public async Task Handle_WhenRolesHaveWhitespace_ShouldSucceed()
    {
        // Arrange: Create a request with roles that include whitespace.
        await AssertRoleAccess([" editor "], [""], true);
        await AssertRoleAccess(["editor"], [], true);
        await AssertRoleAccess([" editor "], [" "], true);
    }

    [Fact(DisplayName = "Handle should throw authorization exception when user lacks required roles")]
    public async Task Handle_WhenUserLacksRequiredRoles_ShouldThrowAuthorizationException()
    {
        // Arrange: Create a request where user's roles do not match required roles.
        await AssertRoleAccess(["user"], ["admin", "editor"], false);
        await AssertRoleAccess(["guest"], ["editor"], false);
        await AssertRoleAccess(["viewer"], ["manager", "editor"], false);
    }

    [Fact(DisplayName = "Handle should throw authorization exception when user has no roles")]
    public async Task Handle_WhenUserHasNoRoles_ShouldThrowAuthorizationException()
    {
        // Arrange: Create a request with an empty set of user roles.
        MockSecuredRequest request = new MockSecuredRequest().SetRoles([], ["editor"]);

        // Act & Assert: Verify exception is thrown.
        _ = await Should.ThrowAsync<AuthorizationException>(async () =>
            await _behavior.Handle(request, _next, CancellationToken.None)
        );
    }

    [Fact(DisplayName = "Handle should succeed when required roles are empty")]
    public async Task Handle_WhenRequiredRolesIsEmpty_ShouldSucceed()
    {
        // Arrange: Create a request with empty required roles.
        MockSecuredRequest request = new MockSecuredRequest().SetRoles(["user"], []);

        // Act: Execute behavior.
        Exception exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert: Verify access is granted.
        exception.ShouldBeNull("Empty required roles should allow access");
    }

    [Fact(DisplayName = "Handle should succeed when required roles are whitespace")]
    public async Task Handle_WhenRequiredRolesIsWhitespace_ShouldSucceed()
    {
        // Arrange: Create a request with whitespace required roles.
        MockSecuredRequest request = new MockSecuredRequest().SetRoles(["user"], [" "]);

        // Act & Assert: Verify operation succeeds.
        Exception exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull("Whitespace required roles should allow access");
    }

    [Fact(DisplayName = "Handle should succeed when required roles are null")]
    public async Task Handle_WhenRequiredRolesIsNull_ShouldSucceed()
    {
        // Arrange: Create a request with null required roles.
        MockSecuredRequest request = new MockSecuredRequest().SetRoles(["user"], null!);

        // Act & Assert: Verify access is granted.
        Exception exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull("Null required roles should allow access");
    }

    [Fact(DisplayName = "Handle should match roles exactly when roles contain special characters")]
    public async Task Handle_WhenRolesContainSpecialCharacters_ShouldMatchExactly()
    {
        // Arrange: Create a request with special character roles.
        MockSecuredRequest request = new MockSecuredRequest().SetRoles(["editor@domain.com"], ["editor@domain.com"]);

        // Act & Assert: Verify exact matching.
        Exception exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull("Roles with special characters should match exactly");
    }

    [Fact(DisplayName = "Handle should succeed when roles are mixed empty and non-empty")]
    public async Task Handle_WhenMixedEmptyAndNonEmptyRoles_ShouldSucceed()
    {
        // Arrange: Create requests with a mix of empty and valid roles.
        // Case 1: Empty required role should grant access
        await AssertRoleAccess(["user"], ["", "editor"], true);

        // Case 2: Non-empty required role should be matched
        await AssertRoleAccess(["editor"], ["", "editor"], true);

        // Case 3: Empty user role should not affect matching
        await AssertRoleAccess(["user", "", "editor"], ["editor"], true);
    }

    [Fact(DisplayName = "Handle should handle boundary conditions correctly")]
    public async Task Handle_WhenBoundaryConditions_ShouldHandleCorrectly()
    {
        // Arrange: Create requests with boundary values.
        // Empty arrays
        await AssertRoleAccess([], [], true);

        // Single empty string vs null
        await AssertRoleAccess([""], null!, true);

        // Mix of empty and valid roles
        await AssertRoleAccess(["editor", ""], ["", "viewer"], true);
    }

    [Fact(DisplayName = "Handle should handle null values in role arrays")]
    public async Task Handle_WhenNullValuesInRoleArrays_ShouldHandle()
    {
        // Arrange: Create a request with null values in the role arrays.
        string[] identityRoles = ["user", null!, "editor"];
        string[] requiredRoles = ["editor", null!];
        MockSecuredRequest request = new MockSecuredRequest().SetRoles(identityRoles, requiredRoles);

        // Act & Assert: Verify no exception is thrown.
        Exception exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

        // Assert
        exception.ShouldBeNull();
    }

    [Fact(DisplayName = "Handle should succeed when multiple whitespace variations exist")]
    public async Task Handle_WhenMultipleWhitespaceVariations_ShouldSucceed()
    {
        // Arrange & Act & Assert: Create and test multiple whitespace scenarios.
        await AssertRoleAccess(["  editor  ", "\teditor\t", "editor\n"], ["editor"], true);
        await AssertRoleAccess(["editor"], ["  editor  ", "\teditor\t"], true);
    }

    [Fact(DisplayName = "Handle should succeed when all roles are empty strings")]
    public async Task Handle_WhenAllEmptyStrings_ShouldSucceed()
    {
        // Arrange: Create a request with all roles empty.
        await AssertRoleAccess(["", "", ""], ["", "", ""], true);
    }

    [Fact(DisplayName = "Handle should succeed when admin role has various whitespace")]
    public async Task Handle_WhenAdminRoleHasVariousWhitespace_ShouldSucceed()
    {
        // Arrange: Create a request with admin role having whitespace variations.
        await AssertRoleAccess([" admin"], ["editor"], true);
        await AssertRoleAccess(["admin "], ["editor"], true);
        await AssertRoleAccess(["\tadmin\t"], ["editor"], true);
        await AssertRoleAccess(["admin\n"], ["editor"], true);
    }

    [Fact(DisplayName = "Handle should succeed when roles are only whitespace")]
    public async Task Handle_WhenOnlyWhitespaceRoles_ShouldSucceed()
    {
        // Arrange: Create a request where roles are only whitespace.
        await AssertRoleAccess([" "], [" "], true);
        await AssertRoleAccess(["\t"], ["\t"], true);
        await AssertRoleAccess(["\n"], ["\n"], true);
    }

    [Fact(DisplayName = "RoleClaims should be unauthenticated when IdentityRoles is null")]
    public void RoleClaims_WhenIdentityRolesIsNull_IsAuthenticatedShouldBeFalse()
    {
        // Arrange: Create a RoleClaims instance with null IdentityRoles.
        var roleClaims = new RoleClaims(null, ["editor"]);

        // Assert: Verify IsAuthenticated is false.
        roleClaims.IsAuthenticated.ShouldBeFalse();

        // Additional check with HasAnyRequiredRole
        roleClaims.HasAnyRequiredRole().ShouldBeFalse("Unauthenticated users should not have any roles");
    }

    private async Task AssertRoleAccess(string[] userRoles, string[] requiredRoles, bool shouldSucceed)
    {
        // Arrange
        MockSecuredRequest request = new MockSecuredRequest().SetRoles(userRoles, requiredRoles);

        if (shouldSucceed)
        {
            // Act
            Exception exception = await Record.ExceptionAsync(() => _behavior.Handle(request, _next, CancellationToken.None));

            // Assert
            exception.ShouldBeNull(
                $"User with roles [{string.Join(", ", (userRoles ?? []).Select(role => role ?? string.Empty))}] should have access with required roles [{string.Join(", ", (requiredRoles ?? []).Select(role => role ?? string.Empty))}]"
            );
        }
        else
        {
            // Act & Assert
            _ = await Should.ThrowAsync<AuthorizationException>(async () =>
                await _behavior.Handle(request, _next, CancellationToken.None)
            );
        }
    }
}
