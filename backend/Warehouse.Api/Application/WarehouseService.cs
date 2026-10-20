using System.Data;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Contracts;
using Warehouse.Api.Domain;
using Warehouse.Api.Persistence;
using WarehouseModel = Warehouse.Api.Domain.Warehouse;

namespace Warehouse.Api.Application;

public sealed class WarehouseService(WarehouseDbContext db)
{
    private IQueryable<WarehouseModel> AggregateQuery => db.Warehouses
        .Include(w => w.Cells).Include(w => w.Robots).Include(w => w.Tasks)
        .Include(w => w.Assignments).Include(w => w.Reservations).AsSplitQuery();

    public async Task<WarehouseSnapshot> CreateAsync(CreateWarehouseRequest request, CancellationToken ct)
    {
        var warehouse = WarehouseModel.Create(request.Name, request.Rows, request.Columns);
        WarehouseModel.Require(request.Cells is null || request.Cells.All(c => c is not null),
            "invalid_cell", "Cell entries cannot be null.", 400);
        WarehouseModel.Require(request.Cells is null ||
            request.Cells.Select(c => c.Position).Distinct().Count() == request.Cells.Count,
            "duplicate_cell", "Each cell may be specified only once.", 400);
        foreach (var cell in request.Cells ?? []) warehouse.SetCell(cell.Position, cell.Type);
        db.Warehouses.Add(warehouse);
        await db.SaveChangesAsync(ct);
        return WarehouseSnapshot.From(warehouse);
    }

    public async Task<WarehouseSnapshot> GetAsync(Guid id, CancellationToken ct)
    {
        // Split queries must see a single consistent snapshot across all collections.
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var warehouse = await LoadAsync(id, ct);
        var result = WarehouseSnapshot.From(warehouse);
        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task<WarehouseSnapshot> MutateAsync(Guid id, long? expectedRevision,
        Action<WarehouseModel> command, CancellationToken ct)
    {
        WarehouseModel.Require(expectedRevision.HasValue, "revision_required", "Supply expectedRevision from the latest response.", 400);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
        var warehouse = await LoadAsync(id, ct);
        warehouse.CheckRevision(expectedRevision!.Value);
        command(warehouse);
        warehouse.CommitRevision();
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return WarehouseSnapshot.From(warehouse);
    }

    private async Task<WarehouseModel> LoadAsync(Guid id, CancellationToken ct)
        => await AggregateQuery.SingleOrDefaultAsync(w => w.Id == id, ct)
           ?? throw new RuleViolation("warehouse_not_found", "Warehouse was not found.", 404);
}
