# Yamen's backend: first runnable increment

For Yamen's installed PostgreSQL 18 and VS Code on Windows 11, follow
[the Windows walkthrough](windows-vscode.md), including the secure local password
prompt and complete test/demo commands.

## What changed

The baseline is Erfan's `erfan/simulation-algorithms` branch at
`fa2f1b4b5a8d74e10e9d02a65918be556655f226`. At inspection, `main` and `develop`
contained placeholders; the simulation code was only on Erfan's branch.
The new work is on `codex/yamen-backend`. Do not merge it over an unrelated
baseline without accounting for the simulation commits.

1. **Foundation:** added `Warehouse.Api`, `Warehouse.Api.Tests`, a solution and a
   pinned local EF CLI tool. Target .NET 9 matches the existing library.
2. **Persistence:** mapped warehouses, grid cells, robots, delivery tasks,
   assignments and tick reservations through EF Core and Npgsql.
   `Persistence/Migrations/` contains the generated initial migration and snapshot.
3. **Rules:** added eligibility checks, guarded state transitions, atomic ticks
   and revisions. `SimulationAdapter` calls the existing algorithms.
4. **Verification:** domain/adapter tests, PostgreSQL/HTTP tests and a GitHub Actions
   workflow. Database tests use migrations against a generated disposable database.

## Start locally

Run these commands from the repository root. Install a .NET 9 SDK and either
Docker with Compose or an accessible PostgreSQL server first.

```powershell
dotnet restore WarehouseRobotSimulator.sln --configfile NuGet.Config
dotnet tool restore --configfile NuGet.Config
docker compose up -d postgres
$localConnection = Read-Host 'Local PostgreSQL connection string' -AsSecureString
$env:ConnectionStrings__Warehouse = [System.Net.NetworkCredential]::new('', $localConnection).Password
Remove-Variable localConnection
dotnet ef database update --project backend/Warehouse.Api
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project backend/Warehouse.Api -- --urls http://localhost:5080
```

The API contains no database credentials. Configure `ConnectionStrings__Warehouse`
before migrations or startup. The existing Compose file defines its local demo
credentials and uses database `warehouse_robot_simulator` on port `5432`. If
PostgreSQL already occupies that port, use your existing development server with
its own credentials instead of starting Compose on the same port. Connection
strings have this shape:

```text
Host=localhost;Port=5432;Database=warehouse_robot_simulator;Username=YOUR_USER;Password=YOUR_PASSWORD
```

Use the secure prompt above, or the Windows walkthrough's connection builder for
passwords with special characters, in the same terminal that starts the API.
Supply credentials locally; do not commit them. The application does not apply migrations
automatically at startup. All schema changes are driven by EF Core migrations;
there are no manual database-management SQL scripts and no `EnsureCreated` path.

In another terminal:

```powershell
Invoke-RestMethod http://localhost:5080/health/ready
./backend/demo-api.ps1
```

The demo creates a warehouse and completes one delivery in six ticks. It prints
the robot/task state and battery after every tick, then the URL for the saved
warehouse. Each run creates a separate example. The development OpenAPI document
is at `http://localhost:5080/openapi/v1.json`; no Swagger UI package is required.

## Tests

```powershell
# Domain tests and Erfan's existing tests; PostgreSQL tests explicitly skip.
dotnet test WarehouseRobotSimulator.sln

# Full suite. The account needs permission to create a temporary test database.
$env:WAREHOUSE_TEST_POSTGRES = $env:ConnectionStrings__Warehouse
dotnet test WarehouseRobotSimulator.sln
dotnet run --project backend/Simulation.Demo
```

The fixture replaces the supplied database name with
`warehouse_tests_<random GUID>`, applies the migration and removes only that
generated database afterwards. It never migrates or deletes the supplied database.
It uses PostgreSQL rather than EF's in-memory provider to exercise relational
constraints and concurrent transactions.

Verified locally: **17 existing simulation tests + 24 backend tests = 41 passed,
0 skipped**, including 7 PostgreSQL integration tests. Local database verification
used an isolated PostgreSQL 18 instance; CI is configured to run against PostgreSQL
16, matching Compose. The CI workflow itself has not been run remotely.

## Change the schema later

Change the entity model and fluent mappings, then generate and review a migration:

```powershell
dotnet ef migrations add DescriptiveChangeName --project backend/Warehouse.Api --output-dir Persistence/Migrations
dotnet ef migrations has-pending-model-changes --project backend/Warehouse.Api
dotnet ef database update --project backend/Warehouse.Api
```

Keep the migration, designer and model snapshot together in version control.
Do not edit an already shared/applied migration to make subsequent schema changes.

## Next increments

This is a local teaching/demo API with manual tick commands. Authentication,
background scheduling, SignalR, automatic queue dispatch, charging, task failure
recovery and deadlock resolution are not implemented. Grid size is capped at
50 × 50 and robots at 100 per warehouse; complete aggregate snapshots prioritize
clarity for this milestone over large-scale throughput.

Review [the integration contracts](backend-contracts.md) with Erfan before changing
algorithm signatures or adding a more permissive movement scheduler.

References: [Npgsql EF provider](https://www.npgsql.org/efcore/),
[EF Core migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/),
[EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency).
