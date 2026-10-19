
using Simulation.Algorithms;
using Simulation.Models;
using Xunit;

namespace Simulation.Tests;

public class TaskAssignerTests
{
    [Fact]
    public void SelectsNearestIdleRobot()
    {
        var grid = SampleWarehouse.Create();

        var robots = new List<Robot>
        {
            new() { Id = 1, Row = 0, Column = 0 },
            new() { Id = 2, Row = 3, Column = 1 },
            new() { Id = 3, Row = 2, Column = 4 }
        };

        var selected = TaskAssigner.FindBestRobot(
            grid, robots, (3, 0));

        Assert.NotNull(selected);
        Assert.Equal(2, selected.Id);
    }

    [Fact]
    public void IgnoresBusyRobots()
    {
        var grid = SampleWarehouse.Create();

        var robots = new List<Robot>
        {
            new()
            {
                Id = 1,
                Row = 3,
                Column = 1,
                State = RobotState.MovingToPickup
            },
            new()
            {
                Id = 2,
                Row = 0,
                Column = 0
            }
        };

        var selected = TaskAssigner.FindBestRobot(
            grid, robots, (3, 0));

        Assert.NotNull(selected);
        Assert.Equal(2, selected.Id);
    }

    [Fact]
    public void ReturnsNullWhenAllRobotsAreBusy()
    {
        var grid = SampleWarehouse.Create();

        var robots = new List<Robot>
        {
            new()
            {
                Id = 1,
                Row = 0,
                Column = 0,
                State = RobotState.MovingToPickup
            }
        };

        var selected = TaskAssigner.FindBestRobot(
            grid, robots, (3, 0));

        Assert.Null(selected);
    }

    [Fact]
    public void BreaksTiesUsingLowestRobotId()
    {
        var grid = SampleWarehouse.Create();

        var robots = new List<Robot>
        {
            new() { Id = 5, Row = 2, Column = 0 },
            new() { Id = 2, Row = 3, Column = 1 }
        };

        var selected = TaskAssigner.FindBestRobot(
            grid, robots, (3, 0));

        Assert.NotNull(selected);
        Assert.Equal(2, selected.Id);
    }
}
