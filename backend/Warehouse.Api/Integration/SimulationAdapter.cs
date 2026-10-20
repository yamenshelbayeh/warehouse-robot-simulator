using Simulation.Algorithms;
using Simulation.Models;
using Warehouse.Api.Domain;
using WarehouseModel = Warehouse.Api.Domain.Warehouse;

namespace Warehouse.Api.Integration;

public sealed class SimulationAdapter : ISimulationAlgorithms
{
    public IReadOnlyList<Position> FindPath(WarehouseModel warehouse, Position start, Position goal,
        IEnumerable<Position>? blocked = null)
        => AStarPathfinder.FindPath(BuildGrid(warehouse, blocked), (start.Row, start.Column), (goal.Row, goal.Column))
            .Select(p => new Position(p.Row, p.Column)).ToArray();

    public int? FindBestRobot(WarehouseModel warehouse, IEnumerable<WarehouseRobot> eligible, Position pickup)
        => TaskAssigner.FindBestRobot(BuildGrid(warehouse), eligible.Select(r => new Robot
        {
            Id = r.Id, Row = r.Row, Column = r.Column, State = r.State, CarryingItem = r.CarryingItem
        }), (pickup.Row, pickup.Column))?.Id;

    public bool HasCollision(Position current, Position next, Position otherCurrent, Position otherNext)
        => CollisionAvoidance.HasCollision((current.Row, current.Column), (next.Row, next.Column),
            (otherCurrent.Row, otherCurrent.Column), (otherNext.Row, otherNext.Column));

    private static WarehouseGrid BuildGrid(WarehouseModel warehouse, IEnumerable<Position>? blocked = null)
    {
        var cells = new CellType[warehouse.Rows, warehouse.Columns];
        foreach (var cell in warehouse.Cells)
            cells[cell.Row, cell.Column] = cell.Type;
        foreach (var position in blocked ?? [])
            cells[position.Row, position.Column] = CellType.Shelf;
        return new WarehouseGrid(cells);
    }
}
