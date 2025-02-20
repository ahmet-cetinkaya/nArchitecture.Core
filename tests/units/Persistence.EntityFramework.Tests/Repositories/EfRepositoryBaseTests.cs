using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NArchitecture.Core.Persistence.EntityFramework.Tests.Repositories;

namespace Core.Persistence.EntityFramework.Tests.Repositories;

public partial class EfRepositoryBaseTests : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly TestDbContext Context;
    protected readonly TestEntityRepository Repository;

    public EfRepositoryBaseTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;

        Context = new TestDbContext(options);
        _ = Context.Database.EnsureCreated();
        Repository = new TestEntityRepository(Context);
    }

    protected TestDbContext CreateTestDbContext()
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        return new TestDbContext(options);
    }

    protected TestEntity CreateTestEntity(string? name = null)
    {
        return new() { Name = name ?? $"Test Entity {Guid.NewGuid()}", Description = "Test Description" };
    }

    protected List<TestEntity> CreateTestEntities(int count)
    {
        return [.. Enumerable.Range(0, count).Select(i => CreateTestEntity($"Test Entity {i}"))];
    }

    public void Dispose()
    {
        _ = Context.Database.EnsureDeleted();
        Context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
