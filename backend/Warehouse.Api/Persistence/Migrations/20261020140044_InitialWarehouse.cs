using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Warehouse.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialWarehouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Warehouses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rows = table.Column<int>(type: "integer", nullable: false),
                    Columns = table.Column<int>(type: "integer", nullable: false),
                    Tick = table.Column<long>(type: "bigint", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GridCell",
                columns: table => new
                {
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Row = table.Column<int>(type: "integer", nullable: false),
                    Column = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GridCell", x => new { x.WarehouseId, x.Row, x.Column });
                    table.ForeignKey(
                        name: "FK_GridCell_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryTask",
                columns: table => new
                {
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PickupRow = table.Column<int>(type: "integer", nullable: false),
                    PickupColumn = table.Column<int>(type: "integer", nullable: false),
                    DropoffRow = table.Column<int>(type: "integer", nullable: false),
                    DropoffColumn = table.Column<int>(type: "integer", nullable: false),
                    Load = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryTask", x => new { x.WarehouseId, x.Id });
                    table.ForeignKey(
                        name: "FK_DeliveryTask_GridCell_WarehouseId_DropoffRow_DropoffColumn",
                        columns: x => new { x.WarehouseId, x.DropoffRow, x.DropoffColumn },
                        principalTable: "GridCell",
                        principalColumns: new[] { "WarehouseId", "Row", "Column" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryTask_GridCell_WarehouseId_PickupRow_PickupColumn",
                        columns: x => new { x.WarehouseId, x.PickupRow, x.PickupColumn },
                        principalTable: "GridCell",
                        principalColumns: new[] { "WarehouseId", "Row", "Column" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryTask_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseRobot",
                columns: table => new
                {
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Row = table.Column<int>(type: "integer", nullable: false),
                    Column = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Battery = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    CarryingItem = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseRobot", x => new { x.WarehouseId, x.Id });
                    table.ForeignKey(
                        name: "FK_WarehouseRobot_GridCell_WarehouseId_Row_Column",
                        columns: x => new { x.WarehouseId, x.Row, x.Column },
                        principalTable: "GridCell",
                        principalColumns: new[] { "WarehouseId", "Row", "Column" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseRobot_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CellReservation",
                columns: table => new
                {
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tick = table.Column<long>(type: "bigint", nullable: false),
                    Row = table.Column<int>(type: "integer", nullable: false),
                    Column = table.Column<int>(type: "integer", nullable: false),
                    RobotId = table.Column<int>(type: "integer", nullable: false),
                    FromRow = table.Column<int>(type: "integer", nullable: false),
                    FromColumn = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CellReservation", x => new { x.WarehouseId, x.Tick, x.Row, x.Column });
                    table.ForeignKey(
                        name: "FK_CellReservation_GridCell_WarehouseId_FromRow_FromColumn",
                        columns: x => new { x.WarehouseId, x.FromRow, x.FromColumn },
                        principalTable: "GridCell",
                        principalColumns: new[] { "WarehouseId", "Row", "Column" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CellReservation_GridCell_WarehouseId_Row_Column",
                        columns: x => new { x.WarehouseId, x.Row, x.Column },
                        principalTable: "GridCell",
                        principalColumns: new[] { "WarehouseId", "Row", "Column" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CellReservation_WarehouseRobot_WarehouseId_RobotId",
                        columns: x => new { x.WarehouseId, x.RobotId },
                        principalTable: "WarehouseRobot",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CellReservation_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskAssignment",
                columns: table => new
                {
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAtTick = table.Column<long>(type: "bigint", nullable: false),
                    EndedAtTick = table.Column<long>(type: "bigint", nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAssignment", x => new { x.WarehouseId, x.Id });
                    table.ForeignKey(
                        name: "FK_TaskAssignment_DeliveryTask_WarehouseId_TaskId",
                        columns: x => new { x.WarehouseId, x.TaskId },
                        principalTable: "DeliveryTask",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskAssignment_WarehouseRobot_WarehouseId_RobotId",
                        columns: x => new { x.WarehouseId, x.RobotId },
                        principalTable: "WarehouseRobot",
                        principalColumns: new[] { "WarehouseId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskAssignment_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CellReservation_WarehouseId_FromRow_FromColumn",
                table: "CellReservation",
                columns: new[] { "WarehouseId", "FromRow", "FromColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_CellReservation_WarehouseId_RobotId",
                table: "CellReservation",
                columns: new[] { "WarehouseId", "RobotId" });

            migrationBuilder.CreateIndex(
                name: "IX_CellReservation_WarehouseId_Row_Column",
                table: "CellReservation",
                columns: new[] { "WarehouseId", "Row", "Column" });

            migrationBuilder.CreateIndex(
                name: "IX_CellReservation_WarehouseId_Tick_RobotId",
                table: "CellReservation",
                columns: new[] { "WarehouseId", "Tick", "RobotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryTask_WarehouseId_DropoffRow_DropoffColumn",
                table: "DeliveryTask",
                columns: new[] { "WarehouseId", "DropoffRow", "DropoffColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryTask_WarehouseId_PickupRow_PickupColumn",
                table: "DeliveryTask",
                columns: new[] { "WarehouseId", "PickupRow", "PickupColumn" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignment_WarehouseId_RobotId",
                table: "TaskAssignment",
                columns: new[] { "WarehouseId", "RobotId" },
                unique: true,
                filter: "\"Active\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_TaskAssignment_WarehouseId_TaskId",
                table: "TaskAssignment",
                columns: new[] { "WarehouseId", "TaskId" },
                unique: true,
                filter: "\"Active\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseRobot_WarehouseId_Row_Column",
                table: "WarehouseRobot",
                columns: new[] { "WarehouseId", "Row", "Column" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CellReservation");

            migrationBuilder.DropTable(
                name: "TaskAssignment");

            migrationBuilder.DropTable(
                name: "DeliveryTask");

            migrationBuilder.DropTable(
                name: "WarehouseRobot");

            migrationBuilder.DropTable(
                name: "GridCell");

            migrationBuilder.DropTable(
                name: "Warehouses");
        }
    }
}
