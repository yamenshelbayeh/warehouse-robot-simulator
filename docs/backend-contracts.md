# Backend API and Simulation Integration

## 1. Overview

Our Warehouse Robot Simulator uses ASP.NET Core for the backend and a separate C# simulation library.

The backend manages warehouses, robots, delivery tasks, and database operations. The simulation library handles pathfinding, robot selection, and collision detection.

## 2. Integration with Erfan's Simulation

We use Erfan's existing algorithms instead of implementing them again.

| Component | Purpose |
|---|---|
| `WarehouseGrid` | Represents the warehouse layout |
| `AStarPathfinder` | Finds routes around obstacles |
| `TaskAssigner` | Selects the nearest available robot |
| `CollisionAvoidance` | Detects movement conflicts |
| `RobotState` | Represents the current robot state |

The backend communicates with these algorithms through `SimulationAdapter.cs`.

Warehouse positions use row and column coordinates, starting from `(0,0)`.

## 3. API Endpoints

Our backend provides REST API endpoints that the frontend can use.

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/warehouses` | Get all warehouses |
| POST | `/api/warehouses` | Create a warehouse |
| GET | `/api/warehouses/{id}` | Get warehouse details |
| PUT | `/api/warehouses/{id}/cells/{row}/{column}` | Update a cell |
| POST | `/api/warehouses/{id}/robots` | Add a robot |
| PUT | `/api/warehouses/{id}/robots/{robotId}/enabled` | Enable or disable a robot |
| POST | `/api/warehouses/{id}/tasks` | Create a delivery task |
| POST | `/api/warehouses/{id}/tasks/{taskId}/assign` | Assign a robot |
| POST | `/api/warehouses/{id}/tasks/{taskId}/cancel` | Cancel a task |
| POST | `/api/warehouses/{id}/ticks` | Advance the simulation |

The API uses JSON to send and receive data.

Requests that change an existing warehouse include `expectedRevision`, which helps prevent conflicting updates. If the warehouse has changed, the API returns an HTTP 409 error.

## 4. Business Rules

The backend checks several conditions before assigning a robot:

- The robot must be available.
- The robot must have enough battery and carrying capacity.
- The pickup and delivery locations must be reachable.
- A robot cannot work on two tasks at the same time.
- Tasks can only be cancelled before pickup.
- Robot movements are checked to avoid collisions.

These rules help make robot assignment and movement more realistic.

## 5. Team Responsibilities

**Yamen — Backend and Database**

- Develop the ASP.NET Core API.
- Manage PostgreSQL using Entity Framework Core.
- Implement task rules and database operations.
- Save robot and task information.

**Erfan — Simulation Algorithms**

- Implement A* pathfinding.
- Develop robot selection algorithms.
- Improve collision avoidance and movement logic.

**Houri — Frontend**

- Build the user interface.
- Connect the frontend to the API.
- Display warehouses, robots, and task progress.

## 6. Testing

We use xUnit to test our backend and simulation.

We successfully passed 41 tests covering pathfinding, business logic, API operations, and database functionality.

We also tested a complete delivery that finished in six simulation ticks.

## 7. Future Improvements

Some features are still planned:

- Automatic robot charging
- Better collision avoidance
- Deadlock detection and recovery
- Automatic task scheduling
- Real-time simulation updates

The current implementation provides the foundation for adding these features later.
