import { useCallback, useEffect, useState } from 'react';
import { api, type Position, type WarehouseSnapshot } from './api';
import './style.css';

type Mode = 'demo' | 'live';
const route: Position[] = [
  {row:0,column:0},{row:1,column:0},{row:2,column:0},{row:2,column:1},
  {row:2,column:2},{row:2,column:3},{row:2,column:4},{row:3,column:4},
];
const demoShelves = new Set(['1,1','1,2','1,3','3,2']);
const same = (a: Position, b: Position) => a.row === b.row && a.column === b.column;
const key = (p: Position) => `${p.row},${p.column}`;
function Icon({name}: {name:'play'|'pause'|'step'|'reset'}) {
 const paths = {play:<path d="m8 5 11 7-11 7z" fill="currentColor"/>,pause:<path d="M8 5v14M16 5v14" strokeWidth="3"/>,step:<><path d="m5 5 9 7-9 7z"/><path d="M19 5v14"/></>,reset:<><path d="M3 11a9 9 0 1 1 2.6 6.4M3 4v7h7"/></>};
 return <svg width="17" height="17" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name]}</svg>;
}
function RobotIcon({large=false}: {large?:boolean}) {
 return <svg className={`robot-svg ${large?'large':''}`} viewBox="0 0 80 80" role="img" aria-label="Robot">
  <ellipse cx="40" cy="69" rx="24" ry="5" fill="#4f1020" opacity=".16"/>
  <rect x="12" y="35" width="8" height="24" rx="3" fill="#5e6878"/><rect x="60" y="35" width="8" height="24" rx="3" fill="#5e6878"/>
  <rect x="16" y="19" width="48" height="44" rx="12" fill="#a91e35" stroke="#861529" strokeWidth="2"/>
  <rect x="24" y="13" width="32" height="10" rx="5" fill="#7f162b"/>
  <rect x="22" y="28" width="36" height="25" rx="8" fill="#fff5f5"/>
  <circle cx="32" cy="39" r="4" fill="#ad263b"/><circle cx="48" cy="39" r="4" fill="#ad263b"/>
  <path d="M34 47q6 5 12 0" fill="none" stroke="#ad263b" strokeWidth="2" strokeLinecap="round"/>
 </svg>;
}
function ShelfIcon() {return <svg className="shelf-svg" viewBox="0 0 80 80" role="img" aria-label="Storage shelf">
 <rect x="13" y="10" width="54" height="60" rx="4" fill="#627187"/>
 {[15,34,53].map(y=><g key={y}><rect x="19" y={y} width="42" height="16" rx="2" fill="#caa77d" stroke="#ad8659" strokeWidth="1"/><path d={`M40 ${y}v16`} stroke="#ab865b" strokeWidth="1.5"/></g>)}
 <path d="M14 32h52M14 51h52" stroke="#405169" strokeWidth="3"/><rect x="11" y="9" width="5" height="63" rx="2" fill="#455366"/><rect x="64" y="9" width="5" height="63" rx="2" fill="#455366"/>
 </svg>}
function GoalIcon(){return <div className="goal-mark"><svg viewBox="0 0 40 42" aria-hidden="true"><path d="M11 37V6" stroke="currentColor" strokeWidth="3" strokeLinecap="round"/><path d="M13 7h22l-8 10 8 10H13z" fill="currentColor"/></svg><span>GOAL</span></div>}

