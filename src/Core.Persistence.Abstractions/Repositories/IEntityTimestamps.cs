namespace NArchitecture.Core.Persistence.Abstractions.Repositories;

/// <summary>
/// Provides timestamp properties for entity auditing.
/// </summary>
public interface IEntityTimestamps
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated.
    /// </summary>
    DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was deleted.
    /// </summary>
    DateTime? DeletedDate { get; set; }
}
