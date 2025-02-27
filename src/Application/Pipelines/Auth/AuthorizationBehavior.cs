using System.Runtime.CompilerServices;
using System.Security.Authentication;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Mediator.Abstractions;

namespace NArchitecture.Core.Application.Pipelines.Auth;

/// <summary>
/// Pipeline behavior that handles authorization for secured requests.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public sealed class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ISecuredRequest
{
    /// <summary>
    /// Handles the authorization check for the request.
    /// </summary>
    /// <exception cref="AuthenticationException">Thrown when the user is not authenticated.</exception>
    /// <exception cref="AuthorizationException">Thrown when the user is not authorized.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // Forces method inlining to reduce overhead in the pipeline
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Check authentication
        if (!request.RoleClaims.IsAuthenticated)
            return Task.FromException<TResponse>(new AuthenticationException(AUTHENTICATION_ERROR_MESSAGE));

        // Check authorization
        if (!request.RoleClaims.HasAnyRequiredRole())
            return Task.FromException<TResponse>(new AuthorizationException(AUTHORIZATION_ERROR_MESSAGE));

        return next();
    }

    private const string AUTHENTICATION_ERROR_MESSAGE = "User is not authenticated.";
    private const string AUTHORIZATION_ERROR_MESSAGE = "User is not authorized.";
}
