using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Core.Persistence.EntityFramework.Tests.Repositories;

public partial class EfRepositoryBaseTests
{
    [Theory(DisplayName = "Update/UpdateAsync - Should update entity properties and set update date")]
    [Trait("Category", "Update")]
    [Trait("Method", "Update")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_ShouldUpdateEntityPropertiesAndSetUpdateDate(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        var originalCreatedDate = entity.CreatedAt;
        var beforeUpdate = DateTime.UtcNow;

        entity.Name = "Updated Name";
        entity.Description = "Updated Description";

        // Act
        if (isAsync)
            _ = await Repository.UpdateAsync(entity);
        else
            _ = Repository.Update(entity);
        _ = await Repository.SaveChangesAsync();

        // Assert
        var updatedEntity = await Context.TestEntities.FindAsync(entity.Id);
        _ = updatedEntity.ShouldNotBeNull();
        updatedEntity.Name.ShouldBe("Updated Name");
        updatedEntity.Description.ShouldBe("Updated Description");
        updatedEntity.CreatedAt.ShouldBe(originalCreatedDate);
        _ = updatedEntity.UpdatedAt.ShouldNotBeNull();
        updatedEntity.UpdatedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
        updatedEntity.UpdatedAt!.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Theory(DisplayName = "Update/UpdateAsync - Should throw when entity is null")]
    [Trait("Category", "Update")]
    [Trait("Method", "Update")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_ShouldThrowWhenEntityIsNull(bool isAsync)
    {
        // Act & Assert
        if (isAsync)
            _ = await Should.ThrowAsync<ArgumentNullException>(async () => await Repository.UpdateAsync(null!));
        else
            _ = Should.Throw<ArgumentNullException>(() => Repository.Update(null!));
    }

    [Theory(DisplayName = "Update/UpdateAsync - Should handle entity with relationships")]
    [Trait("Category", "Update")]
    [Trait("Method", "Update")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_ShouldHandleEntityWithRelationships(bool isAsync)
    {
        // Arrange
        var parent = await CreateAndAddTestEntity();
        var child = CreateTestEntity();
        child.ParentId = parent.Id;
        _ = await Repository.AddAsync(child);
        _ = await Repository.SaveChangesAsync();

        child.Name = "Updated Child";
        var beforeUpdate = DateTime.UtcNow;

        // Act
        if (isAsync)
            _ = await Repository.UpdateAsync(child);
        else
            _ = Repository.Update(child);
        _ = await Repository.SaveChangesAsync();

        // Assert
        var updatedChild = await Context.TestEntities.Include(e => e.Parent).FirstOrDefaultAsync(e => e.Id == child.Id);

        _ = updatedChild.ShouldNotBeNull();
        updatedChild.Name.ShouldBe("Updated Child");
        updatedChild.ParentId.ShouldBe(parent.Id);
        _ = updatedChild.Parent.ShouldNotBeNull();
        _ = updatedChild.UpdatedAt.ShouldNotBeNull();
        updatedChild.UpdatedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
    }

    [Theory(DisplayName = "BulkUpdate - Should update multiple entities")]
    [Trait("Category", "Update")]
    [Trait("Method", "BulkUpdate")]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [InlineData(5, true)]
    [InlineData(5, false)]
    public async Task BulkUpdate_ShouldUpdateMultipleEntities(int entityCount, bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(entityCount);
        _ = await Repository.BulkAddAsync(entities);
        _ = await Repository.SaveChangesAsync();
        var beforeUpdate = DateTime.UtcNow;

        foreach (var entity in entities)
        {
            entity.Name = $"Updated {entity.Name}";
            entity.Description = $"Updated {entity.Description}";
        }

        // Act
        if (isAsync)
        {
            _ = await Repository.BulkUpdateAsync(entities);
            _ = await Repository.SaveChangesAsync();
        }
        else
        {
            _ = Repository.BulkUpdate(entities);
            _ = Repository.SaveChanges();
        }

        // Assert
        var updatedEntities = await Context.TestEntities.ToListAsync();
        updatedEntities.Count.ShouldBe(entityCount);
        foreach (var entity in updatedEntities)
        {
            entity.Name.ShouldStartWith("Updated");
            entity.Description.ShouldStartWith("Updated");
            _ = entity.UpdatedAt.ShouldNotBeNull();
            entity.UpdatedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
        }
    }

    [Theory(DisplayName = "BulkUpdate - Should throw when entities collection is null")]
    [Trait("Category", "Update")]
    [Trait("Method", "BulkUpdate")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BulkUpdate_ShouldThrowWhenCollectionIsNull(bool isAsync)
    {
        // Act & Assert
        if (isAsync)
            _ = await Should.ThrowAsync<ArgumentNullException>(async () => await Repository.BulkUpdateAsync(null!));
        else
            _ = Should.Throw<ArgumentNullException>(() => Repository.BulkUpdate(null!));
    }

    [Theory]
    [Trait("Category", "Update")]
    [Trait("Method", "BulkUpdate")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BulkUpdate_ShouldHandleEmptyCollection(bool isAsync)
    {
        // Arrange
        var entities = new List<TestEntity>();

        // Act & Assert
        if (isAsync)
            await Should.NotThrowAsync(async () => await Repository.BulkUpdateAsync(entities));
        else
            _ = Should.NotThrow(() => Repository.BulkUpdate(entities));
    }

    private async Task<TestEntity> CreateAndAddTestEntity()
    {
        var entity = CreateTestEntity();
        _ = await Repository.AddAsync(entity);
        _ = await Repository.SaveChangesAsync();
        return entity;
    }
}
