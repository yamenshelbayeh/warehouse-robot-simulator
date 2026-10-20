
using Simulation.Algorithms;
using Xunit;

namespace Simulation.Tests;

public class CollisionAvoidanceTests
{
    [Fact]
    public void DetectsSameCellCollision()
    {
        bool collision = CollisionAvoidance.HasCollision(
            (0, 0), (0, 1),
            (0, 2), (0, 1)
        );

        Assert.True(collision);
    }

    [Fact]
    public void DetectsHeadOnSwap()
    {
        bool collision = CollisionAvoidance.HasCollision(
            (0, 0), (0, 1),
            (0, 1), (0, 0)
        );

        Assert.True(collision);
    }

    [Fact]
    public void DetectsBlockedMovement()
    {
        bool collision = CollisionAvoidance.HasCollision(
            (0, 0), (0, 1),
            (0, 1), (0, 1)
        );

        Assert.True(collision);
    }

    [Fact]
    public void AllowsSafeMovement()
    {
        bool collision = CollisionAvoidance.HasCollision(
            (0, 0), (0, 1),
            (2, 0), (2, 1)
        );

        Assert.False(collision);
    }

    [Fact]
    public void AllowsRobotToWaitSafely()
    {
        bool collision = CollisionAvoidance.HasCollision(
            (0, 0), (0, 0),
            (2, 0), (2, 0)
        );

        Assert.False(collision);
    }
}
