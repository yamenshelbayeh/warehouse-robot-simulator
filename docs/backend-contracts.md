# API and simulation handoff

This contract is implemented against Erfan's branch at
`fa2f1b4b5a8d74e10e9d02a65918be556655f226`. It is a reviewable integration proposal;
it has not been confirmed by Erfan. The simulation source, demo and tests are
unchanged.

## Library boundary

| Existing public contract | Backend use |
| --- | --- |
| `WarehouseGrid(CellType[,])` | Adapter builds a fresh `[row, column]` array; dynamic occupancy is overlaid on this copy. |
| `AStarPathfinder.FindPath(grid, start, goal)` | Used for eligibility and each tick's route; no A* implementation is copied into the API. |
| `TaskAssigner.FindBestRobot(grid, robots, pickup)` | Receives only backend-eligible robots; shortest pickup path and lowest-ID tie remain Erfan's responsibility. |
| `CollisionAvoidance.HasCollision(current1, next1, current2, next2)` | Final movement conflict check. The backend owns scheduling and persisted reservations. |
| `CellType`, `RobotState` | Used directly; API serializes their names as strings. |

Coordinates are zero-based `{ "row": 0, "column": 1 }`, never x/y.
Paths include both start and goal; move cost is `path.Count - 1`.
An empty path means blocked/unreachable; start equals goal returns one cell.
Robot IDs are positive integers unique **within a warehouse** and stable across
reloads. Warehouse and task IDs are UUIDs. No database entity is passed into the
simulation library.

`ISimulationAlgorithms` and `SimulationAdapter` form the backend seam. Erfan can
extend the shared library behind that adapter while the HTTP and database contracts
remain stable. Avoid placing EF Core references in `Simulation`.

## HTTP contract

Base path: `/api/warehouses`. JSON uses camelCase, enum **names**, and UUID strings.
All commands on an existing warehouse require `expectedRevision` from its latest
snapshot. Revision starts at zero and increases once per successful command.
An identical replay with an old revision returns 409; clients must fetch and decide
whether to retry. Do not blindly retry a successful tick with a new revision.

| Method and suffix | Body | Result |
| --- | --- | --- |
| `GET /` | — | Warehouse summaries |
| `POST /` | `name`, `rows`, `columns`, optional `cells: [{position, type}]` | 201 and complete snapshot |
| `GET /{id}` | — | Complete snapshot |
| `PUT /{id}/cells/{row}/{column}` | `expectedRevision`, `type` | Updated snapshot |
| `POST /{id}/robots` | `expectedRevision`, `position`, `battery`, `capacity` | Updated snapshot including new robot |
| `PUT /{id}/robots/{robotId}/enabled` | `expectedRevision`, `enabled` | Updated snapshot |
| `POST /{id}/tasks` | `expectedRevision`, `pickup`, `dropoff`, `load`, `priority` | Updated snapshot including new task |
| `POST /{id}/tasks/{taskId}/assign` | `expectedRevision`, optional `robotId` | Updated snapshot; omit robot ID for library selection |
| `POST /{id}/tasks/{taskId}/cancel` | `expectedRevision` | Updated snapshot |
| `POST /{id}/ticks` | `expectedRevision` | One committed tick and updated snapshot |

Snapshot fields: `id`, `name`, `rows`, `columns`, `tick`, `revision`, `cells`,
`robots`, `tasks`, `assignments`, `reservations`. See
`backend/Warehouse.Api/Contracts/WarehouseSnapshot.cs` for individual record fields.
Reservations contain `tick`, `robotId`, `from` and `to`, and describe the most recent
committed movement (including waits). They are not promises for future ticks.

HTTP errors use Problem Details with a stable `code`:
400 for malformed requests or invalid values, 404 for resources outside the named
warehouse or absent resources, and 409 for domain/concurrency conflicts.
Examples include `robot_ineligible`, `task_not_pending`, `unreachable_dropoff`,
`invalid_transition`, `stale_revision` and `concurrent_change`.

The API controls one warehouse at a time. A client should refresh its whole
snapshot after any 409 before offering the next action.

## Decisions to review with Erfan

1. Keep the four existing cell types for this milestone. Add a new obstacle or
   charging type only with a shared walkability contract.
2. Confirm battery units (one per move, reserve ten) and capacity/load units
   (integer simulation units); these are initial backend policies.
3. The current scheduler reserves starting cells for the whole tick. A future
   reservation-aware scheduler may allow following moves, but must handle losers
   that remain stationary, vertex conflicts and reverse-edge swaps together.
4. Assignment eligibility uses static reachability; temporary robot occupancy is
   resolved during movement. Add wait counters, charging and deadlock recovery
   before promising that every accepted delivery will eventually finish.
5. Keep warehouse mutation and persistence in Yamen's service. Erfan's future
   scheduler should return proposed moves/plans; Yamen validates and commits the
   result atomically. Houri consumes the snapshots and sends commands with revisions.

Existing tests protect Erfan's API. New adapter/domain tests exercise the real
library for selection, detours and conflicts, and PostgreSQL tests protect the
transaction boundary. The handoff can be reviewed without changing his branch.
