using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Core.Persistence.EntityFramework.Tests.Repositories;

public partial class EfRepositoryBaseTests
{
    [Theory(DisplayName = "Update/UpdateAsync - Should detect concurrent modification when another user updates the same entity")]
    [Trait("Category", "Concurrency")]
    [Trait("Method", "Update")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_ShouldDetectConcurrentModification(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        var entityFromAnotherContext = await SimulateAnotherUserModifyingEntity(entity.Id);
        var expectedMessage =
            $"The entity with id {entity.Id} has been modified by another user. Please reload the entity and try again.";

        // Act & Assert
        var exception = isAsync
            ? await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
            {
                entity.Name = "Updated by first user";
                _ = await Repository.UpdateAsync(entity);
                _ = await Repository.SaveChangesAsync();
            })
            : Should.Throw<DbUpdateConcurrencyException>(() =>
            {
                entity.Name = "Updated by first user";
                _ = Repository.Update(entity);
                _ = Repository.SaveChanges();
            });

        exception.Message.ShouldBe(expectedMessage);
    }

    [Theory(DisplayName = "Update/UpdateAsync - Should detect when entity is deleted by another user")]
    [Trait("Category", "Concurrency")]
    [Trait("Method", "Update")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_ShouldDetectWhenEntityIsDeleted(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        await SimulateAnotherUserDeletingEntity(entity.Id);
        var expectedMessage = $"The entity with id {entity.Id} has been deleted by another user.";

        // Act & Assert
        var exception = isAsync
            ? await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                entity.Name = "Updated by first user";
                _ = await Repository.UpdateAsync(entity);
                _ = await Repository.SaveChangesAsync();
            })
            : Should.Throw<InvalidOperationException>(() =>
            {
                entity.Name = "Updated by first user";
                _ = Repository.Update(entity);
                _ = Repository.SaveChanges();
            });

