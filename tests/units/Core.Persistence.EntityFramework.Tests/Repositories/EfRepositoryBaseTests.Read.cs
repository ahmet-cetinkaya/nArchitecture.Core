using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Dynamic;
using Shouldly;
using Xunit;

namespace Core.Persistence.EntityFramework.Tests.Repositories;

public partial class EfRepositoryBaseTests
{
    [Theory(DisplayName = "Get/GetAsync - Should return entity by predicate")]
    [Trait("Category", "Read")]
    [Trait("Method", "Get")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_ShouldReturnEntityByPredicate(bool isAsync)
    {
        // Arrange
        var entity = CreateTestEntity();
        _ = await Repository.AddAsync(entity);
        _ = await Repository.SaveChangesAsync();

        // Act
        TestEntity? result;
        if (isAsync)
            result = await Repository.GetAsync(e => e.Id == entity.Id);
        else
            result = Repository.Get(e => e.Id == entity.Id);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Id.ShouldBe(entity.Id);
        result.Name.ShouldBe(entity.Name);
    }

    [Theory(DisplayName = "GetById/GetByIdAsync - Should return entity by ID")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetById")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetById_ShouldReturnEntityById(bool isAsync)
    {
        // Arrange
        var entity = CreateTestEntity();
        _ = await Repository.AddAsync(entity);
        _ = await Repository.SaveChangesAsync();

        // Act
        TestEntity? result;
        if (isAsync)
            result = await Repository.GetByIdAsync(entity.Id);
        else
            result = Repository.GetById(entity.Id);

        // Assert
        _ = result.ShouldNotBeNull();
        result.Id.ShouldBe(entity.Id);
    }

    [Theory(DisplayName = "GetAll/GetAllAsync - Should return all entities")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetAll")]
    [InlineData(5, true)]
    [InlineData(5, false)]
    public async Task GetAll_ShouldReturnAllEntities(int entityCount, bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(entityCount);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act
        ICollection<TestEntity> result;
        if (isAsync)
            result = await Repository.GetAllAsync();
        else
            result = Repository.GetAll();

        // Assert
        result.Count.ShouldBe(entityCount);
        foreach (var entity in entities)
        {
            result.ShouldContain(e => e.Id == entity.Id);
        }
    }

    [Theory(DisplayName = "GetAll/GetAllAsync - Should respect predicate filter")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetAll")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAll_ShouldRespectPredicateFilter(bool isAsync)
    {
        // Arrange
        var entities = new[] { CreateTestEntity("Match"), CreateTestEntity("Match"), CreateTestEntity("NoMatch") };
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act
        ICollection<TestEntity> result;
        if (isAsync)
            result = await Repository.GetAllAsync(e => e.Name.StartsWith("Match"));
        else
            result = Repository.GetAll(e => e.Name.StartsWith("Match"));

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldAllBe(e => e.Name.StartsWith("Match"));
    }

    [Theory(DisplayName = "GetList/GetListAsync - Should return paginated results")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetList")]
    [InlineData(10, 0, 5, true)]
    [InlineData(10, 0, 5, false)]
    [InlineData(10, 1, 3, true)]
    [InlineData(10, 1, 3, false)]
    public async Task GetList_ShouldReturnPaginatedResults(int totalCount, int index, int size, bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(totalCount);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act
        var result = isAsync
            ? await Repository.GetListAsync(index: index, size: size)
            : Repository.GetList(index: index, size: size);

        // Assert
        result.Index.ShouldBe(index);
        result.Size.ShouldBe(size);
        result.Count.ShouldBe(totalCount);
        result.Items.Count.ShouldBe(Math.Min(size, totalCount - (index * size)));
    }

    [Theory(DisplayName = "Any/AnyAsync - Should check existence correctly")]
    [Trait("Category", "Read")]
    [Trait("Method", "Any")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Any_ShouldCheckExistenceCorrectly(bool isAsync)
    {
        // Arrange
        var entity = CreateTestEntity("Unique");
        _ = await Repository.AddAsync(entity);
        _ = await Repository.SaveChangesAsync();

        // Act
        bool exists;
        if (isAsync)
            exists = await Repository.AnyAsync(e => e.Name == "Unique");
        else
            exists = Repository.Any(e => e.Name == "Unique");

        bool notExists;
        if (isAsync)
            notExists = await Repository.AnyAsync(e => e.Name == "NonExistent");
        else
            notExists = Repository.Any(e => e.Name == "NonExistent");

        // Assert
        exists.ShouldBeTrue();
        notExists.ShouldBeFalse();
    }

    [Theory(DisplayName = "Count/CountAsync - Should return correct count")]
    [Trait("Category", "Read")]
    [Trait("Method", "Count")]
    [InlineData(5, true)]
    [InlineData(5, false)]
    [InlineData(0, true)]
    [InlineData(0, false)]
    public async Task Count_ShouldReturnCorrectCount(int entityCount, bool isAsync)
    {
        // Arrange
        if (entityCount > 0)
        {
            var entities = CreateTestEntities(entityCount);
            await Repository.BulkAddAsync(entities);
            await Repository.SaveChangesAsync();
        }

        // Act
        int count;
        if (isAsync)
            count = await Repository.CountAsync();
        else
            count = Repository.Count();

        // Assert
        count.ShouldBe(entityCount);
    }

    [Theory(DisplayName = "GetListByDynamic - Should apply dynamic filters")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetListByDynamic")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetListByDynamic_ShouldApplyDynamicFilters(bool isAsync)
    {
        // Arrange
        var entities = new[]
        {
            CreateTestEntity("High Priority"),
            CreateTestEntity("Medium Priority"),
            CreateTestEntity("Low Priority"),
        };
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        var dynamicQuery = new DynamicQuery
        {
            Filter = new Filter("name", "contains") { Value = "Priority", Logic = "and" },
        };

        // Act
        var result = isAsync
            ? await Repository.GetListByDynamicAsync(dynamicQuery, index: 0, size: 10)
            : Repository.GetListByDynamic(dynamicQuery, index: 0, size: 10);

        // Assert
        result.Items.Count.ShouldBe(3);
        result.Items.ShouldAllBe(e => e.Name.Contains("Priority"));
    }

    [Theory(DisplayName = "ExistsById - Should check existence by Id")]
    [Trait("Category", "Read")]
    [Trait("Method", "ExistsById")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistsById_ShouldCheckExistenceById(bool isAsync)
    {
        // Arrange
        var entity = CreateTestEntity();
        _ = await Repository.AddAsync(entity);
        _ = await Repository.SaveChangesAsync();

        // Act
        bool exists = isAsync ? await Repository.ExistsByIdAsync(entity.Id) : Repository.ExistsById(entity.Id);

        bool notExists = isAsync ? await Repository.ExistsByIdAsync(Guid.NewGuid()) : Repository.ExistsById(Guid.NewGuid());

        // Assert
        exists.ShouldBeTrue();
        notExists.ShouldBeFalse();
    }

    [Theory(DisplayName = "GetRandom - Should return random entity")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetRandom")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetRandom_ShouldReturnRandomEntity(bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(5);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act
        var result = isAsync ? await Repository.GetRandomAsync() : Repository.GetRandom();

        // Assert
        result.ShouldNotBeNull();
        entities.ShouldContain(e => e.Id == result.Id);
    }

    [Theory(DisplayName = "GetRandom - Should respect predicate")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetRandom")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetRandom_ShouldRespectPredicate(bool isAsync)
    {
        // Arrange
        var entities = new[] { CreateTestEntity("Target"), CreateTestEntity("Target"), CreateTestEntity("NonTarget") };
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act
        var result = isAsync
            ? await Repository.GetRandomAsync(e => e.Name == "Target")
            : Repository.GetRandom(e => e.Name == "Target");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Target");
    }

    [Theory(DisplayName = "GetRandomList - Should return random entities")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetRandomList")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetRandomList_ShouldReturnRandomEntities(bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(10);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act
        var result = isAsync ? await Repository.GetRandomListAsync(size: 5) : Repository.GetRandomList(size: 5);

        // Assert
        result.Items.Count.ShouldBe(5);
        result.Items.ShouldAllBe(item => entities.Any(e => e.Id == item.Id));
    }

    [Theory(DisplayName = "CountLong - Should return correct count as long")]
    [Trait("Category", "Read")]
    [Trait("Method", "CountLong")]
    [InlineData(5, true)]
    [InlineData(5, false)]
    [InlineData(0, true)]
    [InlineData(0, false)]
    public async Task CountLong_ShouldReturnCorrectCount(int entityCount, bool isAsync)
    {
        // Arrange
        if (entityCount > 0)
        {
            var entities = CreateTestEntities(entityCount);
            await Repository.BulkAddAsync(entities);
            await Repository.SaveChangesAsync();
        }

        // Act
        long count = isAsync ? await Repository.CountLongAsync() : Repository.CountLong();

        // Assert
        count.ShouldBe(entityCount);
    }

    [Theory(DisplayName = "Count - Should respect predicate")]
    [Trait("Category", "Read")]
    [Trait("Method", "Count")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Count_ShouldRespectPredicate(bool isAsync)
    {
        // Arrange
        var entities = new[] { CreateTestEntity("Match"), CreateTestEntity("Match"), CreateTestEntity("NoMatch") };
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act
        int count = isAsync ? await Repository.CountAsync(e => e.Name == "Match") : Repository.Count(e => e.Name == "Match");

        // Assert
        count.ShouldBe(2);
    }

    [Theory(DisplayName = "GetList - Should throw on invalid pagination parameters")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetList")]
    [InlineData(-1, 10)] // Negative index
    [InlineData(0, 0)] // Zero size
    [InlineData(0, -1)] // Negative size
    [InlineData(int.MaxValue, 10)] // Max index
    [InlineData(0, int.MaxValue)] // Max size
    public void GetList_ShouldThrowOnInvalidPaginationParameters(int index, int size)
    {
        Should.Throw<ArgumentException>(() => Repository.GetList(index: index, size: size));
    }

    [Theory(DisplayName = "GetListByDynamic - Should validate operator")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetListByDynamic")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetListByDynamic_ShouldValidateOperator(bool isAsync)
    {
        // Arrange
        var invalidDynamic = new DynamicQuery
        {
            Filter = new("name", "invalidOperator") { Value = "someValue", Logic = "and" },
        };

        // Act & Assert
        var exception = isAsync
            ? await Should.ThrowAsync<ArgumentException>(async () => await Repository.GetListByDynamicAsync(invalidDynamic))
            : Should.Throw<ArgumentException>(() => Repository.GetListByDynamic(invalidDynamic));

        exception.Message.ShouldBe("Invalid Operator");
    }

    [Theory(DisplayName = "GetListByDynamic - Should handle null query")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetListByDynamic")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetListByDynamic_ShouldHandleNullQuery(bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(3);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act & Assert
        if (isAsync)
            await Should.NotThrowAsync(async () => await Repository.GetListByDynamicAsync(new DynamicQuery()));
        else
            Should.NotThrow(() => Repository.GetListByDynamic(new DynamicQuery()));
    }

    [Theory(DisplayName = "GetListByDynamic - Should handle null filter")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetListByDynamic")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetListByDynamic_ShouldHandleNullFilter(bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(3);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        var dynamicQuery = new DynamicQuery { Filter = null };

        // Act & Assert
        if (isAsync)
            await Should.NotThrowAsync(async () => await Repository.GetListByDynamicAsync(dynamicQuery));
        else
            Should.NotThrow(() => Repository.GetListByDynamic(dynamicQuery));
    }

    [Theory(DisplayName = "GetListByDynamic - Should throw ParseException for nonexistent field")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetListByDynamic")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetListByDynamic_ShouldThrowParseExceptionForNonexistentField(bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(3);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        var dynamicQuery = new DynamicQuery
        {
            Filter = new("nonexistentField", "eq") { Value = "someValue", Logic = "and" },
        };

        // Act & Assert
        var exception = isAsync
            ? await Should.ThrowAsync<System.Linq.Dynamic.Core.Exceptions.ParseException>(
                async () => await Repository.GetListByDynamicAsync(dynamicQuery)
            )
            : Should.Throw<System.Linq.Dynamic.Core.Exceptions.ParseException>(() => Repository.GetListByDynamic(dynamicQuery));

        exception.Message.ShouldBe("No property or field 'nonexistentField' exists in type 'TestEntity'");
    }

    [Theory(DisplayName = "GetAll - Should throw when fetch limit exceeded")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetAll")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAll_ShouldThrowWhenFetchLimitExceeded(bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(5);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act & Assert
        if (isAsync)
            await Should.ThrowAsync<InvalidOperationException>(async () => await Repository.GetAllAsync(fetchLimit: 1));
        else
            Should.Throw<InvalidOperationException>(() => Repository.GetAll(fetchLimit: 1));
    }

    [Theory(DisplayName = "GetAll - Should throw on invalid chunk size")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetAll")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetAll_ShouldThrowOnInvalidChunkSize(int chunkSize)
    {
        await Should.ThrowAsync<ArgumentException>(async () => await Repository.GetAllAsync(chunkSize: chunkSize));
    }

    [Theory(DisplayName = "GetRandomList - Should handle invalid size")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetRandomList")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task GetRandomList_ShouldHandleInvalidSize(int size)
    {
        await Should.ThrowAsync<ArgumentException>(async () => await Repository.GetRandomListAsync(size: size));
    }

    [Fact(DisplayName = "GetById - Should handle invalid id")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetById")]
    public async Task GetById_ShouldHandleInvalidId()
    {
        // Act
        var result = await Repository.GetByIdAsync(default);

        // Assert
        result.ShouldBeNull();
    }

    [Theory(DisplayName = "Get - Should handle null predicate")]
    [Trait("Category", "Read")]
    [Trait("Method", "Get")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_ShouldHandleNullPredicate(bool isAsync)
    {
        // Arrange
        Expression<Func<TestEntity, bool>>? nullPredicate = null;

        // Act & Assert
        if (isAsync)
            await Should.ThrowAsync<ArgumentNullException>(async () => await Repository.GetAsync(nullPredicate!));
        else
            Should.Throw<ArgumentNullException>(() => Repository.Get(nullPredicate!));
    }

    [Theory(DisplayName = "Count - Should handle null predicate")]
    [Trait("Category", "Read")]
    [Trait("Method", "Count")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Count_ShouldHandleNullPredicate(bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(3);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act
        var count = isAsync ? await Repository.CountAsync(predicate: null) : Repository.Count(predicate: null);

        // Assert
        count.ShouldBe(3);
    }

    [Theory(DisplayName = "Get - Should respect tracking behavior")]
    [Trait("Category", "Read")]
    [Trait("Method", "Get")]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Get_ShouldRespectTrackingBehavior(bool isAsync, bool enableTracking)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();

        // Act
        TestEntity? result;
        if (isAsync)
            result = await Repository.GetAsync(e => e.Id == entity.Id, enableTracking: enableTracking);
        else
            result = Repository.Get(e => e.Id == entity.Id, enableTracking: enableTracking);

        // Assert
        result.ShouldNotBeNull();
        var entry = Context.Entry(result);
        if (enableTracking)
            entry.State.ShouldBe(EntityState.Unchanged);
        else
            entry.State.ShouldBe(EntityState.Detached);
    }

    [Theory(DisplayName = "GetAll - Should include related entities")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetAll")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAll_ShouldIncludeRelatedEntities(bool isAsync)
    {
        // Arrange
        var parent = await CreateAndAddTestEntity();
        var child = CreateTestEntity();
        child.ParentId = parent.Id;
        await Repository.AddAsync(child);
        await Repository.SaveChangesAsync();

        // Act
        ICollection<TestEntity> result;
        if (isAsync)
            result = await Repository.GetAllAsync(include: q => q.Include(e => e.Parent));
        else
            result = Repository.GetAll(include: q => q.Include(e => e.Parent));

        // Assert
        var childEntity = result.First(e => e.ParentId != null);
        childEntity.Parent.ShouldNotBeNull();
        childEntity.Parent.Id.ShouldBe(parent.Id);
    }

    [Theory(DisplayName = "GetList - Should respect order by parameter")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetList")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetList_ShouldRespectOrderByParameter(bool isAsync)
    {
        // Arrange
        var entities = new[] { CreateTestEntity("A"), CreateTestEntity("C"), CreateTestEntity("B") };
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act
        var result = isAsync
            ? await Repository.GetListAsync(orderBy: q => q.OrderBy(e => e.Name))
            : Repository.GetList(orderBy: q => q.OrderBy(e => e.Name));

        // Assert
        result.Items.Select(e => e.Name).ShouldBe(new[] { "A", "B", "C" }, ignoreOrder: false);
    }

    [Theory(DisplayName = "GetAll - Should respect withDeleted parameter")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetAll")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAll_ShouldRespectWithDeletedParameter(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        await Repository.DeleteAsync(entity);
        await Repository.SaveChangesAsync();

        // Act
        ICollection<TestEntity> withoutDeleted;
        ICollection<TestEntity> withDeleted;

        if (isAsync)
        {
            withoutDeleted = await Repository.GetAllAsync(withDeleted: false);
            withDeleted = await Repository.GetAllAsync(withDeleted: true);
        }
        else
        {
            withoutDeleted = Repository.GetAll(withDeleted: false);
            withDeleted = Repository.GetAll(withDeleted: true);
        }

        // Assert
        withoutDeleted.Count.ShouldBe(0);
        withDeleted.Count.ShouldBe(1);
    }

    [Theory(DisplayName = "GetListByDynamic - Should handle sort parameters")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetListByDynamic")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetListByDynamic_ShouldHandleSortParameters(bool isAsync)
    {
        // Arrange
        var entities = new[] { CreateTestEntity("A"), CreateTestEntity("C"), CreateTestEntity("B") };
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        var dynamicQuery = new DynamicQuery { Sort = new[] { new Sort("name", "asc") } };

        // Act
        var result = isAsync ? await Repository.GetListByDynamicAsync(dynamicQuery) : Repository.GetListByDynamic(dynamicQuery);

        // Assert
        result.Items.Select(e => e.Name).ShouldBe(new[] { "A", "B", "C" }, ignoreOrder: false);
    }

    [Theory(DisplayName = "GetList - Should handle last page correctly")]
    [Trait("Category", "Read")]
    [Trait("Method", "GetList")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetList_ShouldHandleLastPageCorrectly(bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(10);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act - Request last page
        var result = isAsync ? await Repository.GetListAsync(index: 3, size: 3) : Repository.GetList(index: 3, size: 3);

        // Assert
        result.Index.ShouldBe(3);
        result.Size.ShouldBe(3);
        result.Count.ShouldBe(10); // Total count
        result.Pages.ShouldBe(4); // Ceil(10/3)
        result.Items.Count.ShouldBe(1); // Last page should have 1 item
        result.HasNext.ShouldBeFalse();
        result.HasPrevious.ShouldBeTrue();
    }
}
