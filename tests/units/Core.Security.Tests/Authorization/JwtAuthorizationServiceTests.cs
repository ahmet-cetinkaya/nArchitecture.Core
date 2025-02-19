using System.Security.Claims;
using Moq;
using NArchitecture.Core.Security.Abstractions.Authentication;
using NArchitecture.Core.Security.Abstractions.Authorization.Entities;
using NArchitecture.Core.Security.Authorization;
using Shouldly;

namespace Core.Security.Tests.Authorization;

public class JwtAuthorizationServiceTests
{
    private readonly Mock<IUserRepository<Guid, Guid, Guid>> _userRepositoryMock;
    private readonly JwtAuthorizationService<Guid, Guid, Guid> _authorizationService;

    public JwtAuthorizationServiceTests()
    {
        _userRepositoryMock = new();
        _authorizationService = new(_userRepositoryMock.Object);
    }

    [Theory(DisplayName = "HasPermission should return expected result for both async and sync versions")]
    [Trait("Category", "Authorization")]
    [InlineData("admin", true)]
    [InlineData("user", false)]
    public async Task HasPermission_ShouldReturnExpectedResult_ForBothVersions(string permissionName, bool expected)
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.HasPermissionAsync(userId, permissionName, default)).ReturnsAsync(expected);

        var claims = new List<Claim> { new(ClaimTypes.Role, expected ? permissionName : "other") };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act & Assert - Repository version
        bool asyncResult = await _authorizationService.HasPermissionAsync(userId, permissionName);
        asyncResult.ShouldBe(expected);

        // Act & Assert - ClaimsPrincipal version
        bool syncResult = await _authorizationService.HasPermissionAsync(claimsPrincipal, permissionName);
        syncResult.ShouldBe(expected);
    }

    [Theory(DisplayName = "HasAnyPermission should return expected result for both async and sync versions")]
    [Trait("Category", "Authorization")]
    [InlineData(new[] { "admin", "user" }, true)]
    [InlineData(new[] { "guest", "user" }, false)]
    public async Task HasAnyPermission_ShouldReturnExpectedResult_ForBothVersions(string[] permissionNames, bool expected)
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.HasAnyPermissionAsync(userId, permissionNames, default)).ReturnsAsync(expected);

        var claims = new List<Claim> { new(ClaimTypes.Role, expected ? permissionNames[0] : "other") };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act & Assert - Repository version
        bool asyncResult = await _authorizationService.HasAnyPermissionAsync(userId, permissionNames);
        asyncResult.ShouldBe(expected);

        // Act & Assert - ClaimsPrincipal version
        bool syncResult = await _authorizationService.HasAnyPermissionAsync(claimsPrincipal, permissionNames);
        syncResult.ShouldBe(expected);
    }

    [Theory(DisplayName = "HasAllPermissions should return expected result for both async and sync versions")]
    [Trait("Category", "Authorization")]
    [InlineData(new[] { "admin", "user" }, true)]
    [InlineData(new[] { "admin", "guest" }, false)]
    public async Task HasAllPermissions_ShouldReturnExpectedResult_ForBothVersions(string[] permissionNames, bool expected)
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.HasAllPermissionsAsync(userId, permissionNames, default)).ReturnsAsync(expected);

        var claims = new List<Claim>();
        if (expected)
            claims.AddRange(permissionNames.Select(p => new Claim(ClaimTypes.Role, p)));
        else
            claims.Add(new Claim(ClaimTypes.Role, permissionNames[0]));
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        // Act & Assert - Repository version
        bool asyncResult = await _authorizationService.HasAllPermissionsAsync(userId, permissionNames);
        asyncResult.ShouldBe(expected);

        // Act & Assert - ClaimsPrincipal version
        bool syncResult = await _authorizationService.HasAllPermissionsAsync(claimsPrincipal, permissionNames);
        syncResult.ShouldBe(expected);
    }

    [Fact(DisplayName = "GetUserOperationClaims should return operation claims from repository")]
    [Trait("Category", "Authorization")]
    public async Task GetUserOperationClaims_ShouldReturnOperationClaims_FromRepository()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedClaims = new List<OperationClaim<Guid>>
        {
            new OperationClaim<Guid>("admin") { Id = Guid.NewGuid() },
            new OperationClaim<Guid>("user") { Id = Guid.NewGuid() },
        };

        _userRepositoryMock.Setup(r => r.GetOperationClaimsAsync(userId, default)).ReturnsAsync(expectedClaims);

        // Act
        var result = await _authorizationService.GetUserOperationClaimsAsync(userId);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(expectedClaims.Count);
        result.ShouldBeEquivalentTo(expectedClaims);
    }

    [Fact(DisplayName = "HasPermission should handle empty claims principal")]
    [Trait("Category", "Authorization")]
    public async Task HasPermission_ShouldHandleEmptyClaimsPrincipal()
    {
        // Arrange
        var emptyClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        bool result = await _authorizationService.HasPermissionAsync(emptyClaimsPrincipal, "admin");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "HasAnyPermission should handle empty permission names")]
    [Trait("Category", "Authorization")]
    public async Task HasAnyPermission_ShouldHandleEmptyPermissionNames()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }));

        // Act
        bool result = await _authorizationService.HasAnyPermissionAsync(claimsPrincipal, Array.Empty<string>());

        // Assert
        result.ShouldBeFalse();
    }

    [Fact(DisplayName = "HasAllPermissions should handle empty permission names")]
    [Trait("Category", "Authorization")]
    public async Task HasAllPermissions_ShouldHandleEmptyPermissionNames()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }));

        // Act
        bool result = await _authorizationService.HasAllPermissionsAsync(claimsPrincipal, Array.Empty<string>());

        // Assert
        result.ShouldBeTrue();
    }
}