export default function App() {
 const [mode,setMode] = useState<Mode>('demo');
 const [step,setStep] = useState(0);
 const [snapshot,setSnapshot] = useState<WarehouseSnapshot|null>(null);
 const [playing,setPlaying] = useState(false);
 const [busy,setBusy] = useState(false);
 const [error,setError] = useState('');
 const [apiState,setApiState] = useState('Not connected');
 const [warehouses,setWarehouses] = useState<{id:string;name:string}[]>([]);
 const [selectedId,setSelectedId] = useState('');
 const live = mode === 'live';
 const robot = snapshot?.robots[0];
 const task = snapshot?.tasks[0];
 const total = route.length-1;
 const demoFinished = step === total;
 const liveFinished = !!task && (task.status === 'Completed' || task.status === 'Cancelled');
 const finished = live ? liveFinished : demoFinished;
 const position = live ? (robot?.position ?? {row:0,column:0}) : route[step];
 const goal = live ? (task?.dropoff ?? {row:0,column:4}) : route[total];
 const rows = live ? snapshot?.rows ?? 1 : 4;
 const columns = live ? snapshot?.columns ?? 5 : 5;
 const routeCells = new Set(live ? [] : route.map(key));
 const visitedCells = new Set(live ? [] : route.slice(0,step+1).map(key));
 const cells = new Map(snapshot?.cells.map(c=>[key(c.position),c.type]) ?? []);
 const count = live ? (snapshot?.tick ?? 0) : step;
 const percent = live ? (liveFinished ? 100 : Math.min(95, Math.round(count / 6 * 100))) : Math.round(step / total * 100);
 const status = live ? (robot?.state ?? 'No robot') : finished?'Arrived':playing?'Moving':step?'Paused':'Ready';
 const canTick = live ? (!!snapshot && !!robot && !liveFinished && !busy) : !demoFinished;

 const refreshList = useCallback(async () => {
  setError('');setBusy(true);
  try { await api.ready();setApiState('Connected');const list=await api.warehouses();setWarehouses(list); }
  catch(e){setApiState('Unavailable');setError(e instanceof Error ? e.message : String(e));}
  finally {setBusy(false);}
 },[]);
 const loadWarehouse = useCallback(async (id: string) => {
  setError('');setBusy(true);setPlaying(false);
  try {const result=await api.warehouse(id);setSnapshot(result);setSelectedId(id);}
  catch(e){setError(e instanceof Error ? e.message : String(e));}
  finally{setBusy(false);}
 },[]);
 const createLive = useCallback(async () => {
  setPlaying(false);setError('');setBusy(true);
  try {await api.ready();setApiState('Connected');const result=await api.createDeliveryDemo();setSnapshot(result);setSelectedId(result.id);
    const list=await api.warehouses();setWarehouses(list);}
  catch(e){setApiState('Unavailable');setError(e instanceof Error ? e.message : String(e));}
  finally {setBusy(false);}
 },[]);
 const advance = useCallback(async () => {
  if(!live){setStep(s=>Math.min(total,s+1));return;}
  if(!snapshot || busy || liveFinished)return;
  setBusy(true);setError('');
  try {setSnapshot(await api.tick(snapshot));}
  catch(e){setPlaying(false);setError(e instanceof Error ? e.message : String(e));}
  finally{setBusy(false);}
 },[live,snapshot,busy,liveFinished,total]);
// Automatic movement for demo mode
useEffect(() => {
  if (!playing || live || demoFinished) return;

  const timer = window.setInterval(() => {
    setStep(current => Math.min(current + 1, total));
  }, 850);

  return () => window.clearInterval(timer);
}, [playing, live, demoFinished, total]);

// Automatic movement for live backend mode
useEffect(() => {
  if (!playing || !live || finished || !snapshot || busy) return;

  const timer = window.setTimeout(() => {
    void advance();
  }, 850);

  return () => window.clearTimeout(timer);
}, [playing, live, finished, snapshot, busy, advance]);
 useEffect(()=>{if(finished)setPlaying(false)},[finished]);
 function toggleMode(next:Mode){setPlaying(false);setError('');setMode(next);}
 return <div className="app">
  <div className="accent-line"/>
  <header className="topbar"><div className="topbar-content"><div className="branding"><span className="brand-icon">▦</span><span><strong>WAREHOUSE<span className="red">/OS</span></strong><small>SDE MANIACS · RUNTIME TERROR</small></span></div><div className="header-right"><span className="workspace-tag"><span className="online-dot"/>Prototype workspace</span><span className="user-avatar">RT</span></div></div></header>
  <main className="container">
   <div className="breadcrumbs"><span>Workspace</span><span>/</span><strong>Simulation overview</strong></div>
   <section className="hero"><div className="hero-content"><span className="hero-eyebrow">FLEET CONTROL CENTER · PRESENTATION PROTOTYPE</span><h1>Warehouse Robot Simulator</h1><p>Visualize robot navigation and connect to the actual ASP.NET Core simulation API.</p></div><span className="hero-badge">{live?'● LIVE API MODE':'● A* DEMO MODE'}</span></section>
   <div className="integration-bar"><div><strong>Data source</strong><span>Choose demonstration playback or Yamen's actual backend</span></div><div className="mode-switch"><button className={mode==='demo'?'active':''} onClick={()=>toggleMode('demo')}>A* demo</button><button className={live?'active':''} onClick={()=>toggleMode('live')}>Live backend</button></div></div>
   {live && <div className="backend-tools"><span><strong>API:</strong> {apiState}</span><button onClick={()=>void refreshList()} disabled={busy}>Check connection / refresh</button><select aria-label="Select warehouse" value={selectedId} onChange={e=>void loadWarehouse(e.target.value)} disabled={busy}><option value="">Select a warehouse...</option>{warehouses.map(w=><option key={w.id} value={w.id}>{w.name}</option>)}</select><button className="create-button" onClick={()=>void createLive()} disabled={busy}>Create live delivery demo</button></div>}
   {error && <div className="error-banner" role="alert"><strong>Connection / API error:</strong> {error}<div>Ensure Yamen's API and PostgreSQL are running on port 5080. If this was a revision conflict, refresh the warehouse.</div></div>}
   <section className="kpis" aria-label="Simulation metrics">
    <div className="kpi"><span className="kpi-label">ROBOTS</span><strong>{live?(snapshot?.robots.length ?? 0):1}</strong><span className="kpi-detail">Units in current warehouse</span></div>
    <div className="kpi"><span className="kpi-label">WAREHOUSE GRID</span><strong>{rows} × {columns}</strong><span className="kpi-detail">Grid cells · row and column</span></div>
    <div className="kpi"><span className="kpi-label">{live?'CURRENT TICK':'PATH LENGTH'}</span><strong>{live?count:total}</strong><span className="kpi-detail">{live?'Backend simulation ticks':'A* demonstration steps'}</span></div>
    <div className="kpi"><span className="kpi-label">STATUS</span><strong className="state-title">{status}</strong><span className="kpi-detail">{live?(task?.status ?? 'No task'):`${percent}% complete`}</span></div>
   </section>
   <div className="dashboard">
    <section className="panel warehouse-panel"><div className="panel-head"><div><span className="section-eyebrow">01 / MAP MONITOR</span><h2>Warehouse visualization</h2><p>Robot positions, storage, pickup and destination</p></div><span className="head-tag">{rows} rows × {columns} cols</span></div>
     <div className="map-toolbar"><div className="toolbar-label"><span className="live-indicator"/>FLOOR PLAN · LEVEL 01</div><div className="map-grid-reference">ORIGIN (0, 0) · ROW / COLUMN</div></div>
     <div className="map-container"><div className="grid-with-labels" style={{gridTemplateColumns:`25px repeat(${columns}, minmax(0, 1fr))`,width:`min(100%,${Math.max(420,columns*95)}px)`}}>
      <div className="corner-axis"/>{Array.from({length:columns},(_,c)=><div className="axis-label" key={`h${c}`}>{String(c).padStart(2,'0')}</div>)}
      {Array.from({length:rows},(_,r)=><div key={`row-${r}`} style={{display:'contents'}}><div className="axis-label row-axis">{String(r).padStart(2,'0')}</div>{Array.from({length:columns},(_,c)=>{
       const p={row:r,column:c};const t=live?(cells.get(key(p))??'Empty'):(demoShelves.has(key(p))?'Shelf':'Empty');
       const shelf=t==='Shelf',pickup=t==='Pickup',dropoff=t==='Dropoff';
       const isRobot=live ? snapshot?.robots.some(x=>same(x.position,p))??false : same(p,position);
       const isGoal=live?dropoff:same(p,goal);
       const onRoute=routeCells.has(key(p)),visited=visitedCells.has(key(p));
       return <div key={key(p)} title={`Row ${r}, column ${c} · ${t}${isRobot?' · Robot':''}`} className={`cell ${shelf?'cell-shelf':''} ${onRoute?'cell-route':''} ${visited?'cell-visited':''} ${isGoal?'cell-goal':''} ${isRobot?'cell-robot':''}`}>
        {isRobot?<div className={`robot-unit ${playing?'robot-moving':''}`}><RobotIcon/><span className="robot-unit-tag">R{live?(snapshot?.robots.find(x=>same(x.position,p))?.id??1):1}</span></div>:shelf?<ShelfIcon/>:isGoal?<GoalIcon/>:pickup?<span className="pickup-mark">PICKUP</span>:onRoute?<span className="route-marker"/>:null}
       </div>;
      })}</div>)}
     </div></div>
     <div className="legend"><div><span className="legend-robot"><RobotIcon/></span> Robot</div><div><span className="legend-shelf"><ShelfIcon/></span> Shelf</div><div><i className="legend-route"/> {live?'Pickup / travel':'A* route'}</div><div><i className="legend-finish"/> Goal</div></div>
     <div className="map-footnote">{live?'Coordinates and robot positions come directly from the backend snapshot. Route overlays are shown only in local A* demo mode.':'This sample path matches Erfan’s seven-step A* console demonstration. Switch to Live backend to view real robot positions.'}</div>
    </section>
    <aside className="panel operations-panel"><div className="panel-head"><div><span className="section-eyebrow">02 / FLEET STATUS</span><h2>Robot inspector</h2><p>Position, state, task and simulation controls</p></div><span className="head-tag">{live?'API':'DEMO'}</span></div>
     <div className="unit-card"><div className="unit-portrait"><RobotIcon large/></div><div className="unit-details"><div className="unit-topline">AUTONOMOUS MOBILE ROBOT</div><h3>Robot R{live?(robot?.id??'–'):1}</h3><span className="status-chip">{status}</span></div></div>
     <div className="section-divider-title">NAVIGATION DETAILS</div><div className="detail-list"><div><span>Current position</span><strong>Row {position.row}, Col {position.column}</strong></div><div><span>Destination</span><strong>Row {goal.row}, Col {goal.column}</strong></div><div><span>{live?'Task status':'Movement stage'}</span><strong>{live?(task?.status??'No task'):`${step} / ${total} steps`}</strong></div><div><span>{live?'Battery':'Algorithm'}</span><strong>{live?(robot?`${robot.battery}%`:'–'):'A* Pathfinding'}</strong></div></div>
     <div className="progress-block"><div className="progress-label"><span>{live?'TICK PROGRESS (ILLUSTRATIVE)':'ROUTE COMPLETION'}</span><strong>{percent}%</strong></div><div className="progress-track" role="progressbar" aria-label="Simulation progress" aria-valuemin={0} aria-valuemax={100} aria-valuenow={percent}><div style={{width:`${percent}%`}}/></div><div className="progress-caption">{live?'Task completion is determined by the backend status.':'Path playback for presentation'}</div></div>
     <div className="section-divider-title control-divider">SIMULATION CONTROLS</div><div className="actions"><button className="btn btn-primary" disabled={!canTick} onClick={()=>setPlaying(p=>!p)}><Icon name={playing?'pause':'play'}/>{playing?'Pause':'Start'}</button><button className="btn btn-outline" disabled={!canTick} onClick={()=>{setPlaying(false);void advance()}}><Icon name="step"/>Step</button><button className="btn btn-outline" onClick={()=>{setPlaying(false);if(live){void refreshList()}else{setStep(0)}}}><Icon name="reset"/>{live?'Refresh list':'Reset'}</button></div>
     <div className="side-note">{live?'Live mode sends requests to Yamen’s ASP.NET Core API. It modifies persisted simulation state; no reset endpoint exists, so create a fresh delivery demo to restart.':'Demo mode uses local example data and does not change the database.'}</div>
    </aside>
   </div>
   <footer><span>© 2026 SDE Maniacs · Runtime Terror</span><span>React + TypeScript · Prototype build 0.3</span></footer>
  </main>
 </div>
}
