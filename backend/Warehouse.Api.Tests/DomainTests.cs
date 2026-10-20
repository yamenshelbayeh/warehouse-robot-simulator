using Simulation.Models;
using Warehouse.Api.Domain;
using Warehouse.Api.Integration;
using WarehouseModel = Warehouse.Api.Domain.Warehouse;

namespace Warehouse.Api.Tests;

public sealed class DomainTests
{
    private readonly SimulationAdapter algorithms = new();

    private static WarehouseModel Corridor()
    {
        var warehouse = WarehouseModel.Create("Test", 1, 5);
        warehouse.SetCell(new(0, 1), CellType.Pickup);
        warehouse.SetCell(new(0, 4), CellType.Dropoff);
        return warehouse;
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(5, 51)]
    [InlineData(-1, 5)]
    public void InvalidDimensionsAreRejected(int rows, int columns)
        => Assert.Equal("invalid_dimensions", Assert.Throws<RuleViolation>(() =>
            WarehouseModel.Create("Test", rows, columns)).Code);

    [Fact]
    public void RobotsCannotStartOutsideGridOnShelvesOrOnAnotherRobot()
    {
        var warehouse = Corridor();
        warehouse.SetCell(new(0, 2), CellType.Shelf);
        warehouse.AddRobot(new(0, 0), 100, 5);
        Assert.Throws<RuleViolation>(() => warehouse.AddRobot(new(-1, 0), 100, 5));
        Assert.Throws<RuleViolation>(() => warehouse.AddRobot(new(0, 2), 100, 5));
        Assert.Throws<RuleViolation>(() => warehouse.AddRobot(new(0, 0), 100, 5));
    }

    [Theory]
    [InlineData(13, 5, true)] // Four moves plus ten reserve require fourteen.
    [InlineData(100, 1, true)]
    [InlineData(100, 5, false)]
    public void IneligibleRobotCannotReceiveTask(int battery, int capacity, bool enabled)
    {
        var warehouse = Corridor();
        var robot = warehouse.AddRobot(new(0, 0), battery, capacity);
        warehouse.SetRobotEnabled(robot.Id, enabled);
        var task = warehouse.AddTask(new(0, 1), new(0, 4), 2, 1);
        Assert.Equal("robot_ineligible",
            Assert.Throws<RuleViolation>(() => warehouse.Assign(task.Id, robot.Id, algorithms)).Code);
        Assert.Equal(DeliveryStatus.Pending, task.Status);
        Assert.Empty(warehouse.Assignments);
    }

    [Fact]
    public void ExactBatteryBudgetIsAcceptedAndReserveIsPreserved()
    {
        var warehouse = Corridor();
        var robot = warehouse.AddRobot(new(0, 0), 14, 5);
        var task = warehouse.AddTask(new(0, 1), new(0, 4), 2, 1);
        warehouse.Assign(task.Id, robot.Id, algorithms);
        for (var i = 0; i < 6; i++) warehouse.AdvanceTick(algorithms);
        Assert.Equal(DeliveryStatus.Completed, task.Status);
        Assert.Equal(10, robot.Battery);
    }

    [Fact]
    public void AutomaticAssignmentUsesErfansTieBreakerAfterEligibilityFiltering()
    {
        var warehouse = WarehouseModel.Create("Tie", 3, 3);
        warehouse.SetCell(new(1, 1), CellType.Pickup);
        warehouse.SetCell(new(2, 2), CellType.Dropoff);
        var first = warehouse.AddRobot(new(1, 0), 100, 2);
        warehouse.AddRobot(new(0, 1), 100, 2);
        warehouse.AddRobot(new(1, 1), 0, 2); // Closest robot is ineligible.
        var task = warehouse.AddTask(new(1, 1), new(2, 2), 1, 0);
        Assert.Equal(first.Id, warehouse.Assign(task.Id, null, algorithms).RobotId);
    }

