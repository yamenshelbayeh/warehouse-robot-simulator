
using Simulation.Models;
using Xunit;

namespace Simulation.Tests;

public class WarehouseGridTests
{
    [Fact]
    public void EmptyCell_ShouldBeWalkable()
    {
        var warehouse = SampleWarehouse.Create();

        Assert.True(warehouse.IsWalkable(0, 0));
    }

    [Fact]
    public void Shelf_ShouldNotBeWalkable()
    {
        var warehouse = SampleWarehouse.Create();

        Assert.False(warehouse.IsWalkable(1, 1));
    }

    [Fact]
    public void OutsideGrid_ShouldNotBeWalkable()
    {
        var warehouse = SampleWarehouse.Create();

        Assert.False(warehouse.IsWalkable(-1, 0));
        Assert.False(warehouse.IsWalkable(10, 10));
    }
}
