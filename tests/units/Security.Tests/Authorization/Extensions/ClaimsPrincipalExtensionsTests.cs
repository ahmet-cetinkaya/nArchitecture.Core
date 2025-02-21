using System.Security.Claims;
using NArchitecture.Core.Security.Authorization.Extensions;
using Shouldly;

namespace NArchitecture.Core.Security.Tests.Authorization.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Theory(DisplayName = "GetClaim should return claim value for existing claim type")]
    [Trait("Category", "Extensions")]
    [InlineData(ClaimTypes.Role, "admin")]
    [InlineData(ClaimTypes.Name, "John Doe")]
    [InlineData(ClaimTypes.Email, "test@example.com")]
    public void GetClaim_ShouldReturnClaimValue_WhenClaimExists(string claimType, string expectedValue)
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim(claimType, expectedValue) });
        var principal = new ClaimsPrincipal(identity);

        // Act
        string? result = principal.GetClaim(claimType);

        // Assert
        result.ShouldBe(expectedValue);
    }

    [Fact(DisplayName = "GetClaim should return null for non-existent claim type")]
    [Trait("Category", "Extensions")]
    public void GetClaim_ShouldReturnNull_WhenClaimDoesNotExist()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        string? result = principal.GetClaim(ClaimTypes.Role);

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetClaims should return all values for specified claim type")]
    [Trait("Category", "Extensions")]
    public void GetClaims_ShouldReturnAllValues_ForSpecifiedClaimType()
    {
        // Arrange
        string[] expectedRoles = ["admin", "user", "manager"];
        IEnumerable<Claim> claims = expectedRoles.Select(role => new Claim(ClaimTypes.Role, role));
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        ICollection<string>? result = principal.GetClaims(ClaimTypes.Role);

        // Assert
        _ = result.ShouldNotBeNull();
        result.ShouldBe(expectedRoles);
    }

    [Fact(DisplayName = "GetClaims should return empty collection for non-existent claim type")]
    [Trait("Category", "Extensions")]
    public void GetClaims_ShouldReturnEmpty_WhenNoClaimsExist()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        ICollection<string>? result = principal.GetClaims(ClaimTypes.Role);

        // Assert
        _ = result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact(DisplayName = "GetOperationClaims should return all role claims")]
    [Trait("Category", "Extensions")]
    public void GetOperationClaims_ShouldReturnAllRoleClaims()
    {
        // Arrange
        string[] expectedRoles = ["admin", "user", "manager"];
        IEnumerable<Claim> claims = expectedRoles.Select(role => new Claim(ClaimTypes.Role, role));
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        // Act
        ICollection<string>? result = principal.GetOperationClaims();

        // Assert
        _ = result.ShouldNotBeNull();
        result.ShouldBe(expectedRoles);
    }

    [Fact(DisplayName = "GetUserIdClaim should return nameIdentifier claim value")]
    [Trait("Category", "Extensions")]
    public void GetUserIdClaim_ShouldReturnNameIdentifierValue()
    {
        // Arrange
        string expectedUserId = Guid.NewGuid().ToString();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, expectedUserId) });
        var principal = new ClaimsPrincipal(identity);

        // Act
        string? result = principal.GetUserIdClaim();

        // Assert
        result.ShouldBe(expectedUserId);
    }

    [Fact(DisplayName = "GetUserIdClaim should return null when nameIdentifier claim doesn't exist")]
    [Trait("Category", "Extensions")]
    public void GetUserIdClaim_ShouldReturnNull_WhenNameIdentifierDoesNotExist()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        string? result = principal.GetUserIdClaim();

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "Extension methods should handle null ClaimsPrincipal")]
    [Trait("Category", "Extensions")]
    public void ExtensionMethods_ShouldHandleNullClaimsPrincipal()
    {
        // Arrange
        ClaimsPrincipal? nullPrincipal = null;

        // Act & Assert
        nullPrincipal!.GetClaim(ClaimTypes.Role).ShouldBeNull();
        nullPrincipal!.GetClaims(ClaimTypes.Role).ShouldBeNull();
        nullPrincipal!.GetOperationClaims().ShouldBeNull();
        nullPrincipal!.GetUserIdClaim().ShouldBeNull();
    }
}
