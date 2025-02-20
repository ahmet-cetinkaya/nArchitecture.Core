using System.Linq.Dynamic.Core;
using System.Text;
using NArchitecture.Core.Persistence.Abstractions.Dynamic;

namespace NArchitecture.Core.Persistence.EntityFramework.Dynamic;

/// <summary>
/// Provides extension methods for dynamically filtering and sorting IQueryable collections.
/// </summary>
public static class IQueryableDynamicFilterExtensions
{
    private static readonly string[] Orders = ["asc", "desc"];
    private static readonly string[] Logics = ["and", "or"];

    private static readonly Dictionary<string, string> Operators = new()
    {
        { "eq", "=" },
        { "neq", "!=" },
        { "lt", "<" },
        { "lte", "<=" },
        { "gt", ">" },
        { "gte", ">=" },
        { "isnull", "== null" },
        { "isnotnull", "!= null" },
        { "startswith", "StartsWith" },
        { "endswith", "EndsWith" },
        { "contains", "Contains" },
        { "doesnotcontain", "Contains" },
        { "in", "In" },
        { "between", "Between" },
    };

    /// <summary>
    /// Applies dynamic filtering and sorting to the query based on the provided dynamic query criteria.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the IQueryable.</typeparam>
    /// <param name="query">The source query.</param>
    /// <param name="dynamicQuery">The dynamic query criteria containing filter and sort instructions.</param>
    /// <returns>The modified query after applying dynamic filtering and sorting.</returns>
    public static IQueryable<T> ToDynamic<T>(this IQueryable<T> query, DynamicQuery dynamicQuery)
    {
        if (dynamicQuery.Filter is not null)
            query = Filter(query, dynamicQuery.Filter);
        if (dynamicQuery.Sort is not null && dynamicQuery.Sort.Any())
            query = Sort(query, dynamicQuery.Sort);
        return query;
    }

    /// <summary>
    /// Filters the IQueryable based on the provided filter criteria.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the IQueryable.</typeparam>
    /// <param name="queryable">The source query.</param>
    /// <param name="filter">The filter criteria.</param>
    /// <returns>The filtered IQueryable.</returns>
    private static IQueryable<T> Filter<T>(IQueryable<T> queryable, Filter filter)
    {
        IList<Filter> filters = GetAllFilters(filter);
        // Get filter values for dynamic Where clause parameters
        string?[] values = filters.Select(f => f.Value).ToArray();
        string where = Transform(filter, filters);
        if (!string.IsNullOrEmpty(where) && values != null)
            queryable = queryable.Where(where, values);

        return queryable;
    }

    /// <summary>
    /// Sorts the IQueryable based on the provided sort criteria.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the IQueryable.</typeparam>
    /// <param name="queryable">The source query.</param>
    /// <param name="sort">The sort criteria.</param>
    /// <returns>The sorted IQueryable.</returns>
    private static IQueryable<T> Sort<T>(IQueryable<T> queryable, IEnumerable<Sort> sort)
    {
        foreach (Sort item in sort)
        {
            if (string.IsNullOrEmpty(item.Field))
                throw new ArgumentException("Invalid Field");
            if (string.IsNullOrEmpty(item.Dir) || !Orders.Contains(item.Dir))
                throw new ArgumentException("Invalid Order Type");
        }

        if (sort.Any())
        {
            // Build ordering string in the format: "Field1 asc, Field2 desc"
            string ordering = string.Join(separator: ",", values: sort.Select(s => $"{s.Field} {s.Dir}"));
            return queryable.OrderBy(ordering);
        }

        return queryable;
    }

    /// <summary>
    /// Retrieves a flat list of all filters including nested ones.
    /// </summary>
    /// <param name="filter">The root filter.</param>
    /// <returns>A list that contains all filters.</returns>
    public static IList<Filter> GetAllFilters(Filter filter)
    {
        List<Filter> filters = [];
        GetFilters(filter, filters);
        return filters;
    }

    /// <summary>
    /// Recursively adds the filter and its nested filters to the list.
    /// </summary>
    /// <param name="filter">The current filter.</param>
    /// <param name="filters">The list that accumulates filters.</param>
    private static void GetFilters(Filter filter, IList<Filter> filters)
    {
        filters.Add(filter);
        if (filter.Filters is not null && filter.Filters.Any())
            foreach (Filter item in filter.Filters)
                GetFilters(item, filters);
    }

    /// <summary>
    /// Transforms the filter and its nested filters into a dynamic LINQ where clause.
    /// </summary>
    /// <param name="filter">The current filter.</param>
    /// <param name="filters">A list of all filters for parameter indexing.</param>
    /// <returns>The dynamic where clause expression.</returns>
    public static string Transform(Filter filter, IList<Filter> filters)
    {
        if (string.IsNullOrEmpty(filter.Field))
            throw new ArgumentException("Invalid Field");
        if (string.IsNullOrEmpty(filter.Operator) || !Operators.TryGetValue(filter.Operator, out string? comparison))
            throw new ArgumentException("Invalid Operator");

        int index = filters.IndexOf(filter);
        StringBuilder where = new();

        if (!string.IsNullOrEmpty(filter.Value))
            if (filter.Operator == "doesnotcontain")
                // Handle not containing logic
                _ = where.Append($"(!np({filter.Field}).{comparison}(@{index}))");
            else if (comparison is "StartsWith" or "EndsWith" or "Contains")
            {
                if (!filter.CaseSensitive)
                {
                    // Convert both field and parameter to lower case for case-insensitive comparison
                    _ = where.Append($"(np({filter.Field}).ToLower().{comparison}(@{index}.ToLower()))");
                }
                else
                {
                    _ = where.Append($"(np({filter.Field}).{comparison}(@{index}))");
                }
            }
            else if (filter.Operator == "in")
            {
                _ = where.Append($"np({filter.Field}) in ({filter.Value})");
            }
            else if (filter.Operator == "between")
            {
                // Split value into two parts and check between conditions
                string[] values = filter.Value.Split(',');
                if (values.Length != 2)
                    throw new ArgumentException("Invalid Value for 'between' operator");

                _ = where.Append($"(np({filter.Field}) >= {values[0]} and np({filter.Field}) <= {values[1]}))");
            }
            else
                _ = where.Append($"np({filter.Field}) {comparison} @{index}");
        else if (filter.Operator is "isnull" or "isnotnull")
            _ = where.Append($"np({filter.Field}) {comparison}");

        if (filter.Logic is not null && filter.Filters is not null && filter.Filters.Any())
        {
            if (!Logics.Contains(filter.Logic))
                throw new ArgumentException("Invalid Logic");
            // Combine current expression with nested filters using the logic operator.
            return $"{where} {filter.Logic} ({string.Join(separator: $" {filter.Logic} ", value: filter.Filters.Select(f => Transform(f, filters)).ToArray())})";
        }

        return where.ToString();
    }
}
