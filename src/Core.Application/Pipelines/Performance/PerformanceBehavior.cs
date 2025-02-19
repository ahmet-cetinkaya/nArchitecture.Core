using System.Diagnostics;
using MediatR;
using NArchitecture.Core.CrossCuttingConcerns.Logging.Abstraction;

namespace NArchitecture.Core.Application.Pipelines.Performance;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IIntervalRequest
{
    private readonly ILogger _logger;
    private readonly Stopwatch _stopwatch;

    public PerformanceBehavior(ILogger logger, Stopwatch stopwatch)
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
                await _logger.InformationAsync(
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
