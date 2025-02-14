using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Persistence.EntityFramework.Repositories;
using Shouldly;
using Xunit;

namespace Core.Persistence.EntityFramework.Tests.Repositories;

public partial class EfRepositoryBaseTests
{
    [Theory(DisplayName = "Delete/DeleteAsync - Should soft delete entity by default")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldSoftDeleteEntityByDefault(bool isAsync)
    {
        // Arrange
        var entity = CreateTestEntity();
        _ = await Repository.AddAsync(entity);
        _ = await Repository.SaveChangesAsync();
        var beforeDelete = DateTime.UtcNow;

        // Act
        if (isAsync)
        {
            _ = await Repository.DeleteAsync(entity);
            _ = await Repository.SaveChangesAsync();
        }
        else
        {
            _ = Repository.Delete(entity);
            _ = Repository.SaveChanges();
        }

        // Assert
        var dbEntity = await Repository.GetByIdAsync(entity.Id, withDeleted: true);
        _ = dbEntity.ShouldNotBeNull();
        dbEntity.DeletedDate.ShouldNotBeNull();
        dbEntity.DeletedDate.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
        dbEntity.DeletedDate.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Theory(DisplayName = "Delete/DeleteAsync - Should permanent delete entity when specified")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldPermanentDeleteEntityWhenSpecified(bool isAsync)
    {
        // Arrange
        var entity = CreateTestEntity();
        _ = await Repository.AddAsync(entity);
        _ = await Repository.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            _ = await Repository.DeleteAsync(entity, permanent: true);
            _ = await Repository.SaveChangesAsync();
        }
        else
        {
            _ = Repository.Delete(entity, permanent: true);
            _ = Repository.SaveChanges();
        }

        // Assert
        var dbEntity = await Repository.GetByIdAsync(entity.Id, withDeleted: true);
        dbEntity.ShouldBeNull();
    }

    [Theory(DisplayName = "BulkDelete/BulkDeleteAsync - Should soft delete multiple entities by default")]
    [Trait("Category", "Delete")]
    [Trait("Method", "BulkDelete")]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [InlineData(5, true)]
    [InlineData(5, false)]
    public async Task BulkDelete_ShouldSoftDeleteMultipleEntities(int entityCount, bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(entityCount);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();
        var beforeDelete = DateTime.UtcNow;

        // Act
        if (isAsync)
        {
            await Repository.BulkDeleteAsync(entities);
            await Repository.SaveChangesAsync();
        }
        else
        {
            Repository.BulkDelete(entities);
            Repository.SaveChanges();
        }

        // Assert
        var dbEntities = await Repository.GetAllAsync(withDeleted: true);
        dbEntities.Count.ShouldBe(entityCount);
        foreach (var entity in dbEntities)
        {
            entity.DeletedDate.ShouldNotBeNull();
            entity.DeletedDate.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
            entity.DeletedDate.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
        }
    }

    [Theory(DisplayName = "BulkDelete/BulkDeleteAsync - Should permanent delete multiple entities when specified")]
    [Trait("Category", "Delete")]
    [Trait("Method", "BulkDelete")]
    [InlineData(1, true)]
    [InlineData(1, false)]
    [InlineData(5, true)]
    [InlineData(5, false)]
    public async Task BulkDelete_ShouldPermanentDeleteMultipleEntitiesWhenSpecified(int entityCount, bool isAsync)
    {
        // Arrange
        var entities = CreateTestEntities(entityCount);
        await Repository.BulkAddAsync(entities);
        await Repository.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            await Repository.BulkDeleteAsync(entities, permanent: true);
            await Repository.SaveChangesAsync();
        }
        else
        {
            Repository.BulkDelete(entities, permanent: true);
            Repository.SaveChanges();
        }

        // Assert
        var dbEntities = await Repository.GetAllAsync(withDeleted: true);
        dbEntities.Count.ShouldBe(0);
    }

