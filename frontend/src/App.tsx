
import { useCallback, useEffect, useState } from 'react';
import {
  api,
  type Position,
  type WarehouseSnapshot,
} from './api';
import Warehouse3D from './components/warehouse/Warehouse3D';
import { finalWarehouseLayout } from './data/warehouseLayout';
import {
  advanceFleet,
  createInitialFleet,
  fleetFinished,
  robotTarget,
  type FleetRobot,
} from './data/fleetSimulation';
import './style.css';

type Mode = 'demo' | 'live';
type ViewMode = '2d' | '3d' | 'main';

const ROBOT_COLORS: Record<string, string> = {
  R1: '#b4233a',
  R2: '#2563eb',
  R3: '#16a085',
};

const DEFAULT_COLOR = '#b4233a';

function getRobotColor(id: string): string {
  return ROBOT_COLORS[id] ?? DEFAULT_COLOR;
}

const demoRoute: Position[] = [
  { row: 0, column: 0 },
  { row: 1, column: 0 },
  { row: 2, column: 0 },
  { row: 2, column: 1 },
  { row: 2, column: 2 },
  { row: 2, column: 3 },
  { row: 2, column: 4 },
  { row: 3, column: 4 },
];

const demoShelves = new Set([
  '1,1',
  '1,2',
  '1,3',
  '3,2',
]);

const key = (p: Position) =>
  `${p.row},${p.column}`;

const same = (a: Position, b: Position) =>
  a.row === b.row && a.column === b.column;

// ==========================================
// CONTROL ICONS
// ==========================================

function Icon({
  name,
}: {
  name: 'play' | 'pause' | 'step' | 'reset';
}) {
  const paths = {
    play: (
      <path d="m8 5 11 7-11 7z" fill="currentColor" />
    ),
    pause: (
      <path d="M8 5v14M16 5v14" strokeWidth="3" />
    ),
    step: (
      <>
        <path d="m5 5 9 7-9 7z" />
        <path d="M19 5v14" />
      </>
    ),
    reset: (
      <path d="M3 11a9 9 0 1 1 2.6 6.4M3 4v7h7" />
    ),
  };

  return (
    <svg
      width="17"
      height="17"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      {paths[name]}
    </svg>
  );
}

// ==========================================
// COLORED ROBOT ICON
// ==========================================

function RobotIcon({
  large = false,
  color = DEFAULT_COLOR,
}: {
  large?: boolean;
  color?: string;
}) {
  return (
    <svg
      className={`robot-svg ${large ? 'large' : ''}`}
      viewBox="0 0 80 80"
      role="img"
      aria-label="Warehouse robot"
    >
      <ellipse
        cx="40"
        cy="69"
        rx="24"
        ry="5"
        fill="#4f1020"
        opacity=".16"
      />

      <rect
        x="12"
        y="35"
        width="8"
        height="24"
        rx="3"
        fill="#5e6878"
      />

      <rect
        x="60"
        y="35"
        width="8"
        height="24"
        rx="3"
        fill="#5e6878"
      />

      <rect
        x="16"
        y="19"
        width="48"
        height="44"
        rx="12"
        fill={color}
        stroke={color}
        strokeWidth="2"
      />

      <rect
        x="24"
        y="13"
        width="32"
        height="10"
        rx="5"
        fill={color}
      />

      <rect
        x="22"
        y="28"
        width="36"
        height="25"
        rx="8"
        fill="#fff5f5"
      />

      <circle
        cx="32"
        cy="39"
        r="4"
        fill={color}
      />

      <circle
        cx="48"
        cy="39"
        r="4"
        fill={color}
      />

      <path
        d="M34 47q6 5 12 0"
        fill="none"
        stroke={color}
        strokeWidth="2"
        strokeLinecap="round"
      />
    </svg>
  );
}

// ==========================================
// SHELF ICON
// ==========================================

