using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NArchitecture.Core.Application.Pipelines.Authorization;

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
public readonly ref struct RoleClaims(string[]? identityRoles, string[]? requiredRoles)
{
    private readonly string[]? _identityRoles = identityRoles;
    private readonly string[]? _requiredRoles = requiredRoles;
    private readonly bool _hasAdminRole = identityRoles != null && ContainsAdmin(identityRoles);

    /// <summary>
    /// Checks if the roles array contains the admin role.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsAdmin(string[] roles)
    {
        const string adminRole = "Admin";
        foreach (var role in roles)
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
        if (_requiredRoles == null || _requiredRoles.Length == 0)
            return true;

        bool hasAnyValidRequiredRole = false;
        foreach (var required in _requiredRoles)
        {
            if (string.IsNullOrWhiteSpace(required))
                return true;

            hasAnyValidRequiredRole = true;
            var requiredSpan = required.AsSpan().Trim();

            foreach (var identity in _identityRoles!)
            {
                if (string.IsNullOrWhiteSpace(identity))
                    continue;
                if (identity.AsSpan().Trim().Equals(requiredSpan, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        return !hasAnyValidRequiredRole;
    }

    /// <summary>
    /// Gets a value indicating whether the identity is authenticated.
    /// </summary>
    public bool IsAuthenticated => _identityRoles != null;
}
