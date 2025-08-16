namespace NArchitecture.Core.Application.Pipelines.Performance;

/// <summary>
/// Represents a request with interval options for performance monitoring.
/// </summary>
public interface IIntervalRequest
{
    IntervalOptions IntervalOptions { get; init; }
}
