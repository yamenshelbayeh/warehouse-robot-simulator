
using Simulation.Models;

namespace Simulation.Algorithms;

public static class AStarPathfinder
{
    public static List<(int Row, int Column)> FindPath(
        WarehouseGrid grid,
        (int Row, int Column) start,
        (int Row, int Column) goal)
    {
        var open = new PriorityQueue<
            (int Row, int Column), int>();

        var cameFrom = new Dictionary<
            (int Row, int Column),
            (int Row, int Column)>();

        var gScore = new Dictionary<
            (int Row, int Column), int>
        {
            [start] = 0
        };

        var directions = new[]
        {
            (-1, 0), (1, 0),
            (0, -1), (0, 1)
        };

        if (!grid.IsWalkable(start.Row, start.Column) ||
            !grid.IsWalkable(goal.Row, goal.Column))
            return new();

        open.Enqueue(start, Heuristic(start, goal));

        while (open.Count > 0)
        {
            var current = open.Dequeue();

            if (current == goal)
            {
                var path = new List<(int Row, int Column)>
                {
                    current
                };

                while (cameFrom.TryGetValue(
                    current, out var previous))
                {
                    current = previous;
                    path.Add(current);
                }

                path.Reverse();
                return path;
            }

            foreach (var (dr, dc) in directions)
            {
                var next = (
                    Row: current.Row + dr,
                    Column: current.Column + dc
                );

                if (!grid.IsWalkable(next.Row, next.Column))
                    continue;

                int tentative = gScore[current] + 1;

                if (!gScore.TryGetValue(
                    next, out int oldScore) ||
                    tentative < oldScore)
                {
                    cameFrom[next] = current;
                    gScore[next] = tentative;

                    int priority =
                        tentative + Heuristic(next, goal);

                    open.Enqueue(next, priority);
                }
            }
        }

        return new();
    }

    private static int Heuristic(
        (int Row, int Column) a,
        (int Row, int Column) b)
    {
        return Math.Abs(a.Row - b.Row) +
               Math.Abs(a.Column - b.Column);
    }
}
