namespace NArchitecture.Core.Application.Pipelines.Authorization;

/// <summary>
/// Interface for requests that require authorization.
/// </summary>
public interface ISecuredRequest
{
    /// <summary>
    /// Gets the role claims for authorization.
    /// </summary>
    RoleClaims RoleClaims { get; }
}
