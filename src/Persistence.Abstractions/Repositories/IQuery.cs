namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

/// <summary>
/// Defines a query abstraction for retrieving entities of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IQuery<T>
{
    /// <summary>
    /// Returns a queryable collection of <typeparamref name="T"/>.
    /// </summary>
    /// <returns>An IQueryable of T.</returns>
    IQueryable<T> Query();
}
