using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace NArchitecture.Core.Application.Pipelines.Performance;

/// <summary>
/// Pipeline behavior for measuring request performance.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IIntervalRequest
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly Stopwatch _stopwatch;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, Stopwatch stopwatch)
    {
        _logger = logger;
        _stopwatch = stopwatch;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        // Get the request name.
        string requestName = request.GetType().Name;

        TResponse response;

        try
        {
            // Start timing.
            _stopwatch.Start();

            // Process the request.
            response = await next();
        }
        finally
        {
            // Stop the stopwatch.
            _stopwatch.Stop();

            // Log if elapsed time exceeds threshold.
            if (_stopwatch.Elapsed.TotalSeconds > request.IntervalOptions.Interval)
            {
                _logger.LogInformation(
                    $"Performance -> {requestName} took {_stopwatch.Elapsed.TotalSeconds}s, exceeding the threshold of {request.IntervalOptions.Interval}s"
                );
            }

            // Reset stopwatch for next measurement.
            _stopwatch.Reset();
        }

        // Return the response.
        return response;
    }
}
