using Simulation.Models;

namespace Warehouse.Api.Domain;

public sealed class Warehouse
{
    public const int BatteryReserve = 10;
    private Warehouse() { }
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public int Rows { get; private set; }
    public int Columns { get; private set; }
    public long Tick { get; private set; }
    public long Revision { get; private set; }
    public List<GridCell> Cells { get; private set; } = [];
    public List<WarehouseRobot> Robots { get; private set; } = [];
    public List<DeliveryTask> Tasks { get; private set; } = [];
    public List<TaskAssignment> Assignments { get; private set; } = [];
    public List<CellReservation> Reservations { get; private set; } = [];

    public static Warehouse Create(string name, int rows, int columns)
    {
        Require(!string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 100,
            "invalid_name", "Name must contain 1 to 100 characters.", 400);
        Require(rows is >= 1 and <= 50 && columns is >= 1 and <= 50,
            "invalid_dimensions", "Grid dimensions must be between 1 and 50.", 400);
        var warehouse = new Warehouse { Id = Guid.NewGuid(), Name = name.Trim(), Rows = rows, Columns = columns };
        for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
                warehouse.Cells.Add(new GridCell(warehouse.Id, new(row, column), CellType.Empty));
        return warehouse;
    }

    public void CheckRevision(long expected)
        => Require(expected == Revision, "stale_revision", "The warehouse changed. Fetch it again before retrying.");

    internal void CommitRevision() => Revision++;

    public void SetCell(Position position, CellType type)
    {
        Require(Enum.IsDefined(type), "invalid_cell_type", "Unknown cell type.", 400);
        var cell = CellAt(position);
        Require(!Assignments.Any(a => a.Active), "layout_in_use", "Finish or cancel assignments before editing the layout.");
        Require(type != CellType.Shelf || Robots.All(r => r.Position != position),
            "occupied_cell", "An obstacle cannot replace a robot's current cell.");
        Require(!Tasks.Any(t => t.Pickup == position || t.Dropoff == position) || cell.Type == type,
            "task_station_in_use", "A station referenced by a task cannot be changed.");
        cell.Type = type;
    }

    public WarehouseRobot AddRobot(Position position, int battery, int capacity)
    {
        Require(CellAt(position).Type != CellType.Shelf, "blocked_cell", "Robots must start on a walkable cell.");
        Require(Robots.All(r => r.Position != position), "occupied_cell", "This cell already contains a robot.");
        Require(battery is >= 0 and <= 100 && capacity is >= 1 and <= 1000,
            "invalid_robot", "Battery must be 0–100 and capacity 1–1000.", 400);
        Require(Robots.Count < 100, "robot_limit", "This prototype supports up to 100 robots per warehouse.");
        var robot = new WarehouseRobot(Id, Robots.Select(r => r.Id).DefaultIfEmpty().Max() + 1, position, battery, capacity);
        Robots.Add(robot);
        return robot;
    }

    public void SetRobotEnabled(int robotId, bool enabled)
    {
        var robot = RobotById(robotId);
        Require(!Assignments.Any(a => a.Active && a.RobotId == robotId),
            "robot_busy", "An assigned robot cannot be disabled.");
        robot.Enabled = enabled;
    }

    public DeliveryTask AddTask(Position pickup, Position dropoff, int load, int priority)
    {
        Require(CellAt(pickup).Type == CellType.Pickup && CellAt(dropoff).Type == CellType.Dropoff,
            "invalid_stations", "Use a Pickup cell and a Dropoff cell.", 400);
        Require(load is >= 1 and <= 1000 && priority is >= 0 and <= 10,
            "invalid_task", "Load must be 1–1000 and priority 0–10.", 400);
        var task = new DeliveryTask(Id, pickup, dropoff, load, priority);
        Tasks.Add(task);
        return task;
    }

    public TaskAssignment Assign(Guid taskId, int? requestedRobotId, ISimulationAlgorithms algorithms)
    {
        var task = TaskById(taskId);
        Require(task.Status == DeliveryStatus.Pending, "task_not_pending", "Only pending tasks may be assigned.");
        var delivery = algorithms.FindPath(this, task.Pickup, task.Dropoff);
        Require(delivery.Count > 0, "unreachable_dropoff", "No route exists from pickup to dropoff.");
        var eligible = Robots.Where(r => r.Enabled && r.State == RobotState.Idle && !r.CarryingItem
            && r.Capacity >= task.Load && !Assignments.Any(a => a.Active && a.RobotId == r.Id))
            .Where(r =>
            {
                var pickup = algorithms.FindPath(this, r.Position, task.Pickup);
                return pickup.Count > 0 && r.Battery >= pickup.Count + delivery.Count - 2 + BatteryReserve;
            }).ToList();
        if (requestedRobotId is int requested)
        {
            _ = RobotById(requested);
            Require(eligible.Any(r => r.Id == requested), "robot_ineligible",
                "Robot must be enabled, idle, reachable, have enough capacity and battery for both legs plus 10 reserve.");
        }
        var selectedId = requestedRobotId ?? algorithms.FindBestRobot(this, eligible, task.Pickup);
        Require(selectedId.HasValue, "no_eligible_robot", "No robot can perform this task with the required battery reserve.");
        var robot = RobotById(selectedId!.Value);
        var assignment = new TaskAssignment(Id, task.Id, robot.Id, Tick);
        Assignments.Add(assignment);
        task.Status = DeliveryStatus.Assigned;
        robot.State = RobotState.MovingToPickup;
        return assignment;
    }

