// These shapes mirror backend/Warehouse.Api/Contracts/WarehouseSnapshot.cs.
export type Position = { row: number; column: number };
export type CellView = { position: Position; type: string };
export type RobotView = { id: number; position: Position; state: string; battery: number; enabled: boolean; capacity: number; carryingItem: boolean };
export type TaskView = { id: string; pickup: Position; dropoff: Position; load: number; priority: number; status: string };
export type WarehouseSnapshot = {
  id: string; name: string; rows: number; columns: number; tick: number; revision: number;
  cells: CellView[]; robots: RobotView[]; tasks: TaskView[];
  assignments: unknown[]; reservations: unknown[];
};
export type WarehouseSummary = Pick<WarehouseSnapshot, 'id'|'name'|'rows'|'columns'|'tick'|'revision'>;

async function request<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  const result = await fetch(path, {
    method,
    headers: body === undefined ? undefined : { 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!result.ok) {
    const text = await result.text();
    throw new Error(`${result.status} ${result.statusText}: ${text.slice(0, 300)}`);
  }
  return result.json() as Promise<T>;
}
export const api = {
  ready: () => request<{status: string}>('/health/ready'),
  warehouses: () => request<WarehouseSummary[]>('/api/warehouses/'),
  warehouse: (id: string) => request<WarehouseSnapshot>(`/api/warehouses/${id}`),
  tick: (w: WarehouseSnapshot) => request<WarehouseSnapshot>(`/api/warehouses/${w.id}/ticks`, 'POST', { expectedRevision: w.revision }),
  async createDeliveryDemo(): Promise<WarehouseSnapshot> {
    // Same 1x5 delivery sequence as backend/demo-api.ps1, but without advancing ticks.
    let w = await request<WarehouseSnapshot>('/api/warehouses/', 'POST', {
      name: `Frontend demo ${new Date().toLocaleString()}`, rows: 1, columns: 5,
      cells: [
        { position: { row: 0, column: 1 }, type: 'Pickup' },
        { position: { row: 0, column: 4 }, type: 'Dropoff' },
      ],
    });
    w = await request<WarehouseSnapshot>(`/api/warehouses/${w.id}/robots`, 'POST', {
      expectedRevision: w.revision, position: { row: 0, column: 0 }, battery: 100, capacity: 5,
    });
    w = await request<WarehouseSnapshot>(`/api/warehouses/${w.id}/tasks`, 'POST', {
      expectedRevision: w.revision, pickup: { row: 0, column: 1 },
      dropoff: { row: 0, column: 4 }, load: 2, priority: 1,
    });
    const task = w.tasks[0];
    if (!task) throw new Error('The API returned a warehouse without its new task.');
    return request<WarehouseSnapshot>(`/api/warehouses/${w.id}/tasks/${task.id}/assign`, 'POST', {
      expectedRevision: w.revision,
    });
  },
};
