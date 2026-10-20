using Microsoft.EntityFrameworkCore;
using Warehouse.Api.Domain;
using WarehouseModel = Warehouse.Api.Domain.Warehouse;

namespace Warehouse.Api.Persistence;

public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<WarehouseModel> Warehouses => Set<WarehouseModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var warehouse = modelBuilder.Entity<WarehouseModel>();
        warehouse.HasKey(w => w.Id);
        warehouse.Property(w => w.Id).ValueGeneratedNever();
        warehouse.Property(w => w.Name).HasMaxLength(100).IsRequired();
        warehouse.Property(w => w.Revision).IsConcurrencyToken();
        warehouse.HasMany(w => w.Cells).WithOne().HasForeignKey(c => c.WarehouseId);
        warehouse.HasMany(w => w.Robots).WithOne().HasForeignKey(r => r.WarehouseId);
        warehouse.HasMany(w => w.Tasks).WithOne().HasForeignKey(t => t.WarehouseId);
        warehouse.HasMany(w => w.Assignments).WithOne().HasForeignKey(a => a.WarehouseId);
        warehouse.HasMany(w => w.Reservations).WithOne().HasForeignKey(r => r.WarehouseId);

        var cell = modelBuilder.Entity<GridCell>();
        cell.HasKey(c => new { c.WarehouseId, c.Row, c.Column });
        cell.Ignore(c => c.Position);
        cell.Property(c => c.Type).HasConversion<string>().HasMaxLength(20);

        var robot = modelBuilder.Entity<WarehouseRobot>();
        robot.HasKey(r => new { r.WarehouseId, r.Id });
        robot.Property(r => r.Id).ValueGeneratedNever();
        robot.Ignore(r => r.Position);
        robot.Property(r => r.State).HasConversion<string>().HasMaxLength(30);
        robot.HasIndex(r => new { r.WarehouseId, r.Row, r.Column }).IsUnique();
        robot.HasOne<GridCell>().WithMany()
            .HasForeignKey(r => new { r.WarehouseId, r.Row, r.Column }).OnDelete(DeleteBehavior.Restrict);

        var task = modelBuilder.Entity<DeliveryTask>();
        task.HasKey(t => new { t.WarehouseId, t.Id });
        task.Property(t => t.Id).ValueGeneratedNever();
        task.Ignore(t => t.Pickup);
        task.Ignore(t => t.Dropoff);
        task.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        task.HasOne<GridCell>().WithMany()
            .HasForeignKey(t => new { t.WarehouseId, t.PickupRow, t.PickupColumn }).OnDelete(DeleteBehavior.Restrict);
        task.HasOne<GridCell>().WithMany()
            .HasForeignKey(t => new { t.WarehouseId, t.DropoffRow, t.DropoffColumn }).OnDelete(DeleteBehavior.Restrict);

        var assignment = modelBuilder.Entity<TaskAssignment>();
        assignment.HasKey(a => new { a.WarehouseId, a.Id });
        assignment.Property(a => a.Id).ValueGeneratedNever();
        assignment.HasOne<DeliveryTask>().WithMany()
            .HasForeignKey(a => new { a.WarehouseId, Id = a.TaskId }).OnDelete(DeleteBehavior.Restrict);
        assignment.HasOne<WarehouseRobot>().WithMany()
            .HasForeignKey(a => new { a.WarehouseId, Id = a.RobotId }).OnDelete(DeleteBehavior.Restrict);
        assignment.HasIndex(a => new { a.WarehouseId, a.RobotId }).IsUnique().HasFilter("\"Active\" = TRUE");
        assignment.HasIndex(a => new { a.WarehouseId, a.TaskId }).IsUnique().HasFilter("\"Active\" = TRUE");

        var reservation = modelBuilder.Entity<CellReservation>();
        reservation.HasKey(r => new { r.WarehouseId, r.Tick, r.Row, r.Column });
        reservation.HasIndex(r => new { r.WarehouseId, r.Tick, r.RobotId }).IsUnique();
        reservation.HasOne<WarehouseRobot>().WithMany()
            .HasForeignKey(r => new { r.WarehouseId, Id = r.RobotId }).OnDelete(DeleteBehavior.Restrict);
        reservation.HasOne<GridCell>().WithMany()
            .HasForeignKey(r => new { r.WarehouseId, r.Row, r.Column }).OnDelete(DeleteBehavior.Restrict);
        reservation.HasOne<GridCell>().WithMany()
            .HasForeignKey(r => new { r.WarehouseId, Row = r.FromRow, Column = r.FromColumn })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