        exception.Message.ShouldBe(expectedMessage);
    }

    [Theory(DisplayName = "Update/UpdateAsync - Should detect when entity is permanently deleted")]
    [Trait("Category", "Concurrency")]
    [Trait("Method", "Update")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_ShouldDetectWhenEntityIsPermanentlyDeleted(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        await SimulateAnotherUserPermanentlyDeletingEntity(entity.Id);
        var expectedMessage = $"The entity with id {entity.Id} no longer exists in the database.";

        // Act & Assert
        var exception = isAsync
            ? await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                entity.Name = "Updated by first user";
                _ = await Repository.UpdateAsync(entity);
                _ = await Repository.SaveChangesAsync();
            })
            : Should.Throw<InvalidOperationException>(() =>
            {
                entity.Name = "Updated by first user";
                _ = Repository.Update(entity);
                _ = Repository.SaveChanges();
            });

        exception.Message.ShouldBe(expectedMessage);
    }

    [Theory(DisplayName = "Update/UpdateAsync - Should handle concurrent modifications in related entities")]
    [Trait("Category", "Concurrency")]
    [Trait("Method", "Update")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_ShouldHandleConcurrentModificationsInRelatedEntities(bool isAsync)
    {
        // Arrange
        var parent = await CreateAndAddTestEntity();
        var child = CreateTestEntity();
        child.ParentId = parent.Id;
        _ = await Repository.AddAsync(child);
        _ = await Repository.SaveChangesAsync();

        _ = await SimulateAnotherUserModifyingEntity(child.Id);

        // Act & Assert
        if (isAsync)
            _ = await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
            {
                child.Name = "Updated by first user";
                parent.Name = "Updated parent";
                _ = await Repository.UpdateAsync(child);
                _ = await Repository.UpdateAsync(parent);
                _ = await Repository.SaveChangesAsync();
            });
        else
            _ = Should.Throw<DbUpdateConcurrencyException>(() =>
            {
                child.Name = "Updated by first user";
                parent.Name = "Updated parent";
                _ = Repository.Update(child);
                _ = Repository.Update(parent);
                _ = Repository.SaveChanges();
            });
    }

    [Theory(DisplayName = "Delete/DeleteAsync - Should detect concurrent modification when deleting")]
    [Trait("Category", "Concurrency")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldDetectConcurrentModification(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        _ = await SimulateAnotherUserModifyingEntity(entity.Id);

        // Act & Assert
        if (isAsync)
            _ = await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
            {
                _ = await Repository.DeleteAsync(entity);
                _ = await Repository.SaveChangesAsync();
            });
        else
            _ = Should.Throw<DbUpdateConcurrencyException>(() =>
            {
                _ = Repository.Delete(entity);
                _ = Repository.SaveChanges();
            });
    }

    [Theory(DisplayName = "Delete/DeleteAsync - Should handle already deleted entity")]
    [Trait("Category", "Concurrency")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldHandleAlreadyDeletedEntity(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        await SimulateAnotherUserDeletingEntity(entity.Id);

        // Act & Assert
        if (isAsync)
            _ = await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                _ = await Repository.DeleteAsync(entity);
                _ = await Repository.SaveChangesAsync();
            });
        else
            _ = Should.Throw<InvalidOperationException>(() =>
            {
                _ = Repository.Delete(entity);
                _ = Repository.SaveChanges();
            });
    }

    [Theory(DisplayName = "BulkUpdate/BulkUpdateAsync - Should detect concurrent modifications")]
    [Trait("Category", "Concurrency")]
    [Trait("Method", "BulkUpdate")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BulkUpdate_ShouldDetectConcurrentModifications(bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(3);
        _ = await Repository.BulkAddAsync(entities);
        _ = await Repository.SaveChangesAsync();

        _ = await SimulateAnotherUserModifyingEntity(entities.First().Id);

        // Act & Assert
        if (isAsync)
            _ = await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
            {
                foreach (var entity in entities)
                    entity.Name = $"Updated {entity.Name}";
                _ = await Repository.BulkUpdateAsync(entities);
                _ = await Repository.SaveChangesAsync();
            });
        else
            _ = Should.Throw<DbUpdateConcurrencyException>(() =>
            {
                foreach (var entity in entities)
                    entity.Name = $"Updated {entity.Name}";
                _ = Repository.BulkUpdate(entities);
                _ = Repository.SaveChanges();
            });
    }

    private async Task<TestEntity> SimulateAnotherUserModifyingEntity(Guid entityId)
    {
        using var anotherContext = CreateNewContext();
        var anotherRepository = new TestEntityRepository(anotherContext);

        var entityFromAnotherContext = await anotherContext.TestEntities.FindAsync(entityId);
        entityFromAnotherContext!.Name = "Updated by another user";
        _ = anotherRepository.Update(entityFromAnotherContext);
        _ = await anotherRepository.SaveChangesAsync();

        // Clear change tracker to ensure fresh state
        Context.ChangeTracker.Clear();
        return entityFromAnotherContext;
    }

    private async Task SimulateAnotherUserDeletingEntity(Guid entityId)
    {
        using var anotherContext = CreateNewContext();
        var anotherRepository = new TestEntityRepository(anotherContext);

        var entityFromAnotherContext = await anotherContext.TestEntities.FindAsync(entityId);
        _ = anotherRepository.Delete(entityFromAnotherContext!);
        _ = await anotherRepository.SaveChangesAsync();

        // Clear change tracker to ensure fresh state
        Context.ChangeTracker.Clear();
    }

    private async Task SimulateAnotherUserPermanentlyDeletingEntity(Guid entityId)
    {
        using var anotherContext = CreateNewContext();
        var anotherRepository = new TestEntityRepository(anotherContext);

        var entityFromAnotherContext = await anotherContext.TestEntities.FindAsync(entityId);
        _ = anotherRepository.Delete(entityFromAnotherContext!, permanent: true);
        _ = await anotherRepository.SaveChangesAsync();

        // Clear change tracker to ensure fresh state
        Context.ChangeTracker.Clear();
    }

    private TestDbContext CreateNewContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;
        return new TestDbContext(options);
    }
}