    [Fact]
    public void BothPickupAndDropoffMustBeReachable()
    {
        var warehouse = Corridor();
        warehouse.SetCell(new(0, 3), CellType.Shelf);
        warehouse.AddRobot(new(0, 0), 100, 5);
        var task = warehouse.AddTask(new(0, 1), new(0, 4), 1, 0);
        Assert.Equal("unreachable_dropoff",
            Assert.Throws<RuleViolation>(() => warehouse.Assign(task.Id, null, algorithms)).Code);

        warehouse.SetCell(new(0, 3), CellType.Empty);
        warehouse.SetCell(new(0, 0), CellType.Empty);
        var isolated = WarehouseModel.Create("Isolated", 1, 4);
        isolated.SetCell(new(0, 1), CellType.Shelf);
        isolated.SetCell(new(0, 2), CellType.Pickup);
        isolated.SetCell(new(0, 3), CellType.Dropoff);
        isolated.AddRobot(new(0, 0), 100, 5);
        var unreachable = isolated.AddTask(new(0, 2), new(0, 3), 1, 0);
        Assert.Equal("no_eligible_robot",
            Assert.Throws<RuleViolation>(() => isolated.Assign(unreachable.Id, null, algorithms)).Code);
    }

    [Fact]
    public void AssignmentCannotBeDuplicatedAndBusyRobotCannotTakeAnotherTask()
    {
        var warehouse = Corridor();
        var robot = warehouse.AddRobot(new(0, 0), 100, 5);
        var first = warehouse.AddTask(new(0, 1), new(0, 4), 1, 0);
        var second = warehouse.AddTask(new(0, 1), new(0, 4), 1, 0);
        warehouse.Assign(first.Id, robot.Id, algorithms);
        Assert.Equal("task_not_pending", Assert.Throws<RuleViolation>(() =>
            warehouse.Assign(first.Id, robot.Id, algorithms)).Code);
        Assert.Equal("robot_ineligible", Assert.Throws<RuleViolation>(() =>
            warehouse.Assign(second.Id, robot.Id, algorithms)).Code);
        Assert.Throws<RuleViolation>(() => warehouse.SetRobotEnabled(robot.Id, false));
        Assert.Throws<RuleViolation>(() => warehouse.SetCell(new(0, 2), CellType.Shelf));
    }

    [Fact]
    public void DeliveryTransitionsFollowPhysicalArrivalAndPickup()
    {
        var warehouse = Corridor();
        var robot = warehouse.AddRobot(new(0, 0), 100, 5);
        var task = warehouse.AddTask(new(0, 1), new(0, 4), 1, 0);
        var assignment = warehouse.Assign(task.Id, robot.Id, algorithms);
        warehouse.AdvanceTick(algorithms);
        Assert.Equal(new Position(0, 1), robot.Position);
        Assert.Equal(RobotState.PickingUp, robot.State);
        Assert.Equal(DeliveryStatus.Assigned, task.Status);
        warehouse.AdvanceTick(algorithms);
        Assert.Equal(DeliveryStatus.InProgress, task.Status);
        Assert.True(robot.CarryingItem);
        Assert.Equal("invalid_transition", Assert.Throws<RuleViolation>(() => warehouse.CancelTask(task.Id)).Code);
        for (var i = 0; i < 3; i++) warehouse.AdvanceTick(algorithms);
        Assert.Equal(RobotState.DroppingOff, robot.State);
        Assert.Equal(DeliveryStatus.InProgress, task.Status);
        warehouse.AdvanceTick(algorithms);
        Assert.Equal(DeliveryStatus.Completed, task.Status);
        Assert.Equal(RobotState.Idle, robot.State);
        Assert.False(robot.CarryingItem);
        Assert.False(assignment.Active);
        Assert.Equal(6, assignment.EndedAtTick);
        Assert.Throws<RuleViolation>(() => warehouse.Assign(task.Id, null, algorithms));
    }

    [Fact]
    public void CancellationReleasesRobotButKeepsAssignmentHistory()
    {
        var warehouse = Corridor();
        var robot = warehouse.AddRobot(new(0, 0), 100, 5);
        var task = warehouse.AddTask(new(0, 1), new(0, 4), 1, 0);
        warehouse.Assign(task.Id, robot.Id, algorithms);
        warehouse.CancelTask(task.Id);
        Assert.Equal(DeliveryStatus.Cancelled, task.Status);
        Assert.Equal(RobotState.Idle, robot.State);
        Assert.False(Assert.Single(warehouse.Assignments).Active);
        Assert.Throws<RuleViolation>(() => warehouse.CancelTask(task.Id));
        var next = warehouse.AddTask(new(0, 1), new(0, 4), 1, 0);
        warehouse.Assign(next.Id, robot.Id, algorithms);
        Assert.Equal(2, warehouse.Assignments.Count);
    }

