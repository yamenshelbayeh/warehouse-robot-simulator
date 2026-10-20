namespace Warehouse.Api.Domain;

// The adapter is the only place that calls the simulation algorithms.
public interface ISimulationAlgorithms
{
    IReadOnlyList<Position> FindPath(Warehouse warehouse, Position start, Position goal,
        IEnumerable<Position>? blocked = null);
    int? FindBestRobot(Warehouse warehouse, IEnumerable<WarehouseRobot> eligible, Position pickup);
    bool HasCollision(Position current, Position next, Position otherCurrent, Position otherNext);
}
