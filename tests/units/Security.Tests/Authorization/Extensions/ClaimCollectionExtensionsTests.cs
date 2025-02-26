using System.Security.Claims;
using NArchitecture.Core.Security.Authorization.Extensions;
using Shouldly;

namespace NArchitecture.Core.Security.Tests.Authorization.Extensions;

public class ClaimCollectionExtensionsTests
{
    [Fact(DisplayName = "AddUserId should add name identifier claim with user id")]
    [Trait("Category", "Extensions")]
    public void AddUserId_ShouldAddNameIdentifierClaim_WithUserId()
    {
        // Arrange
        var claims = new List<Claim>();
        var userId = Guid.NewGuid();

        // Act
        claims.AddUserId(userId);

        // Assert
        claims.ShouldContain(claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == userId.ToString());
    }

    [Fact(DisplayName = "AddUserId should throw ArgumentNullException when userId is null")]
    [Trait("Category", "Extensions")]
    public void AddUserId_ShouldThrowArgumentNullException_WhenUserIdIsNull()
    {
        // Arrange
        var claims = new List<Claim>();
        string? nullUserId = null;

        // Act & Assert
        _ = Should.Throw<ArgumentNullException>(() => claims.AddUserId(nullUserId));
    }

    [Theory(DisplayName = "AddOperationClaim should add role claim with operation claim")]
    [Trait("Category", "Extensions")]
    [InlineData("admin")]
    [InlineData("user")]
    public void AddOperationClaim_ShouldAddRoleClaim_WithOperationClaim(string operationClaim)
    {
        // Arrange
        var claims = new List<Claim>();

        // Act
        claims.AddOperationClaim(operationClaim);

        // Assert
        claims.ShouldContain(claim => claim.Type == ClaimTypes.Role && claim.Value == operationClaim);
    }

    [Fact(DisplayName = "AddOperationClaim should add multiple role claims with operation claims collection")]
    [Trait("Category", "Extensions")]
    public void AddOperationClaim_ShouldAddMultipleRoleClaims_WithOperationClaimsCollection()
    {
        // Arrange
        var claims = new List<Claim>();
        string[] operationClaims = ["admin", "user", "manager"];

        // Act
        claims.AddOperationClaim(operationClaims);

        // Assert
        claims.Count.ShouldBe(operationClaims.Length);
        foreach (string operationClaim in operationClaims)
            claims.ShouldContain(claim => claim.Type == ClaimTypes.Role && claim.Value == operationClaim);
    }

    [Fact(DisplayName = "AddOperationClaim should handle empty operation claims collection")]
    [Trait("Category", "Extensions")]
    public void AddOperationClaim_ShouldHandleEmptyCollection()
    {
        // Arrange
        var claims = new List<Claim>();
        string[] emptyOperationClaims = [];

        // Act
        claims.AddOperationClaim(emptyOperationClaims);

        // Assert
        claims.ShouldBeEmpty();
    }
}
