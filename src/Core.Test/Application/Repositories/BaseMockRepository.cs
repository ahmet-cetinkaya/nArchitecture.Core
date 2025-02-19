using AutoMapper;
using Moq;
using NArchitecture.Core.Application.Rules;
using NArchitecture.Core.Localization.Resource.Yaml;
using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Test.Application.FakeData;
using NArchitecture.Core.Test.Application.Helpers;

namespace NArchitecture.Core.Test.Application.Repositories;

public abstract class BaseMockRepository<TRepository, TEntity, TEntityId, TMappingProfile, TBusinessRules, TFakeData>
    where TEntity : BaseEntity<TEntityId>, new()
    where TRepository : class, IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    where TMappingProfile : Profile, new()
    where TBusinessRules : BaseBusinessRules
    where TFakeData : BaseFakeData<TEntity, TEntityId>, new()
{
    public IMapper Mapper { get; }
    public Mock<TRepository> MockRepository { get; }
    public TBusinessRules BusinessRules { get; }
    protected TFakeData FakeData { get; }

    protected BaseMockRepository(TFakeData? fakeData = null, string[]? acceptLocales = null)
    {
        FakeData = fakeData ?? new TFakeData();
        MapperConfiguration mapperConfig = new(c => c.AddProfile<TMappingProfile>());
        Mapper = mapperConfig.CreateMapper();

        MockRepository = MockRepositoryHelper.GetRepository<TRepository, TEntity, TEntityId>(FakeData.Data);
        
        var resourceManager = new ResourceLocalizationManager(resources: []) 
        { 
            AcceptLocales = acceptLocales ?? new[] { "en" } 
        };

        BusinessRules = (TBusinessRules)Activator.CreateInstance(
            typeof(TBusinessRules),
            MockRepository.Object,
            resourceManager
        )! ?? throw new InvalidOperationException($"Cannot create an instance of {typeof(TBusinessRules).FullName}.");
    }
}
