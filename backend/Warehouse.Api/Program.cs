using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Warehouse.Api.Application;
using Warehouse.Api.Contracts;
using Warehouse.Api.Domain;
using Warehouse.Api.Integration;
using Warehouse.Api.Persistence;

var builder = WebApplication.CreateBuilder(args);
// Console logging works for local development and containers without OS event-log permissions.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Warehouse")));
builder.Services.AddScoped<WarehouseService>();
builder.Services.AddSingleton<ISimulationAlgorithms, SimulationAdapter>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
});
builder.Services.AddProblemDetails();
// Keep malformed-body errors consistent in Development and Production.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (RuleViolation ex)
    {
        await Results.Problem(statusCode: ex.Status, title: ex.Code, detail: ex.Message,
            extensions: new Dictionary<string, object?> { ["code"] = ex.Code }).ExecuteAsync(context);
    }
    catch (BadHttpRequestException ex)
    {
        await Results.Problem(statusCode: ex.StatusCode, title: "invalid_request",
            detail: "Check the JSON body, required fields and enum names.",
            extensions: new Dictionary<string, object?> { ["code"] = "invalid_request" }).ExecuteAsync(context);
    }
    catch (Exception ex) when (IsConflict(ex))
    {
        await Results.Problem(statusCode: 409, title: "concurrent_change",
            detail: "Another command changed this warehouse. Fetch the latest revision before retrying.",
            extensions: new Dictionary<string, object?> { ["code"] = "concurrent_change" }).ExecuteAsync(context);
    }
});
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "running" }));
app.MapGet("/health/ready", async (WarehouseDbContext db, CancellationToken ct) =>
{
    try
    {
        _ = await db.Warehouses.AnyAsync(ct);
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException)
    {
        return Results.Problem(statusCode: 503, title: "database_unavailable",
            detail: "Check the database connection and apply EF Core migrations.");
    }
});

var api = app.MapGroup("/api/warehouses");
api.MapGet("/", async (WarehouseDbContext db, CancellationToken ct) =>
    await db.Warehouses.AsNoTracking().OrderBy(w => w.Name)
        .Select(w => new { w.Id, w.Name, w.Rows, w.Columns, w.Tick, w.Revision }).ToListAsync(ct));
api.MapPost("/", async (CreateWarehouseRequest request, WarehouseService service, CancellationToken ct) =>
{
    var snapshot = await service.CreateAsync(request, ct);
    return Results.Created($"/api/warehouses/{snapshot.Id}", snapshot);
});
api.MapGet("/{id:guid}", (Guid id, WarehouseService service, CancellationToken ct) => service.GetAsync(id, ct));
api.MapPut("/{id:guid}/cells/{row:int}/{column:int}",
    (Guid id, int row, int column, SetCellRequest request, WarehouseService service, CancellationToken ct) =>
        service.MutateAsync(id, request.ExpectedRevision, w => w.SetCell(new(row, column), request.Type), ct));
api.MapPost("/{id:guid}/robots",
    (Guid id, AddRobotRequest request, WarehouseService service, CancellationToken ct) =>
        service.MutateAsync(id, request.ExpectedRevision,
            w => w.AddRobot(Required(request.Position, "position"), request.Battery, request.Capacity), ct));
api.MapPut("/{id:guid}/robots/{robotId:int}/enabled",
    (Guid id, int robotId, SetRobotEnabledRequest request, WarehouseService service, CancellationToken ct) =>
        service.MutateAsync(id, request.ExpectedRevision, w => w.SetRobotEnabled(robotId, request.Enabled), ct));
api.MapPost("/{id:guid}/tasks",
    (Guid id, AddTaskRequest request, WarehouseService service, CancellationToken ct) =>
        service.MutateAsync(id, request.ExpectedRevision, w => w.AddTask(Required(request.Pickup, "pickup"),
            Required(request.Dropoff, "dropoff"), request.Load, request.Priority), ct));
api.MapPost("/{id:guid}/tasks/{taskId:guid}/assign",
    (Guid id, Guid taskId, AssignTaskRequest request, WarehouseService service,
        ISimulationAlgorithms algorithms, CancellationToken ct) =>
        service.MutateAsync(id, request.ExpectedRevision, w => w.Assign(taskId, request.RobotId, algorithms), ct));
api.MapPost("/{id:guid}/tasks/{taskId:guid}/cancel",
    (Guid id, Guid taskId, RevisionRequest request, WarehouseService service, CancellationToken ct) =>
        service.MutateAsync(id, request.ExpectedRevision, w => w.CancelTask(taskId), ct));
api.MapPost("/{id:guid}/ticks",
    (Guid id, RevisionRequest request, WarehouseService service, ISimulationAlgorithms algorithms, CancellationToken ct) =>
        service.MutateAsync(id, request.ExpectedRevision, w => w.AdvanceTick(algorithms), ct));
app.Run();

static Position Required(Position? position, string name)
    => position ?? throw new RuleViolation("missing_position", $"Supply {name} coordinates.", 400);

static bool IsConflict(Exception ex) => ex is DbUpdateConcurrencyException ||
    ex is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
        or PostgresErrorCodes.UniqueViolation } ||
    ex.InnerException is not null && IsConflict(ex.InnerException);

public partial class Program { }
