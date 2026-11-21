
import type { Position } from '../api';
import {
  finalWarehouseLayout,
  type WarehouseLayout,
} from './warehouseLayout';

export type RobotPhase =
  | 'ToPickup'
  | 'ToDropoff'
  | 'Completed';

export type FleetRobot = {
  id: string;
  position: Position;
  pickup: Position;
  dropoff: Position;
  phase: RobotPhase;
  battery: number;
  steps: number;
  route: Position[];
};

export type FleetState = {
  tick: number;
  robots: FleetRobot[];
};

export function positionKey(p: Position): string {
  return `${p.row},${p.column}`;
}

export function samePosition(
  a: Position,
  b: Position
): boolean {
  return a.row === b.row && a.column === b.column;
}

function heuristic(a: Position, b: Position): number {
  return (
    Math.abs(a.row - b.row) +
    Math.abs(a.column - b.column)
  );
}

function neighbors(p: Position): Position[] {
  return [
    { row: p.row - 1, column: p.column },
    { row: p.row, column: p.column + 1 },
    { row: p.row + 1, column: p.column },
    { row: p.row, column: p.column - 1 },
  ];
}

// Local A* for the frontend demonstration.
// Static shelves and other occupied cells are blocked.
export function findPath(
  start: Position,
  goal: Position,
  layout: WarehouseLayout,
  occupied: Set<string> = new Set()
): Position[] {
  const obstacles = new Set(
    layout.shelves.map(positionKey)
  );

  for (const cell of occupied) {
    obstacles.add(cell);
  }

  obstacles.delete(positionKey(start));

  const startKey = positionKey(start);
  const goalKey = positionKey(goal);

  if (obstacles.has(goalKey)) return [];

  const open: Position[] = [start];
  const openKeys = new Set([startKey]);
  const cameFrom = new Map<string, string>();
  const positions = new Map<string, Position>([
    [startKey, start],
  ]);

  const gScore = new Map<string, number>([
    [startKey, 0],
  ]);

  const fScore = new Map<string, number>([
    [startKey, heuristic(start, goal)],
  ]);

  while (open.length > 0) {
    open.sort(
      (a, b) =>
        (fScore.get(positionKey(a)) ?? Infinity) -
        (fScore.get(positionKey(b)) ?? Infinity)
    );

    const current = open.shift()!;
    const currentKey = positionKey(current);
    openKeys.delete(currentKey);

    if (currentKey === goalKey) {
      const path: Position[] = [current];
      let cursor = currentKey;

      while (cameFrom.has(cursor)) {
        cursor = cameFrom.get(cursor)!;
        path.unshift(positions.get(cursor)!);
      }

      return path;
    }

    for (const next of neighbors(current)) {
      if (
        next.row < 0 ||
        next.row >= layout.rows ||
        next.column < 0 ||
        next.column >= layout.columns
      ) {
        continue;
      }

      const nextKey = positionKey(next);
      if (obstacles.has(nextKey)) continue;

      const tentative =
        (gScore.get(currentKey) ?? Infinity) + 1;

      if (
        tentative <
        (gScore.get(nextKey) ?? Infinity)
      ) {
        cameFrom.set(nextKey, currentKey);
        positions.set(nextKey, next);
        gScore.set(nextKey, tentative);
        fScore.set(
          nextKey,
          tentative + heuristic(next, goal)
        );

        if (!openKeys.has(nextKey)) {
          open.push(next);
          openKeys.add(nextKey);
        }
      }
    }
  }

  return [];
}

export function robotTarget(
  robot: FleetRobot
): Position {
  return robot.phase === 'ToPickup'
    ? robot.pickup
    : robot.dropoff;
}

export function createInitialFleet(
  layout: WarehouseLayout = finalWarehouseLayout
): FleetState {
  const robots: FleetRobot[] =
    layout.robotStarts.map((start, i) => {
      const pickup =
        layout.pickups[i % layout.pickups.length];

      const dropoff =
        layout.dropoffs[i % layout.dropoffs.length];

      return {
        id: `R${i + 1}`,
        position: { ...start },
        pickup: { ...pickup },
        dropoff: { ...dropoff },
        phase: 'ToPickup',
        battery: 100,
        steps: 0,
        route: findPath(start, pickup, layout),
      };
    });

  return { tick: 0, robots };
}

export function fleetFinished(
  fleet: FleetState
): boolean {
  return (
    fleet.robots.length > 0 &&
    fleet.robots.every(
      robot => robot.phase === 'Completed'
    )
  );
}

// Advance all robots by one simulation tick.
//
// Robots cannot enter shelf cells or cells occupied
// at the beginning of the tick. A reserved destination
// also cannot be taken by another robot.
//
// This is simple cooperative movement, not a full
// multi-agent pathfinding or deadlock-proof planner.
export function advanceFleet(
  fleet: FleetState,
  layout: WarehouseLayout = finalWarehouseLayout
): FleetState {
  if (fleetFinished(fleet)) return fleet;

  const occupiedAtStart = new Set(
    fleet.robots.map(r => positionKey(r.position))
  );

  const reserved = new Set<string>();

  const robots = fleet.robots.map(robot => {
    if (robot.phase === 'Completed') {
      return robot;
    }

    const target = robotTarget(robot);

    // If already at the target, advance task phase.
    if (samePosition(robot.position, target)) {
      if (robot.phase === 'ToPickup') {
        return {
          ...robot,
          phase: 'ToDropoff' as const,
          route: findPath(
            robot.position,
            robot.dropoff,
            layout
          ),
        };
      }

      return {
        ...robot,
        phase: 'Completed' as const,
        route: [],
      };
    }

    // Prefer paths avoiding the other robots.
    const blocked = new Set(occupiedAtStart);
    blocked.delete(positionKey(robot.position));

    const path = findPath(
      robot.position,
      target,
      layout,
      blocked
    );

    const next = path[1];

    // No safe next cell: robot waits this tick.
    if (!next || reserved.has(positionKey(next))) {
      return { ...robot, route: path };
    }

    // Extra guard against overlapping robots.
    if (occupiedAtStart.has(positionKey(next))) {
      return { ...robot, route: path };
    }

    reserved.add(positionKey(next));

    const reachedTarget = samePosition(next, target);

    const phase: RobotPhase = reachedTarget
      ? robot.phase === 'ToPickup'
        ? 'ToDropoff'
        : 'Completed'
      : robot.phase;

    const followingRoute = reachedTarget
      ? phase === 'ToDropoff'
        ? findPath(next, robot.dropoff, layout)
        : []
      : path.slice(1);

    return {
      ...robot,
      position: next,
      phase,
      battery: Math.max(0, robot.battery - 1),
      steps: robot.steps + 1,
      route: followingRoute,
    };
  });

  return {
    tick: fleet.tick + 1,
    robots,
  };
}
