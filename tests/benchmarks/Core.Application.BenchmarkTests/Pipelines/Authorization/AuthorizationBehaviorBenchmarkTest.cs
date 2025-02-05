using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.AspNetCore.Http;
using NArchitecture.Core.Application.Pipelines.Authorization;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

namespace Core.Application.BenchmarkTests.Pipelines.Authorization;

/// <summary>
/// Benchmark tests for AuthorizationBehavior performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class AuthorizationBehaviorBenchmark
{
    private AuthorizationBehavior<TestRequest, TestResponse>? _behavior;
    private TestRequest? _adminRequest;
    private TestRequest? _matchingRoleRequest;
    private TestRequest? _nonMatchingRoleRequest;
    private TestRequest? _multiRoleRequest;
    private static readonly Task<TestResponse> _cachedResponse = Task.FromResult(new TestResponse());
    private IHttpContextAccessor? _httpContextAccessor;

    [GlobalSetup]
    public void Setup()
    {
        _httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var userClaims = new List<Claim> { new(ClaimTypes.Role, "admin"), new(ClaimTypes.Role, "test") };
        _httpContextAccessor.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(userClaims));

        _behavior = new AuthorizationBehavior<TestRequest, TestResponse>(_httpContextAccessor);

        _adminRequest = new TestRequest(["admin"]);
        _matchingRoleRequest = new TestRequest(["test"]);
        _nonMatchingRoleRequest = new TestRequest(["manager"]);
        _multiRoleRequest = new TestRequest(["manager", "test"]);
    }

    /// <summary>
    /// Benchmark for admin role authorization.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<TestResponse> AdminRole() => await _behavior!.Handle(_adminRequest!, () => _cachedResponse, default);

    /// <summary>
    /// Benchmark for matching role authorization.
    /// </summary>
    [Benchmark]
    public async Task<TestResponse> MatchingRole() =>
        await _behavior!.Handle(_matchingRoleRequest!, () => _cachedResponse, default);

    /// <summary>
    /// Benchmark for non-matching role authorization.
    /// </summary>
    [Benchmark]
    public async Task<TestResponse> NonMatchingRole()
    {
        try
        {
            return await _behavior!.Handle(_nonMatchingRoleRequest!, () => _cachedResponse, default);
        }
        catch (AuthorizationException)
        {
            return new TestResponse();
        }
    }

    /// <summary>
    /// Benchmark for multiple roles authorization.
    /// </summary>
    [Benchmark]
    public async Task<TestResponse> MultipleRoles() =>
        await _behavior!.Handle(_multiRoleRequest!, () => _cachedResponse, default);

    private sealed record TestRequest(string[] Roles) : IRequest<TestResponse>, ISecuredRequest;

    public sealed record TestResponse;

    private sealed class TestRequestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public Task<TestResponse> Handle(TestRequest request, CancellationToken ct) => Task.FromResult(new TestResponse());
    }
}
