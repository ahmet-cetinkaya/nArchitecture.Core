namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

/// <summary>
/// Represents an entity with an identifier.
/// </summary>
/// <typeparam name="T">The type of the identifier.</typeparam>
public interface IEntity<T>
{
    /// <summary>
    /// Gets or sets the identifier for the entity.
    /// </summary>
    T Id { get; set; }
}
