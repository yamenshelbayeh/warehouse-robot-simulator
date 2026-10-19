
using Simulation.Algorithms;
using Simulation.Models;
using Xunit;

namespace Simulation.Tests;

public class AStarPathfinderTests
{
    [Fact]
    public void FindsPathThroughEmptyGrid()
    {
        var grid = new WarehouseGrid(new CellType[,]
        {
            { CellType.Empty, CellType.Empty, CellType.Empty },
            { CellType.Empty, CellType.Empty, CellType.Empty },
            { CellType.Empty, CellType.Empty, CellType.Empty }
        });

        var path = AStarPathfinder.FindPath(
            grid, (0, 0), (2, 2));

        Assert.Equal(5, path.Count);
        Assert.Equal((0, 0), path[0]);
        Assert.Equal((2, 2), path[^1]);
    }

    [Fact]
    public void AvoidsShelves()
    {
        var grid = SampleWarehouse.Create();

        var path = AStarPathfinder.FindPath(
            grid, (0, 0), (3, 4));

        Assert.NotEmpty(path);

        foreach (var cell in path)
        {
            Assert.True(
                grid.IsWalkable(cell.Row, cell.Column));
        }
    }

    [Fact]
    public void ReturnsEmptyPathWhenGoalIsBlocked()
    {
        var grid = SampleWarehouse.Create();

        var path = AStarPathfinder.FindPath(
            grid, (0, 0), (1, 1));

        Assert.Empty(path);
    }

    [Fact]
    public void StartEqualsGoalReturnsSingleCell()
    {
        var grid = SampleWarehouse.Create();

        var path = AStarPathfinder.FindPath(
            grid, (0, 0), (0, 0));

        Assert.Single(path);
        Assert.Equal((0, 0), path[0]);
    }
    
    [Fact]
    public void ReturnsEmptyPathWhenGoalIsUnreachable()
    {
        var grid = new WarehouseGrid(new CellType[,]
        {
            { CellType.Empty, CellType.Shelf, CellType.Empty },
            { CellType.Shelf, CellType.Shelf, CellType.Empty },
            { CellType.Empty, CellType.Empty, CellType.Empty }
        });

        var path = AStarPathfinder.FindPath(
            grid, (0, 0), (2, 2));

        Assert.Empty(path);
    }

}
