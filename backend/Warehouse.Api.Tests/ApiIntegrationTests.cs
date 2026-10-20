using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Contracts;
using Warehouse.Api.Domain;
using Warehouse.Api.Integration;
using Warehouse.Api.Persistence;
using WarehouseModel = Warehouse.Api.Domain.Warehouse;

namespace Warehouse.Api.Tests;

[Collection("Postgres")]
public sealed class ApiIntegrationTests(PostgresFixture fixture)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private async Task<WarehouseSnapshot> Create()
        => await Snapshot(await fixture.Client.PostAsJsonAsync("/api/warehouses/", new
        {
            name = "Integration", rows = 1, columns = 5,
            cells = new[]
            {
                new { position = new { row = 0, column = 1 }, type = "Pickup" },
                new { position = new { row = 0, column = 4 }, type = "Dropoff" }
            }
        }));

    private static async Task<WarehouseSnapshot> Snapshot(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{response.StatusCode}: {body}");
        return JsonSerializer.Deserialize<WarehouseSnapshot>(body, Json)!;
    }

    private async Task<WarehouseSnapshot> AddRobot(WarehouseSnapshot warehouse, int column = 0)
        => await Snapshot(await fixture.Client.PostAsJsonAsync($"/api/warehouses/{warehouse.Id}/robots",
            new { expectedRevision = warehouse.Revision, position = new { row = 0, column }, battery = 100, capacity = 5 }));

    private async Task<WarehouseSnapshot> AddTask(WarehouseSnapshot warehouse)
        => await Snapshot(await fixture.Client.PostAsJsonAsync($"/api/warehouses/{warehouse.Id}/tasks",
            new
            {
                expectedRevision = warehouse.Revision,
                pickup = new { row = 0, column = 1 }, dropoff = new { row = 0, column = 4 }, load = 2, priority = 1
            }));

    [PostgresFact]
    public async Task MigrationsAreAppliedAndModelMatchesSnapshot()
    {
        await using var db = fixture.CreateContext();
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.GetAsync("/health/ready")).StatusCode);
    }

    [PostgresFact]
    public async Task HttpDeliveryPersistsEveryTransitionAndReservation()
    {
        var warehouse = await AddTask(await AddRobot(await Create()));
        var taskId = Assert.Single(warehouse.Tasks).Id;
        warehouse = await Snapshot(await fixture.Client.PostAsJsonAsync(
            $"/api/warehouses/{warehouse.Id}/tasks/{taskId}/assign", new { expectedRevision = warehouse.Revision }));
        Assert.Equal(DeliveryStatus.Assigned, Assert.Single(warehouse.Tasks).Status);
        for (var i = 0; i < 6; i++)
        {
            warehouse = await Snapshot(await fixture.Client.PostAsJsonAsync(
                $"/api/warehouses/{warehouse.Id}/ticks", new { expectedRevision = warehouse.Revision }));
            Assert.Equal(warehouse.Tick, Assert.Single(warehouse.Reservations).Tick);
        }
        // A new DbContext verifies that this result does not depend on tracked in-memory objects.
        await using var db = fixture.CreateContext();
        var stored = await db.Warehouses.Include(w => w.Tasks).Include(w => w.Robots)
            .Include(w => w.Assignments).Include(w => w.Reservations)
            .AsSplitQuery().SingleAsync(w => w.Id == warehouse.Id);
        Assert.Equal(DeliveryStatus.Completed, Assert.Single(stored.Tasks).Status);
        Assert.Equal(new Position(0, 4), Assert.Single(stored.Robots).Position);
        Assert.Equal(96, Assert.Single(stored.Robots).Battery);
        Assert.False(Assert.Single(stored.Assignments).Active);
        Assert.Equal(6, Assert.Single(stored.Reservations).Tick);
        Assert.Equal(warehouse.Revision, stored.Revision);
    }

    [PostgresFact]
    public async Task ReplayingTickWithOldRevisionDoesNotAdvanceTwice()
    {
        var warehouse = await Create();
        var route = $"/api/warehouses/{warehouse.Id}/ticks";
        Assert.Equal(HttpStatusCode.OK,
            (await fixture.Client.PostAsJsonAsync(route, new { expectedRevision = 0 })).StatusCode);
        var repeated = await fixture.Client.PostAsJsonAsync(route, new { expectedRevision = 0 });
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        Assert.Contains("stale_revision", await repeated.Content.ReadAsStringAsync());
        var stored = await Snapshot(await fixture.Client.GetAsync($"/api/warehouses/{warehouse.Id}"));
        Assert.Equal(1, stored.Tick);
        Assert.Equal(1, stored.Revision);
    }

    [PostgresFact]
    public async Task TwoConcurrentAssignmentsCommitExactlyOneAndRollbackTheOther()
    {
        var warehouse = await AddTask(await AddTask(await AddRobot(await Create())));
        fixture.Gate.Arm(warehouse.Id);
        HttpResponseMessage[] results;
        try
        {
            results = await Task.WhenAll(warehouse.Tasks.Select(task => fixture.Client.PostAsJsonAsync(
                $"/api/warehouses/{warehouse.Id}/tasks/{task.Id}/assign",
                new { expectedRevision = warehouse.Revision, robotId = 1 })));
        }
        finally { fixture.Gate.Disarm(); }
        Assert.Single(results, r => r.StatusCode == HttpStatusCode.OK);
        var rejected = Assert.Single(results, r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Contains("concurrent_change", await rejected.Content.ReadAsStringAsync());
        var stored = await Snapshot(await fixture.Client.GetAsync($"/api/warehouses/{warehouse.Id}"));
        Assert.Equal(warehouse.Revision + 1, stored.Revision);
        Assert.Single(stored.Assignments);
        Assert.Single(stored.Tasks, t => t.Status == DeliveryStatus.Pending);
        Assert.Single(stored.Tasks, t => t.Status == DeliveryStatus.Assigned);
    }

    [PostgresFact]
    public async Task BadRequestsAndBusinessConflictsDoNotChangeRevision()
    {
        var warehouse = await AddRobot(await Create());
        var path = $"/api/warehouses/{warehouse.Id}";
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Client.PostAsJsonAsync(
            $"{path}/ticks", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Client.PostAsJsonAsync(
            $"{path}/robots", new { expectedRevision = warehouse.Revision, battery = 100, capacity = 5 })).StatusCode);
        var missingColumn = await fixture.Client.PostAsJsonAsync(
            $"{path}/robots", new { expectedRevision = warehouse.Revision, position = new { row = 0 }, battery = 100, capacity = 5 });
        Assert.Equal(HttpStatusCode.BadRequest, missingColumn.StatusCode);
        Assert.Contains("invalid_request", await missingColumn.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Client.PutAsJsonAsync(
            $"{path}/robots/1/enabled", new { expectedRevision = warehouse.Revision })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Client.PutAsJsonAsync(
            $"{path}/cells/0/2", new { expectedRevision = warehouse.Revision, type = "Unknown" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await fixture.Client.PostAsJsonAsync(
            $"{path}/robots", new { expectedRevision = warehouse.Revision, position = new { row = 0, column = 0 }, battery = 100, capacity = 5 })).StatusCode);
        var stored = await Snapshot(await fixture.Client.GetAsync(path));
        Assert.Equal(warehouse.Revision, stored.Revision);
        Assert.Single(stored.Robots);
    }

    [PostgresFact]
    public async Task TaskFromAnotherWarehouseCannotBeAssigned()
    {
        var source = await AddTask(await Create());
        var other = await AddRobot(await Create());
        var response = await fixture.Client.PostAsJsonAsync(
            $"/api/warehouses/{other.Id}/tasks/{source.Tasks[0].Id}/assign", new { expectedRevision = other.Revision });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("task_not_found", await response.Content.ReadAsStringAsync());
    }

    [PostgresFact]
    public async Task DatabaseRejectsTwoReservationsForSameCellAndTick()
    {
        var warehouse = await AddRobot(await AddRobot(await Create()), 3);
        await using var db = fixture.CreateContext();
        var original = (CellReservation)Activator.CreateInstance(typeof(CellReservation), nonPublic: true)!;
        // Set EF mapped values to exercise the constraint independently of domain validation.
        var first = db.Entry(original);
        first.Property(r => r.WarehouseId).CurrentValue = warehouse.Id;
        first.Property(r => r.Tick).CurrentValue = 1;
        first.Property(r => r.RobotId).CurrentValue = 1;
        first.Property(r => r.Row).CurrentValue = 0;
        first.Property(r => r.Column).CurrentValue = 2;
        first.Property(r => r.FromRow).CurrentValue = 0;
        first.Property(r => r.FromColumn).CurrentValue = 0;
        first.State = EntityState.Added;
        await db.SaveChangesAsync();
        await using var other = fixture.CreateContext();
        var duplicate = other.Entry((CellReservation)Activator.CreateInstance(typeof(CellReservation), nonPublic: true)!);
        foreach (var property in first.Properties)
            duplicate.Property(property.Metadata.Name).CurrentValue = property.CurrentValue;
        duplicate.Property(r => r.RobotId).CurrentValue = 2;
        duplicate.Property(r => r.FromColumn).CurrentValue = 3;
        duplicate.State = EntityState.Added;
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());
        Assert.Equal(Npgsql.PostgresErrorCodes.UniqueViolation,
            Assert.IsType<Npgsql.PostgresException>(error.InnerException).SqlState);
    }
}
