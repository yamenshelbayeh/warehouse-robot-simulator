using Simulation.Models;
using Warehouse.Api.Domain;
using WarehouseModel = Warehouse.Api.Domain.Warehouse;

namespace Warehouse.Api.Contracts;

public sealed record CellView(Position Position, CellType Type);
public sealed record RobotView(int Id, Position Position, RobotState State, int Battery, int Capacity, bool Enabled, bool CarryingItem);
public sealed record TaskView(Guid Id, Position Pickup, Position Dropoff, int Load, int Priority, DeliveryStatus Status);
public sealed record AssignmentView(Guid Id, Guid TaskId, int RobotId, long AssignedAtTick, long? EndedAtTick, bool Active);
public sealed record ReservationView(long Tick, int RobotId, Position From, Position To);

public sealed record WarehouseSnapshot(Guid Id, string Name, int Rows, int Columns, long Tick, long Revision,
    IReadOnlyList<CellView> Cells, IReadOnlyList<RobotView> Robots, IReadOnlyList<TaskView> Tasks,
    IReadOnlyList<AssignmentView> Assignments, IReadOnlyList<ReservationView> Reservations)
{
    public static WarehouseSnapshot From(WarehouseModel warehouse) => new(
        warehouse.Id, warehouse.Name, warehouse.Rows, warehouse.Columns, warehouse.Tick, warehouse.Revision,
        warehouse.Cells.OrderBy(c => c.Row).ThenBy(c => c.Column).Select(c => new CellView(c.Position, c.Type)).ToArray(),
        warehouse.Robots.OrderBy(r => r.Id).Select(r => new RobotView(
            r.Id, r.Position, r.State, r.Battery, r.Capacity, r.Enabled, r.CarryingItem)).ToArray(),
        warehouse.Tasks.OrderBy(t => t.Id).Select(t => new TaskView(
            t.Id, t.Pickup, t.Dropoff, t.Load, t.Priority, t.Status)).ToArray(),
        warehouse.Assignments.OrderBy(a => a.AssignedAtTick).ThenBy(a => a.Id).Select(a => new AssignmentView(
            a.Id, a.TaskId, a.RobotId, a.AssignedAtTick, a.EndedAtTick, a.Active)).ToArray(),
        warehouse.Reservations.OrderBy(r => r.RobotId).Select(r => new ReservationView(
            r.Tick, r.RobotId, new(r.FromRow, r.FromColumn), new(r.Row, r.Column))).ToArray());
}
