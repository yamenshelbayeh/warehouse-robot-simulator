
namespace Simulation.Models;

public class Robot
{
    public int Id { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }

    public RobotState State { get; set; } = RobotState.Idle;

    public bool CarryingItem { get; set; } = false;
}

