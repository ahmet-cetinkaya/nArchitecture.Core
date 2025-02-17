namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

/// <summary>
/// Provides timestamp properties for entity auditing.
/// </summary>
public interface IEntityTimestamps
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated.
    /// </summary>
    DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was deleted.
    /// </summary>
    DateTime? DeletedAt { get; set; }
}
