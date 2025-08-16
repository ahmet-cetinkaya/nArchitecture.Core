namespace NArchitecture.Core.Mapper.Abstractions;

/// <summary>
/// Defines mapping operations between objects.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Maps the source object to a new instance of the destination type.
    /// </summary>
    /// <typeparam name="TDestination">Destination type</typeparam>
    /// <param name="source">Source object</param>
    /// <returns>Mapped destination object</returns>
    TDestination Map<TDestination>(object source);

    /// <summary>
    /// Maps the source object to a new instance of the destination type.
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TDestination">Destination type</typeparam>
    /// <param name="source">Source object</param>
    /// <returns>Mapped destination object</returns>
    TDestination Map<TSource, TDestination>(TSource source);

    /// <summary>
    /// Maps the source object to an existing instance of the destination type.
    /// </summary>
    /// <typeparam name="TSource">Source type</typeparam>
    /// <typeparam name="TDestination">Destination type</typeparam>
    /// <param name="source">Source object</param>
    /// <param name="destination">Destination object</param>
    /// <returns>Mapped destination object</returns>
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
}
