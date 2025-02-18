using System.Security.Claims;

namespace NArchitecture.Core.Security.Authorization.Extensions;

public static class ClaimCollectionExtensions
{
    public static void AddUserId<TUserId>(this ICollection<Claim> claims, TUserId userId)
    {
        if (userId == null)
            throw new ArgumentNullException(nameof(userId));

        claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()!));
    }

    public static void AddOperationClaim(this ICollection<Claim> claims, ICollection<string> operationClaims)
    {
        foreach (string role in operationClaims)
            claims.AddOperationClaim(role);
    }

    public static void AddOperationClaim(this ICollection<Claim> claims, string operationClaim)
    {
        claims.Add(new Claim(ClaimTypes.Role, operationClaim));
    }
}
