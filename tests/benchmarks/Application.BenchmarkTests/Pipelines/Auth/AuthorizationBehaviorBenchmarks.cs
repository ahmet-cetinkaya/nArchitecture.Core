using BenchmarkDotNet.Attributes;
using MediatR;
using NArchitecture.Core.Application.Pipelines.Auth;
using NArchitecture.Core.CrossCuttingConcerns.Exception.Types;

namespace NArchitecture.Core.Application.BenchmarkTests.Pipelines.Auth;

/// <summary>
/// Benchmark tests for AuthorizationBehavior performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
internal class AuthorizationBehaviorBenchmarks
{
    private AuthorizationBehavior<TestRequest, TestResponse>? _behavior;
    private TestRequest? _adminRequest;
    private TestRequest? _matchingRoleRequest;
    private TestRequest? _nonMatchingRoleRequest;
    private TestRequest? _multiRoleRequest;
    private static readonly Task<TestResponse> _cachedResponse = Task.FromResult(new TestResponse());

    [GlobalSetup]
    public void Setup()
    {
        _ = new TestRequestHandler();
        _behavior = new();

        _adminRequest = new TestRequest(["admin"], ["test"]);
        _matchingRoleRequest = new TestRequest(["test"], ["test"]);
        _nonMatchingRoleRequest = new TestRequest(["user"], ["admin"]);
        _multiRoleRequest = new TestRequest(["user", "manager", "test"], ["test"]);
    }

    /// <summary>
    /// Benchmark for admin role authorization.
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<TestResponse> AdminRole()
    {
        return await _behavior!.Handle(_adminRequest!, () => _cachedResponse, default);
    }

    /// <summary>
    /// Benchmark for matching role authorization.
    /// </summary>
    [Benchmark]
    public async Task<TestResponse> MatchingRole()
    {
        return await _behavior!.Handle(_matchingRoleRequest!, () => _cachedResponse, default);
    }

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
    public async Task<TestResponse> MultipleRoles()
    {
        return await _behavior!.Handle(_multiRoleRequest!, () => _cachedResponse, default);
    }

    private sealed record TestRequest(string[] IdentityRoles, string[] RequiredRoles) : IRequest<TestResponse>, ISecuredRequest
    {
        public RoleClaims RoleClaims => new(IdentityRoles, RequiredRoles);
    }

    internal sealed record TestResponse;

    private sealed class TestRequestHandler : IRequestHandler<TestRequest, TestResponse>
    {
        public Task<TestResponse> Handle(TestRequest request, CancellationToken ct)
        {
            return Task.FromResult(new TestResponse());
        }
    }
}
