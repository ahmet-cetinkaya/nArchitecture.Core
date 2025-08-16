using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.Abstractions.Repositories;
using NArchitecture.Core.Persistence.EntityFramework.Repositories;

namespace NArchitecture.Core.Persistence.EntityFramework.Tests.Repositories;

public class TestEntity : BaseEntity<Guid>
{
    public TestEntity()
    {
        Id = Guid.NewGuid();
        Children = new HashSet<TestEntity>();
        Tags = new HashSet<TagEntity>();
        Details = new HashSet<DetailEntity>();
    }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // One-to-One
    public virtual SingleDetail? SingleDetail { get; set; }

    // One-to-Many
    public Guid? ParentId { get; set; }
    public virtual TestEntity? Parent { get; set; }
    public virtual ICollection<TestEntity> Children { get; set; }
    public virtual ICollection<DetailEntity> Details { get; set; }

    // Many-to-Many
    public virtual ICollection<TagEntity> Tags { get; set; }
}

public class SingleDetail : BaseEntity<Guid>
{
    public required Guid TestEntityId { get; set; }
    public string Detail { get; set; } = string.Empty;
    public virtual TestEntity TestEntity { get; set; } = null!;
}

public class DetailEntity : BaseEntity<Guid>
{
    public Guid TestEntityId { get; set; }
    public string Detail { get; set; } = string.Empty;
    public virtual TestEntity TestEntity { get; set; } = null!;
}

public class TagEntity : BaseEntity<Guid>
{
    public TagEntity()
    {
        Id = Guid.NewGuid();
        TestEntities = new HashSet<TestEntity>();
    }

    public string Name { get; set; } = string.Empty;
    public virtual ICollection<TestEntity> TestEntities { get; set; }
}

public class TestDbContext : DbContext
{
    public DbSet<TestEntity> TestEntities { get; set; } = null!;
    public DbSet<SingleDetail> SingleDetails { get; set; } = null!;
    public DbSet<DetailEntity> Details { get; set; } = null!;
    public DbSet<TagEntity> Tags { get; set; } = null!;

    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<TestEntity>(builder =>
        {
            _ = builder.HasQueryFilter(e => !e.DeletedAt.HasValue);
            _ = builder.Property(e => e.Name).HasMaxLength(1000);
            _ = builder.Property(e => e.Description).HasMaxLength(2000);
            _ = builder.HasKey(e => e.Id);
            _ = builder.Property(e => e.Id).ValueGeneratedNever();

            // Add concurrency token
            _ = builder.Property(e => e.UpdatedAt).IsConcurrencyToken();

            // One-to-One
            _ = builder
                .HasOne(e => e.SingleDetail)
                .WithOne(d => d.TestEntity)
                .HasForeignKey<SingleDetail>(d => d.TestEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-Many with self
            _ = builder
                .HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // One-to-Many
            _ = builder
                .HasMany(e => e.Details)
                .WithOne(d => d.TestEntity)
                .HasForeignKey(d => d.TestEntityId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-Many
            _ = builder.HasMany(e => e.Tags).WithMany(t => t.TestEntities).UsingEntity(j => j.ToTable("TestEntityTags"));
        });

        _ = modelBuilder.Entity<SingleDetail>(builder =>
        {
            _ = builder.HasQueryFilter(e => !e.DeletedAt.HasValue);
            _ = builder.HasKey(e => e.Id);
            _ = builder.Property(e => e.TestEntityId);
        });

        _ = modelBuilder.Entity<DetailEntity>(builder =>
        {
            _ = builder.HasQueryFilter(e => !e.DeletedAt.HasValue);
        });

        _ = modelBuilder.Entity<TagEntity>(builder =>
        {
            _ = builder.HasQueryFilter(e => !e.DeletedAt.HasValue);
            _ = builder.Property(e => e.Name).HasMaxLength(50);
        });
    }
}

public class TestEntityRepository : EfRepositoryBase<TestEntity, Guid, TestDbContext>
{
    public TestEntityRepository(TestDbContext context)
        : base(context) { }
}
