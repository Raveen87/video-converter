using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VideoConverter.Api.Shared.Infrastructure.Persistence;

namespace VideoConverter.Tests.TestUtils;

public static class DbContextTestFactory
{
    public static AppDbContext CreateInMemory()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public static TestDbContextWithConnection CreateSqliteInMemory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new AppDbContext(options);
        _ = ctx.Database.EnsureCreated();
        return new TestDbContextWithConnection(ctx, connection);
    }

    public static AppDbContext CreateSqliteInMemory(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new AppDbContext(options);
        _ = ctx.Database.EnsureCreated();
        return ctx;
    }

    public sealed class TestDbContextWithConnection : IDisposable
    {
        private readonly SqliteConnection _connection;

        public TestDbContextWithConnection(AppDbContext context, SqliteConnection connection)
        {
            Context = context;
            _connection = connection;
        }

        public AppDbContext Context { get; }

        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
    }
}
