using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;
using NArchitecture.Core.Security.Constants;
using NArchitecture.Core.Security.Extensions;

namespace NArchitecture.Core.Application.Pipelines.Authorization;

public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ISecuredRequest
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthorizationBehavior(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (!_httpContextAccessor.HttpContext?.User.Claims.Any() ?? true)
            throw new AuthorizationException("You are not authorized.");

        IEnumerable<string> userRoleClaims = _httpContextAccessor.HttpContext?.User?.GetRoleClaims() ?? Array.Empty<string>();
        string[] requestRoles =
            request.Roles?.Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role.Trim()).ToArray()
            ?? Array.Empty<string>();

        if (requestRoles.Any())
        {
            bool isAuthorized = userRoleClaims.Any(userRole =>
                userRole.Equals(GeneralOperationClaims.Admin, StringComparison.OrdinalIgnoreCase)
                || requestRoles.Any(requestRole => requestRole.Equals(userRole, StringComparison.OrdinalIgnoreCase))
            );

            if (!isAuthorized)
                throw new AuthorizationException("You are not authorized.");
        }

        TResponse response = await next();
        return response;
    }
}
