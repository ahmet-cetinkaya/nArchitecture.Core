namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

/// <summary>
/// Serves as the base class for entities, incorporating common identifier and timestamp properties.
/// </summary>
/// <typeparam name="TId">The type representing the entity identifier.</typeparam>
public abstract class Entity<TId> : IEntity<TId>, IEntityTimestamps
{
    /// <inheritdoc cref="IEntity{T}.Id"/>
    public TId? Id { get; set; }

    /// <inheritdoc cref="IEntityTimestamps.CreatedDate"/>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <inheritdoc cref="IEntityTimestamps.UpdatedDate"/>
    public DateTime? UpdatedDate { get; set; }

    /// <inheritdoc cref="IEntityTimestamps.DeletedDate"/>
    public DateTime? DeletedDate { get; set; }
}
