using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Persistence.EntityFramework.Repositories;
using NArchitecture.Core.Persistence.EntityFramework.Tests.Repositories;
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
        TestEntity entity = CreateTestEntity();
        _ = await Repository.AddAsync(entity);
        _ = await Repository.SaveChangesAsync();
        DateTime beforeDelete = DateTime.UtcNow;

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
        TestEntity? dbEntity = await Repository.GetByIdAsync(entity.Id, withDeleted: true);
        _ = dbEntity.ShouldNotBeNull();
        _ = dbEntity.DeletedAt.ShouldNotBeNull();
        dbEntity.DeletedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
        dbEntity.DeletedAt.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Theory(DisplayName = "Delete/DeleteAsync - Should permanent delete entity when specified")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldPermanentDeleteEntityWhenSpecified(bool isAsync)
    {
        // Arrange
        TestEntity entity = CreateTestEntity();
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
        TestEntity? dbEntity = await Repository.GetByIdAsync(entity.Id, withDeleted: true);
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
        List<TestEntity> entities = CreateTestEntities(entityCount);
        _ = await Repository.BulkAddAsync(entities);
        _ = await Repository.SaveChangesAsync();
        DateTime beforeDelete = DateTime.UtcNow;

        // Act
        if (isAsync)
        {
            _ = await Repository.BulkDeleteAsync(entities);
            _ = await Repository.SaveChangesAsync();
        }
        else
        {
            _ = Repository.BulkDelete(entities);
            _ = Repository.SaveChanges();
        }

        // Assert
        ICollection<TestEntity> dbEntities = await Repository.GetAllAsync(withDeleted: true);
        dbEntities.Count.ShouldBe(entityCount);
        foreach (TestEntity entity in dbEntities)
        {
            _ = entity.DeletedAt.ShouldNotBeNull();
            entity.DeletedAt.Value.ShouldBeGreaterThanOrEqualTo(beforeDelete);
            entity.DeletedAt.Value.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
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
        List<TestEntity> entities = CreateTestEntities(entityCount);
        _ = await Repository.BulkAddAsync(entities);
        _ = await Repository.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            _ = await Repository.BulkDeleteAsync(entities, permanent: true);
            _ = await Repository.SaveChangesAsync();
        }
        else
        {
            _ = Repository.BulkDelete(entities, permanent: true);
            _ = Repository.SaveChanges();
        }

        // Assert
        ICollection<TestEntity> dbEntities = await Repository.GetAllAsync(withDeleted: true);
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
        _ = isAsync
            ? await Should.ThrowAsync<ArgumentNullException>(async () => await Repository.DeleteAsync(null!))
            : Should.Throw<ArgumentNullException>(() => Repository.Delete(null!));
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
            _ = Should.NotThrow(() => Repository.BulkDelete(entities));
    }

    [Theory(DisplayName = "BulkDelete/BulkDeleteAsync - Should throw when collection is null")]
    [Trait("Category", "Delete")]
    [Trait("Method", "BulkDelete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task BulkDelete_ShouldThrowWhenCollectionIsNull(bool isAsync)
    {
        // Arrange & Act & Assert
        _ = isAsync
            ? await Should.ThrowAsync<ArgumentNullException>(async () => await Repository.BulkDeleteAsync(null!))
            : Should.Throw<ArgumentNullException>(() => Repository.BulkDelete(null!));
    }

    [Theory(DisplayName = "Delete - Should cascade soft delete related entities")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldCascadeSoftDeleteRelatedEntities(bool isAsync)
    {
        // Arrange
        TestEntity parent = await CreateAndAddTestEntity();
        TestEntity child1 = CreateTestEntity("Child 1");
        TestEntity child2 = CreateTestEntity("Child 2");
        child1.ParentId = parent.Id;
        child2.ParentId = parent.Id;
        _ = await Repository.BulkAddAsync(new[] { child1, child2 });
        _ = await Repository.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            _ = await Repository.DeleteAsync(parent);
            _ = await Repository.SaveChangesAsync();
        }
        else
        {
            _ = Repository.Delete(parent);
            _ = Repository.SaveChanges();
        }

        // Assert
        TestEntity? dbParent = await Repository.GetByIdAsync(parent.Id, withDeleted: true);
        _ = dbParent.ShouldNotBeNull();
        _ = dbParent.DeletedAt.ShouldNotBeNull();

        ICollection<TestEntity> children = await Repository.GetAllAsync(
            predicate: e => e.ParentId == parent.Id,
            withDeleted: true
        );
        children.Count.ShouldBe(2);
        children.ShouldAllBe(c => c.DeletedAt != null);
    }

    // Bu testi kaldırıyoruz çünkü artık one-to-one ilişkilerde exception fırlatmıyoruz
    // [Theory(DisplayName = "Delete - Should throw when entity has one-to-one relation")]
    // public async Task Delete_ShouldThrowWhenEntityHasOneToOneRelation(bool isAsync)
    // {
    //     // ...existing code...
    // }

    [Theory(DisplayName = "Delete - Should handle soft deleted entity properly")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true, true)] // isAsync, permanent
    [InlineData(false, true)] // sync, permanent
    [InlineData(true, false)] // isAsync, not permanent
    [InlineData(false, false)] // sync, not permanent
    public async Task Delete_ShouldHandleSoftDeletedEntity(bool isAsync, bool permanent)
    {
        // Arrange
        TestEntity entity = await CreateAndAddTestEntity();
        DateTime initialDeleteTime = DateTime.UtcNow.AddMinutes(-5);
        entity.DeletedAt = initialDeleteTime;
        _ = await Repository.SaveChangesAsync();

        // Clear change tracker and reload entity to ensure fresh state
        Context.ChangeTracker.Clear();
        entity = await Context.TestEntities.IgnoreQueryFilters().SingleAsync(e => e.Id == entity.Id);

        // Act & Assert
        if (!permanent)
        {
            // Should throw when attempting non-permanent delete
            string expectedMessage = $"The entity with id {entity.Id} has already been deleted.";

            _ = isAsync
                ? await Should.ThrowAsync<InvalidOperationException>(
                    async () =>
                    {
                        _ = await Repository.DeleteAsync(entity);
                        _ = await Repository.SaveChangesAsync();
                    },
                    expectedMessage
                )
                : Should.Throw<InvalidOperationException>(
                    () =>
                    {
                        _ = Repository.Delete(entity);
                        _ = Repository.SaveChanges();
                    },
                    expectedMessage
                );

            // Verify the original deletion time wasn't changed
            TestEntity dbEntity = await Context.TestEntities.IgnoreQueryFilters().SingleAsync(e => e.Id == entity.Id);
            dbEntity.DeletedAt.ShouldBe(initialDeleteTime);
        }
        else
        {
            // Should allow permanent delete
            if (isAsync)
                await Should.NotThrowAsync(async () =>
                {
                    _ = await Repository.DeleteAsync(entity, permanent: true);
                    _ = await Repository.SaveChangesAsync();
                });
            else
                Should.NotThrow(() =>
                {
                    _ = Repository.Delete(entity, permanent: true);
                    _ = Repository.SaveChanges();
                });

            // Verify the entity is permanently deleted
            TestEntity? dbEntity = await Context.TestEntities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entity.Id);
            dbEntity.ShouldBeNull();
        }
    }

    [Theory(DisplayName = "Delete - Should handle null navigation properties")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldHandleNullNavigationProperties(bool isAsync)
    {
        // Arrange
        TestEntity entity = await CreateAndAddTestEntity();
        entity.Parent = null;
        entity.Children.Clear();
        _ = await Repository.SaveChangesAsync();

        // Act & Assert
        if (isAsync)
            await Should.NotThrowAsync(async () =>
            {
                _ = await Repository.DeleteAsync(entity);
                _ = await Repository.SaveChangesAsync();
            });
        else
            Should.NotThrow(() =>
            {
                _ = Repository.Delete(entity);
                _ = Repository.SaveChanges();
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
        List<TestEntity> entitiesToSoftDelete = CreateTestEntities(2);
        List<TestEntity> entitiesToPermanentDelete = CreateTestEntities(2);
        _ = await Repository.BulkAddAsync([.. entitiesToSoftDelete, .. entitiesToPermanentDelete]);
        _ = await Repository.SaveChangesAsync();

        // Act
        if (isAsync)
        {
            _ = await Repository.BulkDeleteAsync(entitiesToSoftDelete, permanent: false);
            _ = await Repository.BulkDeleteAsync(entitiesToPermanentDelete, permanent: true);
            _ = await Repository.SaveChangesAsync();
        }
        else
        {
            _ = Repository.BulkDelete(entitiesToSoftDelete, permanent: false);
            _ = Repository.BulkDelete(entitiesToPermanentDelete, permanent: true);
            _ = Repository.SaveChanges();
        }

        // Assert
        ICollection<TestEntity> allEntities = await Repository.GetAllAsync(withDeleted: true);
        allEntities.Count.ShouldBe(2); // Only soft deleted entities should remain
        allEntities.ShouldAllBe(e => e.DeletedAt != null);
    }

    [Theory(DisplayName = "Delete - Should soft delete one-to-one related entity")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldSoftDeleteOneToOneRelatedEntity(bool isAsync)
    {
        // Arrange
        TestEntity entity = await CreateAndAddTestEntity();
        var singleDetail = new SingleDetail { TestEntityId = entity.Id, Detail = "Test Detail" };
        entity.SingleDetail = singleDetail;
        _ = await Context.SaveChangesAsync();

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
        TestEntity dbEntity = await Context
            .TestEntities.IgnoreQueryFilters()
            .Include(e => e.SingleDetail)
            .SingleAsync(e => e.Id == entity.Id);

        _ = dbEntity.DeletedAt.ShouldNotBeNull();
        _ = dbEntity.SingleDetail.ShouldNotBeNull();
        _ = dbEntity.SingleDetail.DeletedAt.ShouldNotBeNull();
        dbEntity.SingleDetail.DeletedAt.ShouldBe(dbEntity.DeletedAt); // Aynı zamanda silinmiş olmalılar
    }

    [Theory(DisplayName = "Delete - Should soft delete one-to-many related entities")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldSoftDeleteOneToManyRelatedEntities(bool isAsync)
    {
        // Arrange
        TestEntity entity = await CreateAndAddTestEntity();
        DetailEntity[] details =
        [
            new DetailEntity { TestEntityId = entity.Id, Detail = "Detail 1" },
            new DetailEntity { TestEntityId = entity.Id, Detail = "Detail 2" },
        ];
        entity.Details = details.ToList();
        _ = await Context.SaveChangesAsync();

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
        TestEntity dbEntity = await Context
            .TestEntities.IgnoreQueryFilters()
            .Include(e => e.Details)
            .SingleAsync(e => e.Id == entity.Id);

        _ = dbEntity.DeletedAt.ShouldNotBeNull();
        dbEntity.Details.Count.ShouldBe(2);
        dbEntity.Details.ShouldAllBe(d => d.DeletedAt != null);
    }

    [Theory(DisplayName = "Delete - Should soft delete many-to-many related entities")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Delete_ShouldSoftDeleteManyToManyRelatedEntities(bool isAsync)
    {
        // Arrange
        TestEntity entity = await CreateAndAddTestEntity();
        TagEntity[] tags = [new TagEntity { Name = "Tag 1" }, new TagEntity { Name = "Tag 2" }];
        await Context.Tags.AddRangeAsync(tags);
        _ = await Context.SaveChangesAsync();

        entity.Tags = tags.ToList();
        _ = await Context.SaveChangesAsync();

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
        TestEntity dbEntity = await Context
            .TestEntities.IgnoreQueryFilters()
            .Include(e => e.Tags)
            .SingleAsync(e => e.Id == entity.Id);

        _ = dbEntity.DeletedAt.ShouldNotBeNull();
        dbEntity.Tags.Count.ShouldBe(2);
        foreach (TagEntity tag in dbEntity.Tags)
        {
            _ = tag.DeletedAt.ShouldNotBeNull();
            tag.DeletedAt.ShouldBe(dbEntity.DeletedAt); // Aynı zamanda silinmiş olmalılar
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
        TestEntity entity = await CreateAndAddTestEntity();

        // Add one-to-one
        var singleDetail = new SingleDetail { TestEntityId = entity.Id, Detail = "Test Detail" };
        _ = await Context.SingleDetails.AddAsync(singleDetail);

        // Add one-to-many
        DetailEntity[] details =
        [
            new DetailEntity { TestEntityId = entity.Id, Detail = "Detail 1" },
            new DetailEntity { TestEntityId = entity.Id, Detail = "Detail 2" },
        ];
        await Context.Details.AddRangeAsync(details);

        // Add many-to-many
        TagEntity[] tags = [new TagEntity { Name = "Tag 1" }, new TagEntity { Name = "Tag 2" }];
        await Context.Tags.AddRangeAsync(tags);

        _ = await Context.SaveChangesAsync();

        entity.SingleDetail = singleDetail;
        entity.Details = details.ToList();
        entity.Tags = tags.ToList();
        _ = await Context.SaveChangesAsync();

        Context.ChangeTracker.Clear();
        entity = await Context
            .TestEntities.Include(e => e.SingleDetail)
            .Include(e => e.Details)
            .Include(e => e.Tags)
            .SingleAsync(e => e.Id == entity.Id);

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
        TestEntity dbEntity = await Context
            .TestEntities.IgnoreQueryFilters()
            .Include(e => e.SingleDetail)
            .Include(e => e.Details)
            .Include(e => e.Tags)
            .SingleAsync(e => e.Id == entity.Id);

        _ = dbEntity.DeletedAt.ShouldNotBeNull();

        _ = dbEntity.SingleDetail.ShouldNotBeNull();
        _ = dbEntity.SingleDetail.DeletedAt.ShouldNotBeNull();

        dbEntity.Details.Count.ShouldBe(2);
        dbEntity.Details.ShouldAllBe(d => d.DeletedAt != null);

        dbEntity.Tags.Count.ShouldBe(2);
        dbEntity.Tags.ShouldAllBe(t => t.DeletedAt != null);
    }

    [Theory(DisplayName = "Delete - Should handle permanent delete and re-delete of soft-deleted entity")]
    [Trait("Category", "Delete")]
    [Trait("Method", "Delete")]
    [InlineData(true, true)] // isAsync, permanent
    [InlineData(false, true)] // sync, permanent
    [InlineData(true, false)] // isAsync, not permanent
    [InlineData(false, false)] // sync, not permanent
    public async Task Delete_ShouldHandlePermanentAndReDeleteOfSoftDeletedEntity(bool isAsync, bool permanent)
    {
        // Arrange
        TestEntity entity = await CreateAndAddTestEntity();
        DateTime initialDeleteTime = DateTime.UtcNow.AddMinutes(-5);
        entity.DeletedAt = initialDeleteTime;
        _ = await Repository.SaveChangesAsync();

        // Clear change tracker and reload entity to ensure fresh state
        Context.ChangeTracker.Clear();
        entity = await Context.TestEntities.IgnoreQueryFilters().SingleAsync(e => e.Id == entity.Id);

        // Act & Assert
        if (!permanent)
        {
            // Should throw when attempting non-permanent delete
            string expectedMessage = $"The entity with id {entity.Id} has already been deleted.";

            _ = isAsync
                ? await Should.ThrowAsync<InvalidOperationException>(
                    async () =>
                    {
                        _ = await Repository.DeleteAsync(entity);
                        _ = await Repository.SaveChangesAsync();
                    },
                    expectedMessage
                )
                : Should.Throw<InvalidOperationException>(
                    () =>
                    {
                        _ = Repository.Delete(entity);
                        _ = Repository.SaveChanges();
                    },
                    expectedMessage
                );

            // Verify the original deletion time wasn't changed
            TestEntity dbEntity = await Context.TestEntities.IgnoreQueryFilters().SingleAsync(e => e.Id == entity.Id);
            dbEntity.DeletedAt.ShouldBe(initialDeleteTime);
        }
        else
        {
            // Should allow permanent delete
            if (isAsync)
                await Should.NotThrowAsync(async () =>
                {
                    _ = await Repository.DeleteAsync(entity, permanent: true);
                    _ = await Repository.SaveChangesAsync();
                });
            else
                Should.NotThrow(() =>
                {
                    _ = Repository.Delete(entity, permanent: true);
                    _ = Repository.SaveChanges();
                });

            // Verify the entity is permanently deleted
            TestEntity? dbEntity = await Context.TestEntities.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == entity.Id);
            dbEntity.ShouldBeNull();
        }
    }

    public class OneToOneEntity : BaseEntity<Guid>
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

    public class OneToOneDependency : BaseEntity<Guid>
    {
        public virtual OneToOneEntity? Entity { get; set; }
    }

    public class TestDbContextWithOneToOne : DbContext
    {
        public DbSet<OneToOneEntity> OneToOneEntities { get; set; } = null!;
        public DbSet<OneToOneDependency> OneToOneDependencies { get; set; } = null!;

        public TestDbContextWithOneToOne(DbContextOptions options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<OneToOneEntity>(builder =>
            {
                _ = builder.ToTable("OneToOneEntities");
                _ = builder.HasKey(e => e.Id);
                _ = builder.Property(e => e.Id).ValueGeneratedNever();

                _ = builder.HasQueryFilter(e => !e.DeletedAt.HasValue);

                _ = builder
                    .HasOne(e => e.Dependency)
                    .WithOne(d => d.Entity)
                    .HasForeignKey<OneToOneDependency>(d => d.Id)
                    .HasPrincipalKey<OneToOneEntity>(e => e.Id)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);
            });

            _ = modelBuilder.Entity<OneToOneDependency>(builder =>
            {
                _ = builder.ToTable("OneToOneDependencies");
                _ = builder.HasKey(e => e.Id);
                _ = builder.Property(e => e.Id).ValueGeneratedNever();

                _ = builder.HasQueryFilter(e => !e.DeletedAt.HasValue);
            });
        }
    }

    public class OneToOneEntityRepository : EfRepositoryBase<OneToOneEntity, Guid, TestDbContextWithOneToOne>
    {
        public OneToOneEntityRepository(TestDbContextWithOneToOne context)
            : base(context) { }
    }
}
