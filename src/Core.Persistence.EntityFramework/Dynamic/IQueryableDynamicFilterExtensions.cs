using NArchitecture.Core.Persistence.Abstractions.Dynamic;
using System.Linq.Dynamic.Core;
using System.Text;

namespace NArchitecture.Core.Persistence.EntityFramework.Dynamic;

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

    public static IQueryable<T> ToDynamic<T>(this IQueryable<T> query, DynamicQuery dynamicQuery)
    {
        if (dynamicQuery.Filter is not null)
            query = Filter(query, dynamicQuery.Filter);
        if (dynamicQuery.Sort is not null && dynamicQuery.Sort.Any())
            query = Sort(query, dynamicQuery.Sort);
        return query;
    }

    private static IQueryable<T> Filter<T>(IQueryable<T> queryable, Filter filter)
    {
        IList<Filter> filters = GetAllFilters(filter);
        string?[] values = filters.Select(f => f.Value).ToArray();
        string where = Transform(filter, filters);
        if (!string.IsNullOrEmpty(where) && values != null)
            queryable = queryable.Where(where, values);

        return queryable;
    }

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
            string ordering = string.Join(separator: ",", values: sort.Select(s => $"{s.Field} {s.Dir}"));
            return queryable.OrderBy(ordering);
        }

        return queryable;
    }

    public static IList<Filter> GetAllFilters(Filter filter)
    {
        List<Filter> filters = [];
        GetFilters(filter, filters);
        return filters;
    }

    private static void GetFilters(Filter filter, IList<Filter> filters)
    {
        filters.Add(filter);
        if (filter.Filters is not null && filter.Filters.Any())
            foreach (Filter item in filter.Filters)
                GetFilters(item, filters);
    }

    public static string Transform(Filter filter, IList<Filter> filters)
    {
        if (string.IsNullOrEmpty(filter.Field))
            throw new ArgumentException("Invalid Field");
        if (string.IsNullOrEmpty(filter.Operator) || !Operators.ContainsKey(filter.Operator))
            throw new ArgumentException("Invalid Operator");

        int index = filters.IndexOf(filter);
        string comparison = Operators[filter.Operator];
        StringBuilder where = new();

        if (!string.IsNullOrEmpty(filter.Value))
            if (filter.Operator == "doesnotcontain")
                _ = where.Append($"(!np({filter.Field}).{comparison}(@{index.ToString()}))");
            else if (comparison is "StartsWith" or "EndsWith" or "Contains")
            {
                if (!filter.CaseSensitive)
                {
                    _ = where.Append($"(np({filter.Field}).ToLower().{comparison}(@{index.ToString()}.ToLower()))");
                }
                else
                {
                    _ = where.Append($"(np({filter.Field}).{comparison}(@{index.ToString()}))");
                }
            }
            else if (filter.Operator == "in")
            {
                _ = where.Append($"np({filter.Field}) in ({filter.Value})");
            }
            else if (filter.Operator == "between")
            {
                string[] values = filter.Value.Split(',');
                if (values.Length != 2)
                    throw new ArgumentException("Invalid Value for 'between' operator");

                _ = where.Append($"(np({filter.Field}) >= {values[0]} and np({filter.Field}) <= {values[1]})");
            }
            else
                _ = where.Append($"np({filter.Field}) {comparison} @{index.ToString()}");
        else if (filter.Operator is "isnull" or "isnotnull")
            _ = where.Append($"np({filter.Field}) {comparison}");

        if (filter.Logic is not null && filter.Filters is not null && filter.Filters.Any())
        {
            if (!Logics.Contains(filter.Logic))
                throw new ArgumentException("Invalid Logic");
            return $"{where} {filter.Logic} ({string.Join(separator: $" {filter.Logic} ", value: filter.Filters.Select(f => Transform(f, filters)).ToArray())})";
        }

        return where.ToString();
    }
}
