
using Simulation.Models;
using Simulation.Algorithms;

var grid = SampleWarehouse.Create();

var start = (Row: 0, Column: 0);
var goal = (Row: 3, Column: 4);

var path = AStarPathfinder.FindPath(
    grid, start, goal);

Console.WriteLine("WAREHOUSE A* DEMO");
Console.WriteLine();

for (int row = 0; row < grid.Rows; row++)
{
    for (int col = 0; col < grid.Columns; col++)
    {
        var position = (Row: row, Column: col);

        char symbol = grid.Cells[row, col] switch
        {
            CellType.Shelf => '#',
            CellType.Pickup => 'P',
            CellType.Dropoff => 'D',
            _ => '.'
        };

        if (path.Contains(position))
            symbol = '*';

        if (position == start)
            symbol = 'R';

        if (position == goal)
            symbol = 'G';

        Console.Write(symbol + " ");
    }

    Console.WriteLine();
}

Console.WriteLine();
Console.WriteLine(
    path.Count > 0
        ? $"Path found! Steps: {path.Count - 1}"
        : "No valid path found.");
