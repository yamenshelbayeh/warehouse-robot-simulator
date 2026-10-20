# Warehouse Robot Simulator

A multi-robot warehouse simulation system for coordinating autonomous robots that move through a warehouse, receive tasks, calculate routes, avoid collisions, and complete orders efficiently.

## Project Goal

The project focuses on non-trivial simulation and decision-making logic rather than simple CRUD functionality. The core challenges include pathfinding, task assignment, collision avoidance, route reservation, battery-aware decisions, and performance analysis.

## Planned Features

- Configurable warehouse grid with shelves, stations, obstacles, and charging points
- Multiple autonomous robots with position, state, battery level, and assigned tasks
- A* pathfinding through the warehouse
- Dynamic task assignment based on distance, workload, priority, and battery level
- Collision prevention and route reservation between robots
- Dynamic rerouting when paths become blocked
- Battery usage and charging behavior
- Order queue with priorities
- Simulation controls: start, pause, reset, and speed adjustment
- Live warehouse visualization
- Statistics such as order completion time, robot utilization, travel distance, and waiting time

## Technology Stack

### Frontend
- React
- TypeScript
- HTML / CSS

### Backend
- C#
- ASP.NET Core
- Entity Framework Core

### Database
- PostgreSQL

### Testing
- xUnit for backend tests
- Frontend testing tools to be selected during implementation

## Repository Structure

```text
warehouse-robot-simulator/
├── backend/      # ASP.NET Core backend and simulation engine
├── frontend/     # React + TypeScript user interface
├── docs/         # Architecture, diagrams, and project documentation
├── docker-compose.yml
└── README.md
```

## Core Simulation Flow

```text
Order created
    ↓
Find eligible robots
    ↓
Estimate task cost for each robot
    ↓
Assign the best robot
    ↓
Calculate route with A*
    ↓
Reserve path / avoid collisions
    ↓
Move robot through simulation ticks
    ↓
Pick up and deliver order
    ↓
Update statistics and robot state
```

## Status

The backend foundation now includes an ASP.NET Core API, EF Core/Npgsql migrations,
persisted delivery tasks and assignments, guarded task transitions, battery/capacity
eligibility, and atomic movement ticks with cell reservations.

- [Run the backend and its demo](docs/backend-getting-started.md)
- [Windows 11 + VS Code: exact setup steps](docs/windows-vscode.md)
- [Domain model and ER diagram](docs/backend-domain.md)
- [Public API and simulation integration contracts](docs/backend-contracts.md)

The API references Erfan's existing `Simulation` library. Its algorithms, demo and
tests are unchanged. The current tick policy is conservative; charging, automatic
dispatch queues and deadlock recovery remain later increments.