function ShelfIcon() {
  return (
    <svg
      className="shelf-svg"
      viewBox="0 0 80 80"
      role="img"
      aria-label="Storage shelf"
    >
      <rect
        x="13"
        y="10"
        width="54"
        height="60"
        rx="4"
        fill="#627187"
      />

      {[15, 34, 53].map(y => (
        <g key={y}>
          <rect
            x="19"
            y={y}
            width="42"
            height="16"
            rx="2"
            fill="#caa77d"
            stroke="#ad8659"
            strokeWidth="1"
          />
          <path
            d={`M40 ${y}v16`}
            stroke="#ab865b"
            strokeWidth="1.5"
          />
        </g>
      ))}

      <path
        d="M14 32h52M14 51h52"
        stroke="#405169"
        strokeWidth="3"
      />

      <rect
        x="11"
        y="9"
        width="5"
        height="63"
        rx="2"
        fill="#455366"
      />

      <rect
        x="64"
        y="9"
        width="5"
        height="63"
        rx="2"
        fill="#455366"
      />
    </svg>
  );
}

// ==========================================
// GOAL ICON
// ==========================================

function GoalIcon() {
  return (
    <div className="goal-mark">
      <svg viewBox="0 0 40 42" aria-hidden="true">
        <path
          d="M11 37V6"
          stroke="currentColor"
          strokeWidth="3"
          strokeLinecap="round"
        />

        <path
          d="M13 7h22l-8 10 8 10H13z"
          fill="currentColor"
        />
      </svg>
      <span>GOAL</span>
    </div>
  );
}

// ==========================================
// ROBOT SELECTION PANEL
// ==========================================

function MainFleetStatus({
  robots,
  selectedId,
  onSelect,
}: {
  robots: FleetRobot[];
  selectedId: string;
  onSelect: (id: string) => void;
}) {
  return (
    <div style={{ padding: '16px 24px' }}>
      <div
        className="section-divider-title"
        style={{ margin: '0 0 12px' }}
      >
        SELECT A ROBOT
      </div>

      <div
        style={{
          display: 'flex',
          gap: 8,
          flexWrap: 'wrap',
        }}
      >
        {robots.map(robot => {
          const color = getRobotColor(robot.id);
          const selected = robot.id === selectedId;

          return (
            <button
              key={robot.id}
              type="button"
              onClick={() => onSelect(robot.id)}
              style={{
                padding: '10px 15px',
                borderRadius: 8,
                cursor: 'pointer',
                fontWeight: 750,
                border: `1px solid ${color}`,
                background: selected ? color : '#ffffff',
                color: selected ? '#ffffff' : color,
                transition: 'all 0.2s ease',
              }}
            >
              {robot.id}
              {' · '}
              {robot.phase === 'Completed'
                ? 'Done'
                : robot.phase === 'ToPickup'
                  ? 'Pickup'
                  : 'Delivery'}
            </button>
          );
        })}
      </div>
    </div>
  );
}

// ==========================================
// MAIN APPLICATION
// ==========================================

