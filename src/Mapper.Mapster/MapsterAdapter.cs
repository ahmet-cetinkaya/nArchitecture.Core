using Mapster;

namespace NArchitecture.Core.Mapper.Mapster;

/// <summary>
/// Provides an adapter implementation for Mapster to work with the application's mapping interface.
/// </summary>
public class MapsterAdapter : NArchitecture.Core.Mapper.Abstractions.IMapper
{
    private readonly TypeAdapterConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsterAdapter"/> class.
    /// </summary>
    /// <param name="config">The TypeAdapterConfig instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    public MapsterAdapter(TypeAdapterConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MapsterAdapter"/> class using the global TypeAdapterConfig.
    /// </summary>
    public MapsterAdapter()
        : this(TypeAdapterConfig.GlobalSettings) { }

    /// <inheritdoc/>
    public TDestination Map<TDestination>(object source)
    {
        return source.Adapt<TDestination>(_config);
    }

    /// <inheritdoc/>
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        return source.Adapt<TDestination>(_config);
    }

    /// <inheritdoc/>
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        return source.Adapt(destination, _config);
    }
}
