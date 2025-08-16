namespace NArchitecture.Core.Application.Pipelines.Performance;

/// <summary>
/// Encapsulates the interval value used for performance threshold comparisons.
/// </summary>
/// <param name="interval">The interval value in seconds.</param>
public readonly struct IntervalOptions(int interval)
{
    /// <summary>
    /// Gets the interval value in seconds.
    /// </summary>
    public int Interval { get; } = interval;
}
