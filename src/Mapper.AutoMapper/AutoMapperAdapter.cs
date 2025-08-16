using NArchitecture.Core.Mapper.Abstractions;

namespace NArchitecture.Core.Mapper.AutoMapper;

/// <summary>
/// Provides an adapter implementation for AutoMapper to work with the application's mapping interface.
/// </summary>
public class AutoMapperAdapter(global::AutoMapper.IMapper autoMapper) : IMapper
{
    private readonly global::AutoMapper.IMapper _autoMapper = autoMapper ?? throw new ArgumentNullException(nameof(autoMapper));

    /// <inheritdoc/>
    public TDestination Map<TDestination>(object source)
    {
        return _autoMapper.Map<TDestination>(source);
    }

    /// <inheritdoc/>
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        return _autoMapper.Map<TSource, TDestination>(source);
    }

    /// <inheritdoc/>
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        return _autoMapper.Map(source, destination);
    }
}
