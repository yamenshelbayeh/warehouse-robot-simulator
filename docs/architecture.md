# Architecture Notes

## Stack

- Frontend: React + TypeScript
- Backend: C# + ASP.NET Core
- Database: PostgreSQL
- ORM: Entity Framework Core

## High-level design

The application is intentionally centered on simulation and decision-making logic rather than a deep layered CRUD architecture.

```text
React UI
   ↕ REST / WebSocket
ASP.NET Core API
   ↕
Simulation Engine
   ├─ Pathfinding
   ├─ Task Assignment
   ├─ Collision Avoidance
   ├─ Route Reservation
   ├─ Battery / Charging Logic
   └─ Statistics
   ↕
PostgreSQL
```

## Core domain objects

- Warehouse
- GridCell
- Shelf
- Station
- ChargingStation
- Robot
- RobotState
- Order
- Task
- Route
- Reservation
- SimulationState

## Initial algorithm targets

1. A* pathfinding on a grid
2. Cost-based robot task assignment
3. Time-step simulation loop
4. Cell/path reservation for collision prevention
5. Dynamic rerouting when routes become invalid
6. Battery-aware charging decisions
7. Deadlock detection/resolution if time permits

## Non-trivial business logic

The main complexity of the project should come from coordinating several robots that compete for shared warehouse space and resources. The system should make decisions from multiple inputs such as robot position, battery level, workload, order priority, route cost, and current reservations.