    [Fact]
    public void PriorityResolvesSameCellConflictAndWaitingKeepsBattery()
    {
        var warehouse = WarehouseModel.Create("Crossing", 3, 3);
        warehouse.SetCell(new(1, 2), CellType.Pickup);
        warehouse.SetCell(new(2, 1), CellType.Pickup);
        warehouse.SetCell(new(2, 2), CellType.Dropoff);
        warehouse.SetCell(new(2, 0), CellType.Dropoff);
        var low = warehouse.AddRobot(new(1, 0), 100, 5);
        var high = warehouse.AddRobot(new(0, 1), 100, 5);
        var task1 = warehouse.AddTask(new(1, 2), new(2, 2), 1, 0);
        var task2 = warehouse.AddTask(new(2, 1), new(2, 0), 1, 10);
        warehouse.Assign(task1.Id, low.Id, algorithms);
        warehouse.Assign(task2.Id, high.Id, algorithms);
        warehouse.AdvanceTick(algorithms);
        Assert.Equal(new Position(1, 1), high.Position);
        Assert.Equal(new Position(1, 0), low.Position);
        Assert.Equal(100, low.Battery);
        Assert.Equal(2, warehouse.Reservations.Select(r => (r.Row, r.Column)).Distinct().Count());
    }

    [Fact]
    public void HeadOnSwapWaitsAndReservesBothOccupiedCells()
    {
        var warehouse = WarehouseModel.Create("Swap", 1, 4);
        warehouse.SetCell(new(0, 0), CellType.Dropoff);
        warehouse.SetCell(new(0, 1), CellType.Pickup);
        warehouse.SetCell(new(0, 2), CellType.Pickup);
        warehouse.SetCell(new(0, 3), CellType.Dropoff);
        var a = warehouse.AddRobot(new(0, 1), 100, 5);
        var b = warehouse.AddRobot(new(0, 2), 100, 5);
        warehouse.Assign(warehouse.AddTask(new(0, 2), new(0, 3), 1, 0).Id, a.Id, algorithms);
        warehouse.Assign(warehouse.AddTask(new(0, 1), new(0, 0), 1, 0).Id, b.Id, algorithms);
        warehouse.AdvanceTick(algorithms);
        Assert.Equal(new Position(0, 1), a.Position);
        Assert.Equal(new Position(0, 2), b.Position);
        Assert.All(warehouse.Reservations, r => Assert.Equal((r.FromRow, r.FromColumn), (r.Row, r.Column)));
    }

    [Fact]
    public void MovingRobotReroutesAroundIdleRobotUsingExistingAStar()
    {
        var warehouse = WarehouseModel.Create("Detour", 3, 4);
        warehouse.SetCell(new(1, 2), CellType.Pickup);
        warehouse.SetCell(new(1, 3), CellType.Dropoff);
        var moving = warehouse.AddRobot(new(1, 0), 100, 5);
        var idle = warehouse.AddRobot(new(1, 1), 100, 5);
        var task = warehouse.AddTask(new(1, 2), new(1, 3), 1, 0);
        warehouse.Assign(task.Id, moving.Id, algorithms);
        for (var i = 0; i < 12; i++)
        {
            var before = warehouse.Robots.ToDictionary(r => r.Id, r => r.Position);
            warehouse.AdvanceTick(algorithms);
            Assert.Equal(2, warehouse.Robots.Select(r => r.Position).Distinct().Count());
            Assert.False(algorithms.HasCollision(before[moving.Id], moving.Position, before[idle.Id], idle.Position));
            Assert.Equal(2, warehouse.Reservations.Count);
            Assert.All(warehouse.Reservations, r => Assert.Equal(warehouse.Tick, r.Tick));
        }
        Assert.Equal(DeliveryStatus.Completed, task.Status);
        Assert.Equal(new Position(1, 1), idle.Position);
        Assert.Equal(95, moving.Battery);
    }

    [Fact]
    public void ForeignTaskAndForeignRobotAreRejected()
    {
        var warehouse = Corridor();
        var other = Corridor();
        var task = warehouse.AddTask(new(0, 1), new(0, 4), 1, 0);
        Assert.Equal("task_not_found", Assert.Throws<RuleViolation>(() =>
            other.Assign(task.Id, null, algorithms)).Code);
        Assert.Equal("robot_not_found", Assert.Throws<RuleViolation>(() =>
            warehouse.Assign(task.Id, 999, algorithms)).Code);
    }
}
