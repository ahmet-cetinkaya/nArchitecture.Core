namespace NArchitecture.Core.Application.Pipelines.Auth;

/// <summary>
/// Interface for requests that require authorization.
/// </summary>
public interface ISecuredRequest
{
    /// <summary>
    /// Gets the role claims for authorization.
    /// </summary>
    AuthOptions AuthOptions { get; }
}
