
namespace Simulation.Models;

public static class SampleWarehouse
{
    public static WarehouseGrid Create()
    {
        CellType[,] layout =
        {
            { CellType.Empty, CellType.Empty, CellType.Empty, CellType.Empty, CellType.Empty },
            { CellType.Empty, CellType.Shelf, CellType.Shelf, CellType.Shelf, CellType.Empty },
            { CellType.Empty, CellType.Empty, CellType.Empty, CellType.Empty, CellType.Empty },
            { CellType.Pickup, CellType.Empty, CellType.Shelf, CellType.Empty, CellType.Dropoff }
        };

        return new WarehouseGrid(layout);
    }
}

