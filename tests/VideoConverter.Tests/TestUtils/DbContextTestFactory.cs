using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VideoConverter.Web.Shared.Infrastructure.Persistence;

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

    public sealed class TestDbContextWithConnection : IDisposable
    {
        public AppDbContext Context { get; }
        private readonly SqliteConnection _connection;
        public TestDbContextWithConnection(AppDbContext context, SqliteConnection connection)
        {
            Context = context;
            _connection = connection;
        }
        public void Dispose()
        {
            Context.Dispose();
            _connection.Dispose();
        }
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
}
