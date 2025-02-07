using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NArchitecture.Core.Security.Constants;

namespace NArchitecture.Core.Application.Pipelines.Authorization;

/// <summary>
/// Represents role claims for authorization.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly ref struct RoleClaims
{
    private readonly string[]? _identityRoles;
    private readonly string[]? _requiredRoles;
    private readonly bool _hasAdminRole;

    /// <summary>
    /// Initializes a new instance of the RoleClaims struct.
    /// </summary>
    /// <param name="identityRoles">The roles assigned to the identity.</param>
    /// <param name="requiredRoles">The roles required for authorization.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public RoleClaims(string[]? identityRoles, string[]? requiredRoles)
    {
        _identityRoles = identityRoles;
        _requiredRoles = requiredRoles;
        _hasAdminRole = identityRoles != null && ContainsAdmin(identityRoles);
    }

    /// <summary>
    /// Checks if the roles array contains the admin role.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsAdmin(string[] roles)
    {
        foreach (var role in roles)
        {
            if (string.IsNullOrWhiteSpace(role))
                continue;
            if (role.AsSpan().Trim().Equals(GeneralOperationClaims.Admin, StringComparison.OrdinalIgnoreCase))
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
        if (_requiredRoles == null)
            return true;

        bool hasAnyValidRequiredRole = false;
        foreach (var required in _requiredRoles)
        {
            if (string.IsNullOrWhiteSpace(required))
                continue;
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
