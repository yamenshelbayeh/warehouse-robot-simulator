# Frontend prototype — Runtime Terror

React + TypeScript + HTML/CSS. This folder is standalone and can be merged into the main repository without changing Yamen's backend or Erfan's simulation code.

## Start

```powershell
cd frontend
npm install
npm run dev
```

Open http://localhost:5173/. Default **A* demo** works without a backend.

## Connect to Yamen's API

1. Yamen must start PostgreSQL, run EF Core migrations, and start `Warehouse.Api` on http://localhost:5080 per `docs/backend-setup.md`.
2. Open the React app → **Live backend** → **Check connection / refresh**.
3. Choose an existing warehouse or click **Create live delivery demo**. This creates a new **1×5** warehouse with one robot and one assigned pickup/dropoff task in PostgreSQL (it is not a mock).
4. Click **Step** or **Start**. Each movement tick calls `POST /api/warehouses/{id}/ticks` with the latest `expectedRevision`. The returned warehouse snapshot updates the UI.
5. **Reset** in demo mode restarts the local route. Live mode has no reset API, so create a **new live delivery demo** instead.

Vite proxies `/api` and `/health` to port 5080 in development, so no ASP.NET Core CORS modifications are needed. If the backend runs elsewhere, change `frontend/vite.config.ts`.

Backend snapshots use `{row, column}`, not `{row, col}`. The UI renders `CellType` values `Empty`, `Shelf`, `Pickup`, and `Dropoff`, plus robot position/state, battery and task status. Do **not** infer an A* route from a snapshot; live map displays actual positions only.

## Verify

```powershell
npm run build
```

Manual tests: sample Start/Pause/Step/Reset; in live mode connect, create demo, advance until task `Completed`, confirm battery/position/status update; try unreachable backend and confirm error displays. Local build does not prove the database/API is online.

## Scope

The frontend is a first-presentation prototype, not a production system. No authentication, charging automation, 3D rendering, automatic task queue, or WebSockets yet.
