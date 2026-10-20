# Backend Setup Guide

This guide explains how to set up and run the Warehouse Robot Simulator backend using **C#, ASP.NET Core, Entity Framework Core, and PostgreSQL**.

## 1. Requirements

Before starting, install:

- Visual Studio Code
- .NET 9 SDK
- PostgreSQL
- Git

The C# Dev Kit extension for VS Code is recommended.

## 2. Download the Project

Clone the repository from GitHub:

```powershell
git clone https://github.com/yamenshelbayeh/warehouse-robot-simulator.git
cd warehouse-robot-simulator
code .
```

Make sure you are using the `main` branch.

## 3. Build the Backend

Open a terminal in VS Code and run:

```powershell
dotnet restore WarehouseRobotSimulator.sln --configfile NuGet.Config
dotnet tool restore --configfile NuGet.Config
dotnet build WarehouseRobotSimulator.sln --no-restore
```

This restores the required packages and builds the project.

## 4. Connect PostgreSQL

Make sure PostgreSQL is running.

In PowerShell, enter your PostgreSQL username and password:

```powershell
$user = Read-Host "PostgreSQL username"
$password = Read-Host "PostgreSQL password" -AsSecureString

$credentials = [PSCredential]::new($user, $password)

$connection = [System.Data.Common.DbConnectionStringBuilder]::new()
$connection["Host"] = "localhost"
$connection["Port"] = 5432
$connection["Database"] = "warehouse_robot_simulator"
$connection["Username"] = $user
$connection["Password"] = $credentials.GetNetworkCredential().Password

$env:ConnectionStrings__Warehouse = $connection.ConnectionString
$env:WAREHOUSE_TEST_POSTGRES = $connection.ConnectionString

Remove-Variable password, credentials, connection
```

The password is not saved in the project. The connection settings are only available in the current terminal session.

## 5. Create the Database

We use Entity Framework Core to manage the database.

Run:

```powershell
dotnet ef database update --project backend/Warehouse.Api
```

This applies the database migrations and creates the required tables.

We do not need to manually create tables using SQL scripts.

The PostgreSQL user must have permission to create the database.

## 6. Run the Tests

To run all backend and simulation tests:

```powershell
dotnet test WarehouseRobotSimulator.sln --no-restore
```

The project currently contains 41 tests covering simulation algorithms, business logic, API functionality, and database operations.

PostgreSQL integration tests require the database connection configured in Step 4 and permission to create test databases.

## 7. Start the Backend

Run:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project backend/Warehouse.Api --no-restore -- --urls http://localhost:5080
```

The API should start at:

http://localhost:5080

To check the database connection, open:

http://localhost:5080/health/ready

A successful response should show:

```json
{
  "status": "ready"
}
```

## 8. Run the Robot Delivery Demo

Keep the API running and open a second VS Code terminal.

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\backend\demo-api.ps1
```

The demo creates a warehouse, assigns a robot to a delivery task, and advances the simulation.

In the tested example, the robot completed its delivery in six simulation ticks and finished with 96% battery.

The delivery information is stored in PostgreSQL.

## 9. Notes

- The backend is built with ASP.NET Core.
- EF Core is used for database operations and migrations.
- PostgreSQL stores warehouse, robot, and task information.
- Erfan's simulation library provides A* pathfinding, robot selection, and collision checks.
- The backend handles task assignment, battery rules, movement updates, and data persistence.
- Advanced features such as automatic charging and deadlock recovery are planned for future development.
