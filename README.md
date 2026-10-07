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

Early development / project setup.
