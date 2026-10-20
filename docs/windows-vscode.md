# Run Yamen's backend on Windows 11 with VS Code

These steps use the PostgreSQL installation already on Yamen's computer.
Verification found .NET SDK 9.0.314, VS Code and a running `postgresql-x64-18`
Windows service. Docker is not required for these steps.

## 1. Open the Yamen branch

The existing local checkout is:

```powershell
code "C:\Users\acer\.codex\.chatgpt-projects\g-p-6ac1117fa50081919c38083e1074d779\warehouse-robot-simulator"
```

Alternatively, make a fresh checkout from GitHub:

```powershell
New-Item -ItemType Directory -Force "$env:USERPROFILE\source" | Out-Null
git clone --branch codex/yamen-backend https://github.com/yamenshelbayeh/warehouse-robot-simulator.git "$env:USERPROFILE\source\warehouse-robot-simulator-yamen"
Set-Location "$env:USERPROFILE\source\warehouse-robot-simulator-yamen"
code .
```

In VS Code, choose **Terminal → New Terminal** and select **PowerShell**. All
commands below run in the repository root, where `WarehouseRobotSimulator.sln` is.
The Microsoft **C# Dev Kit** extension is optional for editor navigation and
debugging; the terminal commands work without it.

```powershell
git branch --show-current
dotnet --list-sdks
Get-Service postgresql-x64-18
```

Expect branch `codex/yamen-backend`, a `9.0.x` SDK and a `Running` database service.
If the service is stopped, start it from the Windows **Services** application.

## 2. Restore packages and the migration tool

```powershell
dotnet restore WarehouseRobotSimulator.sln --configfile NuGet.Config
dotnet tool restore --configfile NuGet.Config
dotnet build WarehouseRobotSimulator.sln --no-restore
```

## 3. Configure the local PostgreSQL connection

Use the PostgreSQL account/password set during installation, **not** your Windows
or GitHub password. The usual installation account is `postgres`. The role must be
able to create databases for the initial migration and the integration tests.

Run this whole block in the same terminal you will use to start the API:

```powershell
$databaseUser = Read-Host 'PostgreSQL username (usually postgres)'
$databasePassword = Read-Host 'PostgreSQL password' -AsSecureString
$databaseCredential = [System.Management.Automation.PSCredential]::new($databaseUser, $databasePassword)
$databaseSettings = [System.Data.Common.DbConnectionStringBuilder]::new()
$databaseSettings['Host'] = 'localhost'
$databaseSettings['Port'] = 5432
$databaseSettings['Database'] = 'warehouse_robot_simulator_yamen'
$databaseSettings['Username'] = $databaseUser
$databaseSettings['Password'] = $databaseCredential.GetNetworkCredential().Password
$env:ConnectionStrings__Warehouse = $databaseSettings.ConnectionString
$env:WAREHOUSE_TEST_POSTGRES = $env:ConnectionStrings__Warehouse
Remove-Variable databasePassword, databaseCredential, databaseSettings
```

The builder quotes special characters in passwords correctly. The connection stays
in this terminal's environment and is passed to the API/tests. It is not written
to the repository. Repeat the block in any new terminal that needs database access.

## 4. Apply the EF Core migration and run every test

```powershell
dotnet ef database update --project backend/Warehouse.Api
dotnet ef migrations has-pending-model-changes --project backend/Warehouse.Api
dotnet test WarehouseRobotSimulator.sln --no-restore
dotnet run --project backend/Simulation.Demo --no-restore
```

The first command creates the dedicated `warehouse_robot_simulator_yamen` database
if absent and applies the generated migration. No SQL files need to be executed.
Expect **41 passed, 0 skipped**, no pending model changes, and the A* demo to report
**7 steps**. If 7 tests are skipped, `WAREHOUSE_TEST_POSTGRES` was not set in that
terminal. Integration tests create and delete their own randomly named databases;
they preserve your application database.

## 5. Start the API

In that same terminal:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project backend/Warehouse.Api --no-restore -- --urls http://localhost:5080
```

Wait for `Now listening on: http://localhost:5080`. Keep the terminal running.

In a **second VS Code PowerShell terminal**, run:

```powershell
Invoke-RestMethod http://localhost:5080/health/ready
powershell -NoProfile -ExecutionPolicy Bypass -File .\backend\demo-api.ps1
```

The execution-policy flag applies only to this child process; it does not change
your Windows execution policy. The demo is the checked-in script you can inspect
at `backend/demo-api.ps1`.

Expected output:

```text
Tick 1: robot=PickingUp, task=Assigned, battery=99
Tick 2: robot=MovingToDropoff, task=InProgress, battery=99
Tick 3: robot=MovingToDropoff, task=InProgress, battery=98
Tick 4: robot=MovingToDropoff, task=InProgress, battery=97
Tick 5: robot=DroppingOff, task=InProgress, battery=96
Tick 6: robot=Idle, task=Completed, battery=96
```

The last line prints the saved warehouse's URL. Open it in your browser to inspect
the state. Useful endpoints:

- `http://localhost:5080/health/ready`
- `http://localhost:5080/api/warehouses/`
- `http://localhost:5080/openapi/v1.json`

Press **Ctrl+C** in the API terminal to stop it. Saved data remains in PostgreSQL.
This milestone exposes an API and console demo; the React frontend is still a
separate team task.

## Troubleshooting

- **Password authentication failed:** rerun the connection block with the correct
  PostgreSQL account/password. The Compose example's `warehouse/warehouse` values
  do not configure an existing Windows PostgreSQL installation.
- **Connection refused:** check the PostgreSQL service and port. Change the
  connection block if you installed it on a different port.
- **Permission denied to create database:** use your local PostgreSQL installation
  administrator for this development setup, or ask its administrator to grant the
  development role database-creation permission through their normal tooling.
- **Port 5080 occupied:** start with `--urls http://localhost:5081` and run the demo
  with `-BaseUrl http://localhost:5081`.
- **`dotnet` or `code` not recognized:** reopen the terminal after installing the
  SDK/editor. You can open the repository using VS Code's **File → Open Folder**.

## Pull request dependency

Yamen's branch starts from Erfan's simulation branch. The backend PR targets
`erfan/simulation-algorithms` to keep its diff limited to backend work. Merge
[Erfan's PR #7](https://github.com/yamenshelbayeh/warehouse-robot-simulator/pull/7)
first, then retarget the backend PR to `main` and recheck its diff before merging.
No merge is performed as part of this backend setup.
