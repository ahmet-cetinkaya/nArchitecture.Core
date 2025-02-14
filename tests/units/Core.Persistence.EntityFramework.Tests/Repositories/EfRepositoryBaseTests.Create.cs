using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Core.Persistence.EntityFramework.Tests.Repositories;

public partial class EfRepositoryBaseTests
{
    [Theory(DisplayName = "Add/AddAsync - Should add entity and set creation date")]
    [Trait("Category", "Create")]
    [Trait("Method", "Add")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Add_ShouldAddEntityAndSetCreationDate(bool isAsync)
    {
        // Arrange
        var entity = CreateTestEntity();
        var beforeAdd = DateTime.UtcNow;

        // Act
        if (isAsync)
        {
            _ = await Repository.AddAsync(entity);
            _ = await Repository.SaveChangesAsync();
        }
        else
        {
            _ = Repository.Add(entity);
            _ = Repository.SaveChanges();
        }

        // Assert
        var dbEntity = await Context.TestEntities.FindAsync(entity.Id);
        _ = dbEntity.ShouldNotBeNull();
        dbEntity.Name.ShouldBe(entity.Name);
        dbEntity.Description.ShouldBe(entity.Description);
        dbEntity.CreatedDate.ShouldBeGreaterThanOrEqualTo(beforeAdd);
        dbEntity.CreatedDate.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Theory(DisplayName = "Add/AddAsync - Should throw when entity is null")]
    [Trait("Category", "Create")]
    [Trait("Method", "Add")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Add_ShouldThrowWhenEntityIsNull(bool isAsync)
    {
        // Arrange & Act & Assert
        if (isAsync)
            _ = await Should.ThrowAsync<ArgumentNullException>(async () => await Repository.AddAsync(null!));
        else
            _ = Should.Throw<ArgumentNullException>(() => Repository.Add(null!));
    }

    [Theory(DisplayName = "BulkAdd/BulkAddAsync - Should add multiple entities")]
    [Trait("Category", "Create")]
    [Trait("Method", "BulkAdd")]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [InlineData(5, true)]
    [InlineData(5, false)]
    [InlineData(10, true)]
    [InlineData(10, false)]
    public async Task BulkAdd_ShouldAddMultipleEntities(int entityCount, bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(entityCount);
        var beforeAdd = DateTime.UtcNow;

        // Act
        if (isAsync)
        {
            await Repository.BulkAddAsync(entities);
            await Repository.SaveChangesAsync();
        }
        else
        {
            Repository.BulkAdd(entities);
            Repository.SaveChanges();
        }

        // Assert
        var dbEntities = await Context.TestEntities.ToListAsync();
        dbEntities.Count.ShouldBe(entityCount);
        foreach (var entity in dbEntities)
        {
            entity.CreatedDate.ShouldBeGreaterThanOrEqualTo(beforeAdd);
            entity.CreatedDate.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        }
    }

    [Theory(DisplayName = "BulkAdd/BulkAddAsync - Should handle empty collection")]
    [Trait("Category", "Create")]
    [Trait("Method", "BulkAdd")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BulkAdd_ShouldHandleEmptyCollection(bool isAsync)
    {
        // Arrange
        var entities = new List<TestEntity>();

        // Act & Assert
        if (isAsync)
            await Should.NotThrowAsync(async () => await Repository.BulkAddAsync(entities));
        else
            Should.NotThrow(() => Repository.BulkAdd(entities));
    }

    [Theory(DisplayName = "BulkAdd/BulkAddAsync - Should throw when collection is null")]
    [Trait("Category", "Create")]
    [Trait("Method", "BulkAdd")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BulkAdd_ShouldThrowWhenCollectionIsNull(bool isAsync)
    {
        // Arrange & Act & Assert
        if (isAsync)
            _ = await Should.ThrowAsync<ArgumentNullException>(async () => await Repository.BulkAddAsync(null!));
        else
            _ = Should.Throw<ArgumentNullException>(() => Repository.BulkAdd(null!));
    }
}