    [Theory(DisplayName = "Delete/DeleteAsync - Should throw when entity is null")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldThrowWhenEntityIsNull(bool isAsync)
    {
        // Arrange & Act & Assert
        if (isAsync)
            await Should.ThrowAsync<ArgumentNullException>(async () => await Repository.DeleteAsync(null!));
        else
            Should.Throw<ArgumentNullException>(() => Repository.Delete(null!));
    }

    [Theory(DisplayName = "BulkDelete/BulkDeleteAsync - Should handle empty collection")]
    [Trait("Category", "Delete")]
    [Trait("Method", "BulkDelete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BulkDelete_ShouldHandleEmptyCollection(bool isAsync)
    {
        // Arrange
        var entities = new List<TestEntity>();

        // Act & Assert
        if (isAsync)
            await Should.NotThrowAsync(async () => await Repository.BulkDeleteAsync(entities));
        else
            Should.NotThrow(() => Repository.BulkDelete(entities));
    }

    [Theory(DisplayName = "BulkDelete/BulkDeleteAsync - Should throw when collection is null")]
    [Trait("Category", "Delete")]
    [Trait("Method", "BulkDelete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BulkDelete_ShouldThrowWhenCollectionIsNull(bool isAsync)
    {
        // Arrange & Act & Assert
        if (isAsync)
            await Should.ThrowAsync<ArgumentNullException>(async () => await Repository.BulkDeleteAsync(null!));
        else
            Should.Throw<ArgumentNullException>(() => Repository.BulkDelete(null!));
    }

    [Theory(DisplayName = "Delete - Should cascade soft delete related entities")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldCascadeSoftDeleteRelatedEntities(bool isAsync)
    {
        // Arrange
        var parent = await CreateAndAddTestEntity();
        var child1 = CreateTestEntity("Child 1");
        var child2 = CreateTestEntity("Child 2");
        child1.ParentId = parent.Id;
        child2.ParentId = parent.Id;
        await Repository.BulkAddAsync(new[] { child1, child2 });
        await Repository.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            await Repository.DeleteAsync(parent);
            await Repository.SaveChangesAsync();
        }
        else
        {
            Repository.Delete(parent);
            Repository.SaveChanges();
        }

        // Assert
        var dbParent = await Repository.GetByIdAsync(parent.Id, withDeleted: true);
        dbParent.ShouldNotBeNull();
        dbParent.DeletedDate.ShouldNotBeNull();

        var children = await Repository.GetAllAsync(
            predicate: e => e.ParentId == parent.Id,
            withDeleted: true
        );
        children.Count.ShouldBe(2);
        children.ShouldAllBe(c => c.DeletedDate != null);
    }

    // Bu testi kaldırıyoruz çünkü artık one-to-one ilişkilerde exception fırlatmıyoruz
    // [Theory(DisplayName = "Delete - Should throw when entity has one-to-one relation")]
    // public async Task Delete_ShouldThrowWhenEntityHasOneToOneRelation(bool isAsync)
    // {
    //     // ...existing code...
    // }

    [Theory(DisplayName = "Delete - Should skip already soft deleted entities")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldSkipAlreadySoftDeletedEntities(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        var initialDeleteTime = DateTime.UtcNow.AddMinutes(-5);
        entity.DeletedDate = initialDeleteTime;
        await Repository.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            await Repository.DeleteAsync(entity);
            await Repository.SaveChangesAsync();
        }
        else
        {
            Repository.Delete(entity);
            Repository.SaveChanges();
        }

        // Assert
        var dbEntity = await Repository.GetByIdAsync(entity.Id, withDeleted: true);
        dbEntity.ShouldNotBeNull();
        dbEntity.DeletedDate.ShouldBe(initialDeleteTime);
    }

    [Theory(DisplayName = "Delete - Should handle null navigation properties")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldHandleNullNavigationProperties(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        entity.Parent = null;
        entity.Children.Clear();
        await Repository.SaveChangesAsync();

        // Act & Assert
        if (isAsync)
            await Should.NotThrowAsync(async () => 
            {
                await Repository.DeleteAsync(entity);
                await Repository.SaveChangesAsync();
            });
        else
            Should.NotThrow(() => 
            {
                Repository.Delete(entity);
                Repository.SaveChanges();
            });
    }

    [Theory(DisplayName = "BulkDelete - Should handle mixed soft/permanent delete")]
    [Trait("Category", "Delete")]
    [Trait("Method", "BulkDelete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BulkDelete_ShouldHandleMixedSoftPermanentDelete(bool isAsync)
    {
        // Arrange
        var entitiesToSoftDelete = CreateTestEntities(2);
        var entitiesToPermanentDelete = CreateTestEntities(2);
        await Repository.BulkAddAsync([.. entitiesToSoftDelete, .. entitiesToPermanentDelete]);
        await Repository.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            await Repository.BulkDeleteAsync(entitiesToSoftDelete, permanent: false);
            await Repository.BulkDeleteAsync(entitiesToPermanentDelete, permanent: true);
            await Repository.SaveChangesAsync();
        }
        else
        {
            Repository.BulkDelete(entitiesToSoftDelete, permanent: false);
            Repository.BulkDelete(entitiesToPermanentDelete, permanent: true);
            Repository.SaveChanges();
        }

        // Assert
        var allEntities = await Repository.GetAllAsync(withDeleted: true);
        allEntities.Count.ShouldBe(2); // Only soft deleted entities should remain
        allEntities.ShouldAllBe(e => e.DeletedDate != null);
    }

    [Theory(DisplayName = "Delete - Should soft delete one-to-one related entity")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldSoftDeleteOneToOneRelatedEntity(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        var singleDetail = new SingleDetail { TestEntityId = entity.Id, Detail = "Test Detail" };
        entity.SingleDetail = singleDetail;
        await Context.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            await Repository.DeleteAsync(entity);
            await Repository.SaveChangesAsync();
        }
        else
        {
            Repository.Delete(entity);
            Repository.SaveChanges();
        }

        // Assert
        var dbEntity = await Context.TestEntities
            .IgnoreQueryFilters()
            .Include(e => e.SingleDetail)
            .SingleAsync(e => e.Id == entity.Id);

        dbEntity.DeletedDate.ShouldNotBeNull();
        dbEntity.SingleDetail.ShouldNotBeNull();
        dbEntity.SingleDetail.DeletedDate.ShouldNotBeNull();
        dbEntity.SingleDetail.DeletedDate.ShouldBe(dbEntity.DeletedDate); // Aynı zamanda silinmiş olmalılar
    }

    [Theory(DisplayName = "Delete - Should soft delete one-to-many related entities")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldSoftDeleteOneToManyRelatedEntities(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        var details = new[]
        {
            new DetailEntity { TestEntityId = entity.Id, Detail = "Detail 1" },
            new DetailEntity { TestEntityId = entity.Id, Detail = "Detail 2" }
        };
        entity.Details = details.ToList();
        await Context.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            await Repository.DeleteAsync(entity);
            await Repository.SaveChangesAsync();
        }
        else
        {
            Repository.Delete(entity);
            Repository.SaveChanges();
        }

        // Assert
        var dbEntity = await Context.TestEntities
            .IgnoreQueryFilters()
            .Include(e => e.Details)
            .SingleAsync(e => e.Id == entity.Id);

        dbEntity.DeletedDate.ShouldNotBeNull();
        dbEntity.Details.Count.ShouldBe(2);
        dbEntity.Details.ShouldAllBe(d => d.DeletedDate != null);
    }

    [Theory(DisplayName = "Delete - Should soft delete many-to-many related entities")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldSoftDeleteManyToManyRelatedEntities(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        var tags = new[]
        {
            new TagEntity { Name = "Tag 1" },
            new TagEntity { Name = "Tag 2" }
        };
        await Context.Tags.AddRangeAsync(tags);
        await Context.SaveChangesAsync();

        entity.Tags = tags.ToList();
        await Context.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            await Repository.DeleteAsync(entity);
            await Repository.SaveChangesAsync();
        }
        else
        {
            Repository.Delete(entity);
            Repository.SaveChanges();
        }

        // Assert
        var dbEntity = await Context.TestEntities
            .IgnoreQueryFilters()
            .Include(e => e.Tags)
            .SingleAsync(e => e.Id == entity.Id);

        dbEntity.DeletedDate.ShouldNotBeNull();
        dbEntity.Tags.Count.ShouldBe(2);
        foreach (var tag in dbEntity.Tags)
        {
            tag.DeletedDate.ShouldNotBeNull();
            tag.DeletedDate.ShouldBe(dbEntity.DeletedDate); // Aynı zamanda silinmiş olmalılar
        }
    }

    [Theory(DisplayName = "Delete - Should soft delete nested related entities")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldSoftDeleteNestedRelatedEntities(bool isAsync)
    {
        // Arrange
        var entity = await CreateAndAddTestEntity();
        
        // Add one-to-one
        var singleDetail = new SingleDetail { TestEntityId = entity.Id, Detail = "Test Detail" };
        await Context.SingleDetails.AddAsync(singleDetail);
        
        // Add one-to-many
        var details = new[]
        {
            new DetailEntity { TestEntityId = entity.Id, Detail = "Detail 1" },
            new DetailEntity { TestEntityId = entity.Id, Detail = "Detail 2" }
        };
        await Context.Details.AddRangeAsync(details);
        
        // Add many-to-many
        var tags = new[]
        {
            new TagEntity { Name = "Tag 1" },
            new TagEntity { Name = "Tag 2" }
        };
        await Context.Tags.AddRangeAsync(tags);
        
        await Context.SaveChangesAsync();

        entity.SingleDetail = singleDetail;
        entity.Details = details.ToList();
        entity.Tags = tags.ToList();
        await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        entity = await Context.TestEntities
            .Include(e => e.SingleDetail)
            .Include(e => e.Details)
            .Include(e => e.Tags)
            .SingleAsync(e => e.Id == entity.Id);

        // Act
        if (isAsync)
        {
            await Repository.DeleteAsync(entity);
            await Repository.SaveChangesAsync();
        }
        else
        {
            Repository.Delete(entity);
            Repository.SaveChanges();
        }

        // Assert
        var dbEntity = await Context.TestEntities
            .IgnoreQueryFilters()
            .Include(e => e.SingleDetail)
            .Include(e => e.Details)
            .Include(e => e.Tags)
            .SingleAsync(e => e.Id == entity.Id);

        dbEntity.DeletedDate.ShouldNotBeNull();
        
        dbEntity.SingleDetail.ShouldNotBeNull();
        dbEntity.SingleDetail.DeletedDate.ShouldNotBeNull();
        
        dbEntity.Details.Count.ShouldBe(2);
        dbEntity.Details.ShouldAllBe(d => d.DeletedDate != null);
        
        dbEntity.Tags.Count.ShouldBe(2);
        dbEntity.Tags.ShouldAllBe(t => t.DeletedDate != null);
    }

    public class OneToOneEntity : Entity<Guid>
    {
        public virtual OneToOneDependency? Dependency { get; set; }

        public static (OneToOneEntity Entity, OneToOneDependency Dependency) CreateWithDependency(Guid id)
        {
            var entity = new OneToOneEntity { Id = id };
            var dependency = new OneToOneDependency { Id = id, Entity = entity };
            entity.Dependency = dependency;
            return (entity, dependency);
        }
    }

    public class OneToOneDependency : Entity<Guid>
    {
        public virtual OneToOneEntity? Entity { get; set; }
    }

    public class TestDbContextWithOneToOne : DbContext
    {
        public DbSet<OneToOneEntity> OneToOneEntities { get; set; } = null!;
        public DbSet<OneToOneDependency> OneToOneDependencies { get; set; } = null!;

        public TestDbContextWithOneToOne(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OneToOneEntity>(builder =>
            {
                builder.ToTable("OneToOneEntities");
                builder.HasKey(e => e.Id);
                builder.Property(e => e.Id).ValueGeneratedNever();
                
                builder.HasQueryFilter(e => !e.DeletedDate.HasValue);

                builder.HasOne(e => e.Dependency)
                    .WithOne(d => d.Entity)
                    .HasForeignKey<OneToOneDependency>(d => d.Id)
                    .HasPrincipalKey<OneToOneEntity>(e => e.Id)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OneToOneDependency>(builder =>
            {
                builder.ToTable("OneToOneDependencies");
                builder.HasKey(e => e.Id);
                builder.Property(e => e.Id).ValueGeneratedNever();
                
                builder.HasQueryFilter(e => !e.DeletedDate.HasValue);
            });
        }
    }

    public class OneToOneEntityRepository : EfRepositoryBase<OneToOneEntity, Guid, TestDbContextWithOneToOne>
    {
        public OneToOneEntityRepository(TestDbContextWithOneToOne context) : base(context) { }
    }
}
