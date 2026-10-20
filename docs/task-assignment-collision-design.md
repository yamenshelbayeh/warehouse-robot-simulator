
# Task Assignment and Collision Avoidance Design

## Task Assignment

Strategy: Shortest reachable path first.

1. Find all idle robots.
2. Calculate each robot's A* path to pickup.
3. Exclude robots with no valid route.
4. Select the robot with the shortest path.
5. Break ties using the lowest robot ID.

## Collision Cases

- Same-cell conflict: Two robots enter one cell.
- Head-on swap: Two robots exchange cells.
- Blocked route: A robot tries to enter an occupied cell.

## Collision Avoidance

Strategy: Cell reservations and wait/reroute.

1. Each robot proposes its next position.
2. Check proposed moves for collisions.
3. Resolve conflicts using robot priority.
4. Allow only safe movements.
5. Waiting robots remain in their current cells.
6. Recalculate a route after repeated waiting.

## Future Implementation

- Tick-based movement.
- Cell reservations.
- Swap prevention.
- Deadlock detection and recovery.
- Multi-robot simulation tests.
