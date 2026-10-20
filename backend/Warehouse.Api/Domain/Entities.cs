using Simulation.Models;
using System.Text.Json.Serialization;

namespace Warehouse.Api.Domain;

public readonly record struct Position([property: JsonRequired] int Row, [property: JsonRequired] int Column);
public enum DeliveryStatus { Pending, Assigned, InProgress, Completed, Cancelled }

public sealed class RuleViolation(string code, string message, int status = 409) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}

public sealed class GridCell
{
    private GridCell() { }
    internal GridCell(Guid warehouseId, Position position, CellType type)
        => (WarehouseId, Row, Column, Type) = (warehouseId, position.Row, position.Column, type);
    public Guid WarehouseId { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }
    public CellType Type { get; internal set; }
    public Position Position => new(Row, Column);
}

public sealed class WarehouseRobot
{
    private WarehouseRobot() { }
    internal WarehouseRobot(Guid warehouseId, int id, Position position, int battery, int capacity)
    {
        (WarehouseId, Id, Row, Column, Battery, Capacity) =
            (warehouseId, id, position.Row, position.Column, battery, capacity);
    }
    public Guid WarehouseId { get; private set; }
    // Stable warehouse-local ID, also used by Erfan's deterministic tie breaker.
    public int Id { get; private set; }
    public int Row { get; internal set; }
    public int Column { get; internal set; }
    public RobotState State { get; internal set; } = RobotState.Idle;
    public int Battery { get; internal set; }
    public int Capacity { get; private set; }
    public bool Enabled { get; internal set; } = true;
    public bool CarryingItem { get; internal set; }
    public Position Position => new(Row, Column);
}

public sealed class DeliveryTask
{
    private DeliveryTask() { }
    internal DeliveryTask(Guid warehouseId, Position pickup, Position dropoff, int load, int priority)
    {
        WarehouseId = warehouseId;
        Id = Guid.NewGuid();
        (PickupRow, PickupColumn, DropoffRow, DropoffColumn) =
            (pickup.Row, pickup.Column, dropoff.Row, dropoff.Column);
        (Load, Priority) = (load, priority);
    }
    public Guid WarehouseId { get; private set; }
    public Guid Id { get; private set; }
    public int PickupRow { get; private set; }
    public int PickupColumn { get; private set; }
    public int DropoffRow { get; private set; }
    public int DropoffColumn { get; private set; }
    public int Load { get; private set; }
    public int Priority { get; private set; }
    public DeliveryStatus Status { get; internal set; }
    public Position Pickup => new(PickupRow, PickupColumn);
    public Position Dropoff => new(DropoffRow, DropoffColumn);
}

public sealed class TaskAssignment
{
    private TaskAssignment() { }
    internal TaskAssignment(Guid warehouseId, Guid taskId, int robotId, long tick)
        => (WarehouseId, Id, TaskId, RobotId, AssignedAtTick, Active) =
            (warehouseId, Guid.NewGuid(), taskId, robotId, tick, true);
    public Guid WarehouseId { get; private set; }
    public Guid Id { get; private set; }
    public Guid TaskId { get; private set; }
    public int RobotId { get; private set; }
    public long AssignedAtTick { get; private set; }
    public long? EndedAtTick { get; internal set; }
    public bool Active { get; internal set; }
}

public sealed class CellReservation
{
    private CellReservation() { }
    internal CellReservation(Guid warehouseId, long tick, int robotId, Position from, Position to)
    {
        (WarehouseId, Tick, RobotId) = (warehouseId, tick, robotId);
        (FromRow, FromColumn, Row, Column) = (from.Row, from.Column, to.Row, to.Column);
    }
    public Guid WarehouseId { get; private set; }
    public long Tick { get; private set; }
    public int RobotId { get; private set; }
    public int FromRow { get; private set; }
    public int FromColumn { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }
}
