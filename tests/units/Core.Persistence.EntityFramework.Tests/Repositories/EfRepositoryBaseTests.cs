using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

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

        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_connection).Options;

        Context = new TestDbContext(options);
        Context.Database.EnsureCreated();
        Repository = new TestEntityRepository(Context);
    }

    protected TestEntity CreateTestEntity(string? name = null) =>
        new() { Name = name ?? $"Test Entity {Guid.NewGuid()}", Description = "Test Description" };

    protected List<TestEntity> CreateTestEntities(int count) =>
        [.. Enumerable.Range(0, count).Select(i => CreateTestEntity($"Test Entity {i}"))];

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
