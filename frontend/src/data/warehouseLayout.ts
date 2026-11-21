
import type { Position } from '../api';

// A warehouse layout that can be reused by the 3D viewer.

export type WarehouseLayout = {
  id: string;
  name: string;
  rows: number;
  columns: number;
  shelves: Position[];
  pickups: Position[];
  dropoffs: Position[];
  robotStarts: Position[];
};

// Convert row and column into a unique key.
export function cellKey(position: Position): string {
  return `${position.row},${position.column}`;
}

// Check whether a cell is inside the warehouse.
export function isInsideWarehouse(
  position: Position,
  layout: WarehouseLayout
): boolean {
  return (
    position.row >= 0 &&
    position.row < layout.rows &&
    position.column >= 0 &&
    position.column < layout.columns
  );
}

// Build an organized warehouse with storage blocks
// and clear aisles between them.
function createShelves(): Position[] {
  const shelves: Position[] = [];

  const rowBlocks = [
    [2, 3],
    [5, 6],
    [9, 10],
  ];

  const columnBlocks = [
    [2, 3, 4],
    [7, 8, 9],
    [12, 13],
  ];

  for (const rows of rowBlocks) {
    for (const columns of columnBlocks) {
      for (const row of rows) {
        for (const column of columns) {
          shelves.push({ row, column });
        }
      }
    }
  }

  return shelves;
}

export const finalWarehouseLayout: WarehouseLayout = {
  id: 'warehouse-phase-2',
  name: 'Runtime Terror — Main Warehouse',

  rows: 12,
  columns: 16,

  shelves: createShelves(),

  pickups: [
    { row: 0, column: 2 },
    { row: 0, column: 7 },
    { row: 0, column: 12 },
  ],

  dropoffs: [
    { row: 11, column: 3 },
    { row: 11, column: 8 },
    { row: 11, column: 13 },
  ],

  robotStarts: [
    { row: 0, column: 0 },
    { row: 0, column: 5 },
    { row: 0, column: 10 },
  ],
};
