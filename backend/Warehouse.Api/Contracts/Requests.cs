using Simulation.Models;
using Warehouse.Api.Domain;

namespace Warehouse.Api.Contracts;

public sealed record CellInput(Position Position, CellType Type);
public sealed record CreateWarehouseRequest(string Name, int Rows, int Columns, List<CellInput>? Cells = null);
public sealed record SetCellRequest(long? ExpectedRevision, CellType Type);
public sealed record AddRobotRequest(long? ExpectedRevision, Position? Position, int Battery, int Capacity);
public sealed record SetRobotEnabledRequest(long? ExpectedRevision, bool Enabled);
public sealed record AddTaskRequest(long? ExpectedRevision, Position? Pickup, Position? Dropoff, int Load, int Priority);
public sealed record AssignTaskRequest(long? ExpectedRevision, int? RobotId = null);
public sealed record RevisionRequest(long? ExpectedRevision);
