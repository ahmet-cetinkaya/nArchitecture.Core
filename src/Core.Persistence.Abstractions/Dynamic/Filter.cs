namespace NArchitecture.Core.Persistence.Abstractions.Dynamic;

/// <summary>
/// Represents filter criteria for dynamic queries.
/// </summary>
public record Filter(string Field, string Operator)
{
    /// <summary>
    /// Gets or sets the field to filter by.
    /// </summary>
    public string Field { get; set; } = Field;

    /// <summary>
    /// Gets or sets the operator used for filtering.
    /// <para>Available operator values:</para>
    /// <list type="bullet">
    ///   <item><description>eq: Equal</description></item>
    ///   <item><description>neq: Not equal</description></item>
    ///   <item><description>lt: Less than</description></item>
    ///   <item><description>lte: Less than or equal</description></item>
    ///   <item><description>gt: Greater than</description></item>
    ///   <item><description>gte: Greater than or equal</description></item>
    ///   <item><description>isnull: Is null</description></item>
    ///   <item><description>isnotnull: Is not null</description></item>
    ///   <item><description>startswith: Starts with</description></item>
    ///   <item><description>endswith: Ends with</description></item>
    ///   <item><description>contains: Contains</description></item>
    ///   <item><description>doesnotcontain: Does not contain</description></item>
    ///   <item><description>in: In collection</description></item>
    ///   <item><description>between: Between two values</description></item>
    /// </list>
    /// </summary>
    public string Operator { get; set; } = Operator;

    /// <summary>
    /// Gets or sets the value for the filter operation.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the logical operator to combine nested filters (e.g., "and", "or").
    /// </summary>
    public string? Logic { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether the filter comparison is case sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Gets or sets the nested filters for complex query scenarios.
    /// </summary>
    public IEnumerable<Filter>? Filters { get; set; }
}
