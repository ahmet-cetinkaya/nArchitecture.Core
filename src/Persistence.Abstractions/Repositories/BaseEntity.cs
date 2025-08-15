namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

/// <summary>
/// Serves as the base class for entities, incorporating common identifier and timestamp properties.
/// </summary>
/// <typeparam name="TId">The type representing the entity identifier.</typeparam>
public abstract class BaseEntity<TId>(TId? id) : IEntity<TId>, IEntityTimestamps
{
    /// <inheritdoc cref="IEntity{T}.Id"/>
    public TId? Id { get; set; } = id;

    /// <inheritdoc cref="IEntityTimestamps.CreatedAt"/>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <inheritdoc cref="IEntityTimestamps.UpdatedAt"/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc cref="IEntityTimestamps.DeletedAt"/>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Used for optimistic concurrency control. This property is automatically updated by the database.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];

    /// <summary>
    /// Protected parameterless constructor for reflection
    /// </summary>
    protected BaseEntity()
        : this(default) { }
}
