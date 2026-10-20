
namespace Simulation.Algorithms;

public static class CollisionAvoidance
{
    public static bool HasCollision(
        (int Row, int Column) robot1Current,
        (int Row, int Column) robot1Next,
        (int Row, int Column) robot2Current,
        (int Row, int Column) robot2Next)
    {
        // Both robots want the same cell
        if (robot1Next == robot2Next)
            return true;

        // Robots attempt to swap positions
        if (robot1Next == robot2Current &&
            robot2Next == robot1Current)
            return true;

        return false;
    }
}