    public void CancelTask(Guid taskId)
    {
        var task = TaskById(taskId);
        Require(task.Status is DeliveryStatus.Pending or DeliveryStatus.Assigned,
            "invalid_transition", "Only a pending task or an assignment before pickup can be cancelled.");
        var assignment = Assignments.SingleOrDefault(a => a.TaskId == task.Id && a.Active);
        if (assignment is not null)
        {
            var robot = RobotById(assignment.RobotId);
            robot.State = RobotState.Idle;
            assignment.Active = false;
            assignment.EndedAtTick = Tick;
        }
        task.Status = DeliveryStatus.Cancelled;
    }

    public void AdvanceTick(ISimulationAlgorithms algorithms)
    {
        var nextTick = Tick + 1;
        var active = Assignments.Where(a => a.Active).ToDictionary(a => a.RobotId);
        var from = Robots.ToDictionary(r => r.Id, r => r.Position);
        var accepted = new Dictionary<int, Position>();

        // Conservative reservation: current cells stay occupied for the entire tick.
        // This avoids a losing contender invalidating a follower's previously accepted move.
        foreach (var robot in Robots.OrderByDescending(r => active.TryGetValue(r.Id, out var a)
                     ? TaskById(a.TaskId).Priority : -1).ThenBy(r => r.Id))
        {
            var next = robot.Position;
            if (active.TryGetValue(robot.Id, out var assignment))
            {
                var task = TaskById(assignment.TaskId);
                if (robot.State is RobotState.MovingToPickup or RobotState.MovingToDropoff)
                {
                    var goal = robot.State == RobotState.MovingToPickup ? task.Pickup : task.Dropoff;
                    var path = algorithms.FindPath(this, robot.Position, goal,
                        Robots.Where(r => r.Id != robot.Id).Select(r => from[r.Id]));
                    if (path.Count > 1 && robot.Battery > BatteryReserve)
                        next = path[1];
                }
            }
            if (Robots.Where(r => r.Id != robot.Id).Any(other =>
                    algorithms.HasCollision(from[robot.Id], next, from[other.Id],
                        accepted.GetValueOrDefault(other.Id, from[other.Id]))))
                next = from[robot.Id];
            accepted[robot.Id] = next;
        }

        Reservations.Clear();
        foreach (var robot in Robots)
        {
            var next = accepted[robot.Id];
            Reservations.Add(new CellReservation(Id, nextTick, robot.Id, from[robot.Id], next));
            if (next != from[robot.Id])
            {
                robot.Row = next.Row;
                robot.Column = next.Column;
                robot.Battery--;
            }
            if (!active.TryGetValue(robot.Id, out var assignment)) continue;
            var task = TaskById(assignment.TaskId);
            switch (robot.State)
            {
                case RobotState.MovingToPickup when robot.Position == task.Pickup:
                    robot.State = RobotState.PickingUp;
                    break;
                case RobotState.PickingUp:
                    robot.CarryingItem = true;
                    robot.State = RobotState.MovingToDropoff;
                    task.Status = DeliveryStatus.InProgress;
                    break;
                case RobotState.MovingToDropoff when robot.Position == task.Dropoff:
                    robot.State = RobotState.DroppingOff;
                    break;
                case RobotState.DroppingOff:
                    robot.CarryingItem = false;
                    robot.State = RobotState.Idle;
                    task.Status = DeliveryStatus.Completed;
                    assignment.Active = false;
                    assignment.EndedAtTick = nextTick;
                    break;
            }
        }
        Tick = nextTick;
    }

    private GridCell CellAt(Position position)
    {
        Require(position.Row >= 0 && position.Row < Rows && position.Column >= 0 && position.Column < Columns,
            "outside_grid", "Cell coordinates are outside the warehouse.", 400);
        return Cells.Single(c => c.Position == position);
    }

    private WarehouseRobot RobotById(int id) => Robots.SingleOrDefault(r => r.Id == id)
        ?? throw new RuleViolation("robot_not_found", "Robot does not belong to this warehouse.", 404);
    private DeliveryTask TaskById(Guid id) => Tasks.SingleOrDefault(t => t.Id == id)
        ?? throw new RuleViolation("task_not_found", "Task does not belong to this warehouse.", 404);
    internal static void Require(bool condition, string code, string message, int status = 409)
    {
        if (!condition) throw new RuleViolation(code, message, status);
    }
}
