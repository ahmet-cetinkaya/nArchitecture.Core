using System.Collections.Immutable;
using System.Security.Claims;

namespace NArchitecture.Core.Security.Authorization.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetClaim(this ClaimsPrincipal claimsPrincipal, string claimType)
    {
        return claimsPrincipal?.FindFirst(claimType)?.Value;
    }

    public static ICollection<string>? GetClaims(this ClaimsPrincipal claimsPrincipal, string claimType)
    {
        return claimsPrincipal?.FindAll(claimType)?.Select(x => x.Value).ToImmutableArray();
    }

    /// <summary>
    /// Get all <see cref="ClaimTypes.Role"/> claims.
    /// </summary>
    public static ICollection<string>? GetOperationClaims(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.GetClaims(ClaimTypes.Role);
    }

    /// <summary>
    /// Get the <see cref="ClaimTypes.NameIdentifier"/> claim.
    /// </summary>
    public static string? GetUserIdClaim(this ClaimsPrincipal claimsPrincipal)
    {
        return claimsPrincipal?.GetClaim(ClaimTypes.NameIdentifier);
    }
}
