using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NArchitecture.Core.Application.Pipelines.Auth;

/// <summary>
/// Represents role claims for authorization.
/// </summary>
/// <remarks>
/// Initializes a new instance of the RoleClaims struct.
/// </remarks>
/// <param name="identityRoles">The roles assigned to the identity.</param>
/// <param name="requiredRoles">The roles required for authorization.</param>
[StructLayout(LayoutKind.Auto)]
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly ref struct AuthOptions(string[]? identityRoles, string[]? requiredRoles)
{
    private readonly string[]? _identityRoles = identityRoles;
    private readonly string[]? _requiredRoles = requiredRoles;
    private readonly bool _hasAdminRole = identityRoles != null && ContainsAdmin(identityRoles);

    /// <summary>
    /// Checks if the roles array contains the admin role.
    /// </summary>
    /// <param name="roles"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsAdmin(string[] roles)
    {
        const string adminRole = "Admin";
        foreach (string role in roles)
        {
            if (string.IsNullOrWhiteSpace(role))
                continue;
            if (role.AsSpan().Trim().Equals(adminRole, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if the identity has any of the required roles.
    /// </summary>
    /// <returns>True if the identity has required roles or is admin; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasAnyRequiredRole()
    {
        if (!IsAuthenticated)
            return false;

        if (_hasAdminRole)
            return true;

        if (_requiredRoles is null || _requiredRoles.Length == 0)
            return true;

        // If there are no valid roles to check against, access is granted.
        bool hasValidRequiredRoles = false;
        foreach (string required in _requiredRoles)
        {
            if (!string.IsNullOrWhiteSpace(required))
            {
                hasValidRequiredRoles = true;
                break;
            }
        }

        if (!hasValidRequiredRoles)
            return true;

        foreach (string required in _requiredRoles)
        {
            if (string.IsNullOrWhiteSpace(required))
                continue;

            ReadOnlySpan<char> requiredSpan = required.AsSpan().Trim();
            foreach (string identity in _identityRoles!)
            {
                if (string.IsNullOrWhiteSpace(identity))
                    continue;

                if (identity.AsSpan().Trim().Equals(requiredSpan, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether the identity is authenticated.
    /// </summary>
    public bool IsAuthenticated => _identityRoles != null;
}
