using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Warehouse.Api.Persistence;
using WarehouseModel = Warehouse.Api.Domain.Warehouse;

namespace Warehouse.Api.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WAREHOUSE_TEST_POSTGRES")))
            Skip = "Set WAREHOUSE_TEST_POSTGRES to run tests against a disposable PostgreSQL database.";
    }
}

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>;

public sealed class PostgresFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = "";
    public HttpClient Client { get; private set; } = null!;
    public SaveGate Gate { get; } = new();
    private WebApplicationFactory<Program>? factory;

    public async Task InitializeAsync()
    {
        var configured = Environment.GetEnvironmentVariable("WAREHOUSE_TEST_POSTGRES");
        if (string.IsNullOrWhiteSpace(configured)) return;
        // Never migrate or delete the database supplied by the user.
        var settings = new NpgsqlConnectionStringBuilder(configured)
        {
            Database = $"warehouse_tests_{Guid.NewGuid():N}"
        };
        ConnectionString = settings.ConnectionString;
        await using (var db = CreateContext()) await db.Database.MigrateAsync();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<WarehouseDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<WarehouseDbContext>>();
                services.AddDbContext<WarehouseDbContext>(options =>
                    options.UseNpgsql(ConnectionString).AddInterceptors(Gate));
            });
        });
        Client = factory.CreateClient();
    }

    public WarehouseDbContext CreateContext() => new(new DbContextOptionsBuilder<WarehouseDbContext>()
        .UseNpgsql(ConnectionString).Options);

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (factory is not null) await factory.DisposeAsync();
        if (ConnectionString.Length == 0) return;
        var database = new NpgsqlConnectionStringBuilder(ConnectionString).Database;
        if (database is null || !database.StartsWith("warehouse_tests_", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to delete a database outside the generated test namespace.");
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}

// Forces both HTTP requests to load the same revision before either saves.
public sealed class SaveGate : SaveChangesInterceptor
{
    private Guid target;
    private int arrivals;
    private TaskCompletionSource ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Arm(Guid warehouseId)
    {
        target = warehouseId;
        arrivals = 2;
        ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
    public void Disarm() => target = Guid.Empty;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context!.ChangeTracker.Entries<WarehouseModel>()
            .Any(e => e.Entity.Id == target && e.State == EntityState.Modified))
        {
            if (Interlocked.Decrement(ref arrivals) == 0) ready.TrySetResult();
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
        return result;
    }
}
