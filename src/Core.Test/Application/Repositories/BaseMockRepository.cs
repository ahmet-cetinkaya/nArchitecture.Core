using AutoMapper;
using Moq;
using NArchitecture.Core.Application.Rules;
using NArchitecture.Core.Localization.Resource.Yaml;
using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Test.Application.FakeData;
using NArchitecture.Core.Test.Application.Helpers;

namespace NArchitecture.Core.Test.Application.Repositories;

/// <summary>
/// Base class for creating mock repositories for testing purposes.
/// </summary>
/// <typeparam name="TRepository">The type of the repository to mock</typeparam>
/// <typeparam name="TEntity">The entity type that the repository manages</typeparam>
/// <typeparam name="TEntityId">The type of the entity's identifier</typeparam>
/// <typeparam name="TMappingProfile">The AutoMapper profile for the entity</typeparam>
/// <typeparam name="TBusinessRules">The business rules type for the entity</typeparam>
/// <typeparam name="TFakeData">The fake data provider type for the entity</typeparam>
public abstract class BaseMockRepository<TRepository, TEntity, TEntityId, TMappingProfile, TBusinessRules, TFakeData>
    where TEntity : BaseEntity<TEntityId>, new()
    where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    where TMappingProfile : Profile, new()
    where TBusinessRules : BaseBusinessRules
    where TFakeData : BaseFakeData<TEntity, TEntityId>, new()
{
    /// <summary>
    /// Gets the configured AutoMapper instance.
    /// </summary>
    public IMapper Mapper { get; }

    /// <summary>
    /// Gets the mock repository instance.
    /// </summary>
    public Mock<TRepository> MockRepository { get; }

    /// <summary>
    /// Gets the business rules instance.
    /// </summary>
    public TBusinessRules BusinessRules { get; }

    /// <summary>
    /// Gets the fake data provider instance.
    /// </summary>
    protected TFakeData FakeData { get; }

    protected BaseMockRepository(TFakeData? fakeData = null, string[]? acceptLocales = null)
    {
        FakeData = fakeData ?? new TFakeData();
        MapperConfiguration mapperConfig = new(c => c.AddProfile<TMappingProfile>());
        Mapper = mapperConfig.CreateMapper();

        MockRepository = MockRepositoryHelper.GetRepository<TRepository, TEntity, TEntityId>(FakeData.Data);

        var resourceManager = new ResourceLocalizationManager(resources: []) { AcceptLocales = acceptLocales ?? new[] { "en" } };

        BusinessRules =
            (TBusinessRules)Activator.CreateInstance(typeof(TBusinessRules), MockRepository.Object, resourceManager)!
            ?? throw new InvalidOperationException($"Cannot create an instance of {typeof(TBusinessRules).FullName}.");
    }
}
