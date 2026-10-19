
using Simulation.Models;

namespace Simulation.Algorithms;

public static class TaskAssigner
{
    public static Robot? FindBestRobot(
        WarehouseGrid grid,
        IEnumerable<Robot> robots,
        (int Row, int Column) pickup)
    {
        Robot? bestRobot = null;
        int shortestDistance = int.MaxValue;

        foreach (var robot in robots.OrderBy(r => r.Id))
        {
            if (robot.State != RobotState.Idle)
                continue;

            var path = AStarPathfinder.FindPath(
                grid,
                (robot.Row, robot.Column),
                pickup
            );

            if (path.Count == 0)
                continue;

            int distance = path.Count - 1;

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                bestRobot = robot;
            }
        }

        return bestRobot;
    }
}

