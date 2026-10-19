
namespace Simulation.Models;

public class WarehouseGrid
{
    public CellType[,] Cells { get; }

    public int Rows => Cells.GetLength(0);
    public int Columns => Cells.GetLength(1);

    public WarehouseGrid(CellType[,] cells)
    {
        Cells = (CellType[,])cells.Clone();
    }

    public bool IsWalkable(int row, int column)
    {
        return row >= 0 &&
               row < Rows &&
               column >= 0 &&
               column < Columns &&
               Cells[row, column] != CellType.Shelf;
    }
}