export default function App() {
  const [mode, setMode] = useState<Mode>('demo');

  const [warehouseView, setWarehouseView] =
    useState<ViewMode>('2d');

  const [step, setStep] = useState(0);
  const [playing, setPlaying] = useState(false);

  const [fleet, setFleet] = useState(() =>
    createInitialFleet(finalWarehouseLayout)
  );

  const [selectedRobotId, setSelectedRobotId] =
    useState('R1');

  const [snapshot, setSnapshot] =
    useState<WarehouseSnapshot | null>(null);

  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [apiState, setApiState] =
    useState('Not connected');

  const [warehouses, setWarehouses] = useState<
    { id: string; name: string }[]
  >([]);

  const [selectedId, setSelectedId] = useState('');

  // ========================================
  // SIMULATION STATE
  // ========================================

  const main = warehouseView === 'main';
  const live = mode === 'live';

  const robot = snapshot?.robots[0];
  const task = snapshot?.tasks[0];

  const activeFleetRobot =
    fleet.robots.find(r => r.id === selectedRobotId) ??
    fleet.robots[0];

  // Main Warehouse robot-specific colors
  const selectedRobotColor = main
    ? getRobotColor(activeFleetRobot.id)
    : DEFAULT_COLOR;

  const mainDone = fleetFinished(fleet);

  const demoTotal = demoRoute.length - 1;
  const demoDone = step >= demoTotal;

  const liveDone =
    !!task &&
    (
      task.status === 'Completed' ||
      task.status === 'Cancelled'
    );

  const regularDone = live ? liveDone : demoDone;
  const finished = main ? mainDone : regularDone;

  const position: Position = live
    ? (robot?.position ?? { row: 0, column: 0 })
    : demoRoute[step];

  const goal: Position = live
    ? (task?.dropoff ?? { row: 0, column: 4 })
    : demoRoute[demoTotal];

  const rows = live
    ? (snapshot?.rows ?? 1)
    : 4;

  const columns = live
    ? (snapshot?.columns ?? 5)
    : 5;

  const displayRows = main
    ? finalWarehouseLayout.rows
    : rows;

  const displayColumns = main
    ? finalWarehouseLayout.columns
    : columns;

  const shelfPositions: Position[] = live
    ? (
        snapshot?.cells
          .filter(c => c.type === 'Shelf')
          .map(c => c.position) ?? []
      )
    : Array.from(demoShelves, cell => {
        const [row, column] = cell
          .split(',')
          .map(Number);

        return { row, column };
      });

  const cells = new Map(
    snapshot?.cells.map(c => [
      key(c.position),
      c.type,
    ]) ?? []
  );

  const routeCells = new Set(
    live ? [] : demoRoute.map(key)
  );

  const visitedCells = new Set(
    live
      ? []
      : demoRoute.slice(0, step + 1).map(key)
  );

  const currentTick = live
    ? (snapshot?.tick ?? 0)
    : step;

  const completedCount = fleet.robots.filter(
    r => r.phase === 'Completed'
  ).length;

  const percent = main
    ? Math.round(
        completedCount /
        Math.max(1, fleet.robots.length) * 100
      )
    : live
      ? liveDone
        ? 100
        : Math.min(
            95,
            Math.round(currentTick / 6 * 100)
          )
      : Math.round(step / demoTotal * 100);

  const status = main
    ? mainDone
      ? 'Completed'
      : playing
        ? 'Running'
        : fleet.tick
          ? 'Paused'
          : 'Ready'
    : live
      ? (robot?.state ?? 'No robot')
      : demoDone
        ? 'Arrived'
        : playing
          ? 'Moving'
          : step
            ? 'Paused'
            : 'Ready';

  const canTick = main
    ? !mainDone
    : live
      ? !!snapshot && !!robot && !liveDone && !busy
      : !demoDone;

  // ========================================
  // BACKEND FUNCTIONS
  // ========================================

  const refreshList = useCallback(async () => {
    setError('');
    setBusy(true);

    try {
      await api.ready();
      setApiState('Connected');

      setWarehouses(await api.warehouses());
    } catch (e) {
      setApiState('Unavailable');

      setError(
        e instanceof Error ? e.message : String(e)
      );
    } finally {
      setBusy(false);
    }
  }, []);

  const loadWarehouse = useCallback(
    async (id: string) => {
      setPlaying(false);
      setError('');

      if (!id) {
        setSelectedId('');
        setSnapshot(null);
        return;
      }

      setBusy(true);

      try {
        const result = await api.warehouse(id);

        setSnapshot(result);
        setSelectedId(id);
      } catch (e) {
        setError(
          e instanceof Error ? e.message : String(e)
        );
      } finally {
        setBusy(false);
      }
    },
    []
  );

  const createLive = useCallback(async () => {
    setPlaying(false);
    setError('');
    setBusy(true);

    try {
      await api.ready();
      setApiState('Connected');

      const result = await api.createDeliveryDemo();

      setSnapshot(result);
      setSelectedId(result.id);

      setWarehouses(await api.warehouses());
    } catch (e) {
      setApiState('Unavailable');

      setError(
        e instanceof Error ? e.message : String(e)
      );
    } finally {
      setBusy(false);
    }
  }, []);

  // ========================================
  // SIMULATION MOVEMENT
  // ========================================

  const advance = useCallback(async () => {
    if (main) {
      setFleet(current =>
        advanceFleet(current, finalWarehouseLayout)
      );
      return;
    }

    if (!live) {
      setStep(s =>
        Math.min(demoTotal, s + 1)
      );
      return;
    }

    if (!snapshot || busy || liveDone) return;

    setBusy(true);
    setError('');

    try {
      setSnapshot(await api.tick(snapshot));
    } catch (e) {
      setPlaying(false);

      setError(
        e instanceof Error ? e.message : String(e)
      );
    } finally {
      setBusy(false);
    }
  }, [
    main,
    live,
    snapshot,
    busy,
    liveDone,
    demoTotal,
  ]);

  // Main Warehouse automatic simulation
  useEffect(() => {
    if (!playing || !main || mainDone) return;

    const timer = window.setInterval(() => {
      setFleet(current =>
        advanceFleet(current, finalWarehouseLayout)
      );
    }, 850);

    return () => window.clearInterval(timer);
  }, [playing, main, mainDone]);

  // Original A* demonstration
  useEffect(() => {
    if (
      !playing ||
      main ||
      live ||
      demoDone
    ) {
      return;
    }

    const timer = window.setInterval(() => {
      setStep(s =>
        Math.min(demoTotal, s + 1)
      );
    }, 850);

    return () => window.clearInterval(timer);
  }, [
    playing,
    main,
    live,
    demoDone,
    demoTotal,
  ]);

  // Live backend ticks
  useEffect(() => {
    if (
      !playing ||
      main ||
      !live ||
      regularDone ||
      !snapshot ||
      busy
    ) {
      return;
    }

    const timer = window.setTimeout(() => {
      void advance();
    }, 850);

    return () => window.clearTimeout(timer);
  }, [
    playing,
    main,
    live,
    regularDone,
    snapshot,
    busy,
    advance,
  ]);

  useEffect(() => {
    if (finished) {
      setPlaying(false);
    }
  }, [finished]);

  function changeView(view: ViewMode) {
    setPlaying(false);
    setWarehouseView(view);
  }

  function changeMode(next: Mode) {
    setPlaying(false);
    setError('');
    setMode(next);
  }

  function resetSimulation() {
    setPlaying(false);

    if (main) {
      setFleet(
        createInitialFleet(finalWarehouseLayout)
      );
      return;
    }

    if (live) {
      void refreshList();
    } else {
      setStep(0);
    }
  }

  // ========================================
  // ROBOT INSPECTOR DATA
  // ========================================

  const inspectorPosition = main
    ? activeFleetRobot.position
    : position;

  const inspectorGoal = main
    ? robotTarget(activeFleetRobot)
    : goal;

  const inspectorStatus = main
    ? activeFleetRobot.phase
    : status;

  // ========================================
  // APPLICATION INTERFACE
  // ========================================

  return (
    <div className="app">
      <div className="accent-line" />

      {/* HEADER */}
      <header className="topbar">
        <div className="topbar-content">
          <div className="branding">
            <span className="brand-icon">▦</span>

            <span>
              <strong>
                WAREHOUSE
                <span className="red">/OS</span>
              </strong>

              <small>
                SDE MANIACS · RUNTIME TERROR
              </small>
            </span>
          </div>

          <div className="header-right">
            <span className="workspace-tag">
              <span className="online-dot" />
              Prototype workspace
            </span>

            <span className="user-avatar">
              RT
            </span>
          </div>
        </div>
      </header>

      <main className="container">
        {/* BREADCRUMBS */}
        <div className="breadcrumbs">
          <span>Workspace</span>
          <span>/</span>
          <strong>Simulation overview</strong>
        </div>

        {/* HERO */}
        <section className="hero">
          <div className="hero-content">
            <span className="hero-eyebrow">
              FLEET CONTROL CENTER · PHASE 2
            </span>

            <h1>
              Warehouse Robot Simulator
            </h1>

            <p>
              Monitor robot navigation in 2D,
              explore 3D environments, and manage
              multiple autonomous warehouse robots.
            </p>
          </div>

          <span className="hero-badge">
            {main
              ? '● LOCAL MULTI-ROBOT MODE'
              : live
                ? '● LIVE API MODE'
                : '● A* DEMO MODE'}
          </span>
        </section>

        {/* DATA SOURCE */}
        <div className="integration-bar">
          <div>
            <strong>Data source</strong>

            <span>
              Choose local demonstration or live backend data
            </span>
          </div>

          <div className="mode-switch">
            <button
              type="button"
              className={
                mode === 'demo' ? 'active' : ''
              }
              onClick={() => changeMode('demo')}
            >
              A* demo
            </button>

            <button
              type="button"
              className={live ? 'active' : ''}
              onClick={() => changeMode('live')}
            >
              Live backend
            </button>
          </div>
        </div>

        {/* LIVE BACKEND CONTROLS */}
        {live && !main && (
          <div className="backend-tools">
            <span>
              <strong>API:</strong> {apiState}
            </span>

            <button
              type="button"
              onClick={() => void refreshList()}
              disabled={busy}
            >
              Check connection / refresh
            </button>

            <select
              aria-label="Select warehouse"
              value={selectedId}
              onChange={e =>
                void loadWarehouse(e.target.value)
              }
              disabled={busy}
            >
              <option value="">
                Select a warehouse...
              </option>

              {warehouses.map(w => (
                <option key={w.id} value={w.id}>
                  {w.name}
                </option>
              ))}
            </select>

            <button
              type="button"
              className="create-button"
              onClick={() => void createLive()}
              disabled={busy}
            >
              Create live delivery demo
            </button>
          </div>
        )}

        {error && !main && (
          <div
            className="error-banner"
            role="alert"
          >
            <strong>
              Connection / API error:
            </strong>{' '}
            {error}

            <div>
              Ensure ASP.NET Core and PostgreSQL
              are running.
            </div>
          </div>
        )}

        {/* DASHBOARD STATISTICS */}
        <section
          className="kpis"
          aria-label="Simulation metrics"
        >
          <div className="kpi">
            <span className="kpi-label">
              ROBOTS
            </span>

            <strong>
              {main
                ? fleet.robots.length
                : live
                  ? snapshot?.robots.length ?? 0
                  : 1}
            </strong>

            <span className="kpi-detail">
              Units in current simulation
            </span>
          </div>

          <div className="kpi">
            <span className="kpi-label">
              WAREHOUSE GRID
            </span>

            <strong>
              {displayRows} × {displayColumns}
            </strong>

            <span className="kpi-detail">
              Grid cells · row and column
            </span>
          </div>

          <div className="kpi">
            <span className="kpi-label">
              {main
                ? 'FLEET TICK'
                : live
                  ? 'CURRENT TICK'
                  : 'PATH LENGTH'}
            </span>

            <strong>
              {main
                ? fleet.tick
                : live
                  ? currentTick
                  : demoTotal}
            </strong>

            <span className="kpi-detail">
              {main
                ? 'Local simulation steps'
                : live
                  ? 'Backend simulation ticks'
                  : 'A* demonstration steps'}
            </span>
          </div>

          <div className="kpi">
            <span className="kpi-label">
              STATUS
            </span>

            <strong className="state-title">
              {status}
            </strong>

            <span className="kpi-detail">
              {main
                ? `${completedCount} of ${fleet.robots.length} tasks complete`
                : live
                  ? task?.status ?? 'No task'
                  : `${percent}% complete`}
            </span>
          </div>
        </section>

        {/* MAIN DASHBOARD */}
        <div className="dashboard">
          <section className="panel warehouse-panel">
            <div className="panel-head">
              <div>
                <span className="section-eyebrow">
                  01 / MAP MONITOR
                </span>

                <h2>
                  Warehouse visualization
                </h2>

                <p>
                  Robot positions, shelves, stations,
                  and destinations
                </p>
              </div>

              <span className="head-tag">
                {displayRows} rows × {displayColumns} cols
              </span>
            </div>

            {/* VIEW SELECTION */}
            <div
              className="warehouse-view-switch"
              role="group"
              aria-label="Warehouse display mode"
              style={{ flexWrap: 'wrap' }}
            >
              <button
                type="button"
                className={
                  warehouseView === '2d'
                    ? 'active'
                    : ''
                }
                onClick={() => changeView('2d')}
              >
                2D View
              </button>

              <button
                type="button"
                className={
                  warehouseView === '3d'
                    ? 'active'
                    : ''
                }
                onClick={() => changeView('3d')}
              >
                3D Demo
              </button>

              <button
                type="button"
                className={
                  warehouseView === 'main'
                    ? 'active'
                    : ''
                }
                onClick={() => changeView('main')}
              >
                Main Warehouse
              </button>
            </div>

            {/* MAIN WAREHOUSE */}
            {main ? (
              <div className="warehouse-3d-container">
                <Warehouse3D
                  rows={finalWarehouseLayout.rows}
                  columns={finalWarehouseLayout.columns}
                  robotPosition={fleet.robots[0].position}
                  goal={finalWarehouseLayout.dropoffs[0]}
                  shelves={finalWarehouseLayout.shelves}
                  pickups={finalWarehouseLayout.pickups}
                  dropoffs={finalWarehouseLayout.dropoffs}
                  robots={fleet.robots.map(r => ({
                    id: r.id,
                    position: r.position,
                    status: r.phase,
                  }))}
                  routePositions={activeFleetRobot.route}
                  preview
                />

                <div className="map-footnote">
                  Local multi-robot simulation.
                  All three robots navigate around shelves.
                  Select R1, R2 or R3 in the inspector.
                  This view does not change backend data.
                </div>
              </div>
            ) : warehouseView === '3d' ? (
              /* ORIGINAL 3D DEMO */
              <div className="warehouse-3d-container">
                <Warehouse3D
                  rows={rows}
                  columns={columns}
                  robotPosition={position}
                  goal={goal}
                  shelves={shelfPositions}
                  routePositions={
                    live ? [] : demoRoute
                  }
                />
              </div>
            ) : (
              /* ORIGINAL 2D VIEW */
              <>
                <div className="map-toolbar">
                  <div className="toolbar-label">
                    <span className="live-indicator" />
                    FLOOR PLAN · LEVEL 01
                  </div>

                  <div className="map-grid-reference">
                    ORIGIN (0, 0) · ROW / COLUMN
                  </div>
                </div>

                <div className="map-container">
                  <div
                    className="grid-with-labels"
                    style={{
                      gridTemplateColumns:
                        `25px repeat(${columns}, minmax(0, 1fr))`,
                      width:
                        `min(100%,${Math.max(
                          420,
                          columns * 95
                        )}px)`,
                    }}
                  >
                    <div className="corner-axis" />

                    {/* COLUMN NUMBERS */}
                    {Array.from(
                      { length: columns },
                      (_, c) => (
                        <div
                          className="axis-label"
                          key={`h${c}`}
                        >
                          {String(c).padStart(2, '0')}
                        </div>
                      )
                    )}

                    {/* WAREHOUSE GRID */}
                    {Array.from(
                      { length: rows },
                      (_, r) => (
                        <div
                          key={`row-${r}`}
                          style={{ display: 'contents' }}
                        >
                          <div className="axis-label row-axis">
                            {String(r).padStart(2, '0')}
                          </div>

                          {Array.from(
                            { length: columns },
                            (_, c) => {
                              const p = {
                                row: r,
                                column: c,
                              };

                              const cellType = live
                                ? (
                                    cells.get(key(p)) ??
                                    'Empty'
                                  )
                                : (
                                    demoShelves.has(key(p))
                                      ? 'Shelf'
                                      : 'Empty'
                                  );

                              const shelf =
                                cellType === 'Shelf';

                              const pickup =
                                cellType === 'Pickup';

                              const isRobot = live
                                ? (
                                    snapshot?.robots.some(
                                      x => same(x.position, p)
                                    ) ?? false
                                  )
                                : same(p, position);

                              const isGoal = live
                                ? cellType === 'Dropoff'
                                : same(p, goal);

                              const onRoute =
                                routeCells.has(key(p));

                              const visited =
                                visitedCells.has(key(p));

                              return (
                                <div
                                  key={key(p)}
                                  title={`Row ${r}, Col ${c}`}
                                  className={`cell
                                    ${shelf ? 'cell-shelf' : ''}
                                    ${onRoute ? 'cell-route' : ''}
                                    ${visited ? 'cell-visited' : ''}
                                    ${isGoal ? 'cell-goal' : ''}
                                    ${isRobot ? 'cell-robot' : ''}
                                  `}
                                >
                                  {isRobot ? (
                                    <div
                                      className={`robot-unit ${
                                        playing
                                          ? 'robot-moving'
                                          : ''
                                      }`}
                                    >
                                      <RobotIcon />

                                      <span className="robot-unit-tag">
                                        R{live
                                          ? (
                                              snapshot?.robots.find(
                                                x => same(
                                                  x.position,
                                                  p
                                                )
                                              )?.id ?? 1
                                            )
                                          : 1}
                                      </span>
                                    </div>
                                  ) : shelf ? (
                                    <ShelfIcon />
                                  ) : isGoal ? (
                                    <GoalIcon />
                                  ) : pickup ? (
                                    <span className="pickup-mark">
                                      PICKUP
                                    </span>
                                  ) : onRoute ? (
                                    <span className="route-marker" />
                                  ) : null}
                                </div>
                              );
                            }
                          )}
                        </div>
                      )
                    )}
                  </div>
                </div>

                {/* MAP LEGEND */}
                <div className="legend">
                  <div>
                    <span className="legend-robot">
                      <RobotIcon />
                    </span>
                    Robot
                  </div>

                  <div>
                    <span className="legend-shelf">
                      <ShelfIcon />
                    </span>
                    Shelf
                  </div>

                  <div>
                    <i className="legend-route" />
                    A* route
                  </div>

                  <div>
                    <i className="legend-finish" />
                    Goal
                  </div>
                </div>

                <div className="map-footnote">
                  {live
                    ? 'Coordinates come from the backend snapshot.'
                    : 'Original seven-step A* demonstration.'}
                </div>
              </>
            )}
          </section>

          {/* ROBOT INSPECTOR */}
          <aside className="panel operations-panel">
            <div className="panel-head">
              <div>
                <span className="section-eyebrow">
                  02 / FLEET STATUS
                </span>

                <h2>
                  Robot inspector
                </h2>

                <p>
                  Position, state, task and simulation controls
                </p>
              </div>

              <span className="head-tag">
                {main
                  ? 'FLEET'
                  : live
                    ? 'API'
                    : 'DEMO'}
              </span>
            </div>

            {/* COLORED ROBOT SELECTOR */}
            {main && (
              <MainFleetStatus
                robots={fleet.robots}
                selectedId={selectedRobotId}
                onSelect={setSelectedRobotId}
              />
            )}

            {/* COLORED ROBOT CARD */}
            <div
              className="unit-card"
              style={{
                borderColor: main
                  ? `${selectedRobotColor}55`
                  : undefined,
              }}
            >
              <div
                className="unit-portrait"
                style={{
                  borderColor: main
                    ? selectedRobotColor
                    : undefined,
                  background: '#ffffff',
                }}
              >
                <RobotIcon
                  large
                  color={selectedRobotColor}
                />
              </div>

              <div className="unit-details">
                <div className="unit-topline">
                  AUTONOMOUS MOBILE ROBOT
                </div>

                <h3
                  style={{
                    color: main
                      ? selectedRobotColor
                      : undefined,
                  }}
                >
                  Robot {main
                    ? selectedRobotId
                    : `R${live
                        ? robot?.id ?? '–'
                        : 1}`}
                </h3>

                <span
                  className="status-chip"
                  style={{
                    color: selectedRobotColor,
                    borderColor: main
                      ? selectedRobotColor
                      : undefined,
                  }}
                >
                  {inspectorStatus}
                </span>
              </div>
            </div>

            {/* NAVIGATION INFORMATION */}
            <div className="section-divider-title">
              NAVIGATION DETAILS
            </div>

            <div className="detail-list">
              <div>
                <span>
                  Current position
                </span>

                <strong>
                  Row {inspectorPosition.row},
                  {' '}Col {inspectorPosition.column}
                </strong>
              </div>

              <div>
                <span>
                  {main ? 'Next target' : 'Destination'}
                </span>

                <strong>
                  Row {inspectorGoal.row},
                  {' '}Col {inspectorGoal.column}
                </strong>
              </div>

              <div>
                <span>
                  {main
                    ? 'Task stage'
                    : live
                      ? 'Task status'
                      : 'Movement stage'}
                </span>

                <strong>
                  {main
                    ? inspectorStatus
                    : live
                      ? task?.status ?? 'No task'
                      : `${step} / ${demoTotal} steps`}
                </strong>
              </div>

              <div>
                <span>
                  {main || live
                    ? 'Battery'
                    : 'Algorithm'}
                </span>

                <strong>
                  {main
                    ? `${activeFleetRobot.battery}%`
                    : live
                      ? robot
                        ? `${robot.battery}%`
                        : '–'
                      : 'A* Pathfinding'}
                </strong>
              </div>

              {main && (
                <div>
                  <span>
                    Robot movements
                  </span>

                  <strong>
                    {activeFleetRobot.steps} steps
                  </strong>
                </div>
              )}
            </div>

            {/* PROGRESS */}
            <div className="progress-block">
              <div className="progress-label">
                <span>
                  {main
                    ? 'FLEET TASK COMPLETION'
                    : live
                      ? 'TICK PROGRESS (ILLUSTRATIVE)'
                      : 'ROUTE COMPLETION'}
                </span>

                <strong
                  style={{
                    color: main
                      ? selectedRobotColor
                      : undefined,
                  }}
                >
                  {percent}%
                </strong>
              </div>

              <div
                className="progress-track"
                role="progressbar"
                aria-label="Simulation progress"
                aria-valuemin={0}
                aria-valuemax={100}
                aria-valuenow={percent}
              >
                <div
                  style={{
                    width: `${percent}%`,
                    background: main
                      ? selectedRobotColor
                      : undefined,
                  }}
                />
              </div>

              <div className="progress-caption">
                {main
                  ? `${completedCount} of ${fleet.robots.length} deliveries completed`
                  : live
                    ? 'Task completion comes from backend status.'
                    : 'Path playback for presentation'}
              </div>
            </div>

            {/* SIMULATION BUTTONS */}
            <div className="section-divider-title control-divider">
              SIMULATION CONTROLS
            </div>

            <div className="actions">
              <button
                type="button"
                className="btn btn-primary"
                disabled={!canTick}
                onClick={() =>
                  setPlaying(p => !p)
                }
              >
                <Icon
                  name={
                    playing
                      ? 'pause'
                      : 'play'
                  }
                />

                {playing ? 'Pause' : 'Start'}
              </button>

              <button
                type="button"
                className="btn btn-outline"
                disabled={!canTick}
                onClick={() => {
                  setPlaying(false);
                  void advance();
                }}
              >
                <Icon name="step" />
                Step
              </button>

              <button
                type="button"
                className="btn btn-outline"
                onClick={resetSimulation}
              >
                <Icon name="reset" />

                {main || !live
                  ? 'Reset'
                  : 'Refresh list'}
              </button>
            </div>

            <div className="side-note">
              {main
                ? 'Main Warehouse uses a local three-robot simulation with basic collision prevention. Final navigation algorithms will come from the backend.'
                : live
                  ? 'Live mode calls the ASP.NET Core API and modifies persisted simulation state.'
                  : 'Demo mode uses local example data and does not change the database.'}
            </div>
          </aside>
        </div>

        {/* FOOTER */}
        <footer>
          <span>
            © 2026 SDE Maniacs · Runtime Terror
          </span>

          <span>
            React + TypeScript · Phase 2 / Multi-Robot Simulation
          </span>
        </footer>
      </main>
    </div>
  );
}
