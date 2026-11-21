
import { useRef, useState } from 'react';
import { Canvas, useFrame } from '@react-three/fiber';
import { OrbitControls, Text } from '@react-three/drei';
import type { Group } from 'three';
import type { Position } from '../../api';

export type WarehouseRobot3D = {
  id: string;
  position: Position;
  status?: string;
};

export type Warehouse3DProps = {
  rows: number;
  columns: number;
  robotPosition: Position;
  goal: Position;
  shelves: Position[];
  routePositions?: Position[];
  pickups?: Position[];
  dropoffs?: Position[];
  robots?: WarehouseRobot3D[];
  preview?: boolean;
};

function toWorld(
  p: Position,
  rows: number,
  columns: number
): [number, number, number] {
  return [
    p.column - (columns - 1) / 2,
    0,
    p.row - (rows - 1) / 2,
  ];
}

function StorageRack({
  position,
}: {
  position: [number, number, number];
}) {
  return (
    <group position={position}>
      {[-0.36, 0.36].flatMap(x =>
        [-0.33, 0.33].map(z => (
          <mesh
            key={`${x}-${z}`}
            position={[x, 0.72, z]}
            castShadow
          >
            <boxGeometry args={[0.055, 1.44, 0.055]} />
            <meshStandardMaterial
              color="#475569"
              metalness={0.5}
              roughness={0.5}
            />
          </mesh>
        ))
      )}

      {[0.3, 0.7, 1.1].map(height => (
        <group key={height}>
          <mesh position={[0, height, 0]} castShadow>
            <boxGeometry args={[0.8, 0.065, 0.72]} />
            <meshStandardMaterial color="#64748b" />
          </mesh>

          {[-0.18, 0.18].map(x => (
            <mesh
              key={x}
              position={[x, height + 0.15, 0]}
              castShadow
            >
              <boxGeometry args={[0.31, 0.27, 0.52]} />
              <meshStandardMaterial
                color={x < 0 ? '#c9a173' : '#d8b489'}
              />
            </mesh>
          ))}
        </group>
      ))}
    </group>
  );
}

function MovingRobot({
  target,
  id,
  color,
}: {
  target: [number, number, number];
  id: string;
  color: string;
}) {
  const ref = useRef<Group>(null);

  useFrame((_, delta) => {
    const robot = ref.current;
    if (!robot) return;

    const smooth = 1 - Math.exp(-5 * delta);

    robot.position.x +=
      (target[0] - robot.position.x) * smooth;

    robot.position.z +=
      (target[2] - robot.position.z) * smooth;
  });

  return (
    <group ref={ref} position={target}>
      {[-0.33, 0.33].map(x => (
        <mesh key={x} position={[x, 0.16, 0]}>
          <boxGeometry args={[0.12, 0.22, 0.5]} />
          <meshStandardMaterial color="#334155" />
        </mesh>
      ))}

      <mesh position={[0, 0.27, 0]} castShadow>
        <boxGeometry args={[0.64, 0.43, 0.64]} />
        <meshStandardMaterial
          color={color}
          metalness={0.2}
          roughness={0.4}
        />
      </mesh>

      <mesh position={[0, 0.55, 0]} castShadow>
        <boxGeometry args={[0.42, 0.14, 0.42]} />
        <meshStandardMaterial color="#ffffff" />
      </mesh>

      <mesh position={[0, 0.34, 0.33]}>
        <boxGeometry args={[0.43, 0.24, 0.018]} />
        <meshStandardMaterial color="#fff5f5" />
      </mesh>

      {[-0.1, 0.1].map(x => (
        <mesh key={x} position={[x, 0.37, 0.35]}>
          <sphereGeometry args={[0.035, 12, 12]} />
          <meshStandardMaterial color="#991b1b" />
        </mesh>
      ))}

      <mesh position={[0, 0.65, 0]}>
        <sphereGeometry args={[0.065, 16, 16]} />
        <meshStandardMaterial
          color="#22c55e"
          emissive="#16a34a"
          emissiveIntensity={0.5}
        />
      </mesh>

      <Text
        position={[0, 0.95, 0]}
        fontSize={0.25}
        color={color}
        anchorX="center"
        anchorY="middle"
      >
        {id}
      </Text>
    </group>
  );
}

function Station({
  position,
  type,
}: {
  position: [number, number, number];
  type: 'pickup' | 'dropoff';
}) {
  const pickup = type === 'pickup';
  const color = pickup ? '#16a085' : '#e11d48';

  return (
    <group position={position}>
      <mesh position={[0, 0.025, 0]}>
        <cylinderGeometry args={[0.35, 0.35, 0.04, 32]} />
        <meshStandardMaterial
          color={color}
          transparent
          opacity={0.7}
        />
      </mesh>

      <mesh position={[0.22, 0.35, 0]}>
        <cylinderGeometry args={[0.025, 0.025, 0.7, 12]} />
        <meshStandardMaterial color={color} />
      </mesh>

      <mesh position={[0.08, 0.53, 0]}>
        <boxGeometry args={[0.3, 0.2, 0.025]} />
        <meshStandardMaterial color={color} />
      </mesh>

      <Text
        position={[0, 1.02, 0]}
        fontSize={0.16}
        color={color}
        anchorX="center"
      >
        {pickup ? 'PICKUP' : 'GOAL'}
      </Text>
    </group>
  );
}

function WarehouseScene({
  rows,
  columns,
  robotPosition,
  goal,
  shelves,
  routePositions = [],
  pickups = [],
  dropoffs = [],
  robots,
}: Warehouse3DProps) {
  const shelfKeys = new Set(
    shelves.map(s => `${s.row},${s.column}`)
  );

  const routeKeys = new Set(
    routePositions.map(p => `${p.row},${p.column}`)
  );

  const displayedRobots = robots ?? [
    { id: 'R1', position: robotPosition },
  ];

  const goals = dropoffs.length
    ? dropoffs
    : [goal];

  const colors = [
    '#b4233a',
    '#2563eb',
    '#16a085',
    '#9333ea',
    '#d97706',
  ];

  return (
    <>
      <color attach="background" args={['#f8fafc']} />

      <ambientLight intensity={1.5} />

      <directionalLight
        position={[5, 12, 7]}
        intensity={2}
        castShadow
        shadow-mapSize={[2048, 2048]}
        shadow-camera-left={-25}
        shadow-camera-right={25}
        shadow-camera-top={25}
        shadow-camera-bottom={-25}
      />

      {/* Warehouse foundation */}
      <mesh position={[0, -0.13, 0]} receiveShadow>
        <boxGeometry
          args={[columns + 1.2, 0.2, rows + 1.2]}
        />
        <meshStandardMaterial color="#dce2e8" />
      </mesh>

      {/* Grid floor */}
      {Array.from({ length: rows }, (_, row) =>
        Array.from({ length: columns }, (_, column) => {
          const p = { row, column };
          const [x, , z] = toWorld(p, rows, columns);
          const id = `${row},${column}`;

          const isShelf = shelfKeys.has(id);
          const isRoute = routeKeys.has(id);

          return (
            <mesh
              key={id}
              position={[x, -0.015, z]}
              receiveShadow
            >
              <boxGeometry args={[0.96, 0.025, 0.96]} />
              <meshStandardMaterial
                color={
                  isShelf
                    ? '#e4e8ee'
                    : isRoute
                      ? '#fff0f3'
                      : '#ffffff'
                }
              />
            </mesh>
          );
        })
      )}

      {/* Storage racks */}
      {shelves.map(shelf => (
        <StorageRack
          key={`${shelf.row}-${shelf.column}`}
          position={toWorld(shelf, rows, columns)}
        />
      ))}

      {/* Pickup locations */}
      {pickups.map(p => (
        <Station
          key={`p-${p.row}-${p.column}`}
          position={toWorld(p, rows, columns)}
          type="pickup"
        />
      ))}

      {/* Delivery locations */}
      {goals.map(p => (
        <Station
          key={`g-${p.row}-${p.column}`}
          position={toWorld(p, rows, columns)}
          type="dropoff"
        />
      ))}

      {/* All robots, independently animated */}
      {displayedRobots.map((robot, index) => (
        <MovingRobot
          key={robot.id}
          id={robot.id}
          target={toWorld(robot.position, rows, columns)}
          color={colors[index % colors.length]}
        />
      ))}

      {/* Camera rotation, movement and zoom */}
      <OrbitControls
        makeDefault
        enableRotate
        enablePan
        enableZoom
        enableDamping
        dampingFactor={0.08}
        screenSpacePanning
        minDistance={2}
        maxDistance={Math.max(30, rows * 4, columns * 4)}
        minPolarAngle={0.1}
        maxPolarAngle={Math.PI / 2.02}
      />
    </>
  );
}

export default function Warehouse3D(
  props: Warehouse3DProps
) {
  const [cameraVersion, setCameraVersion] = useState(0);
  const size = Math.max(props.rows, props.columns, 4);

  return (
    <div className="warehouse-3d-wrapper">
      <div className="warehouse-3d-toolbar">
        <span className="warehouse-3d-status">
          <span className="warehouse-3d-dot" />
          {props.preview
            ? 'MAIN WAREHOUSE · MULTI-ROBOT SIMULATION'
            : 'INTERACTIVE 3D WAREHOUSE'}
        </span>

        <button
          type="button"
          className="warehouse-camera-reset"
          onClick={() => setCameraVersion(v => v + 1)}
        >
          Reset camera
        </button>
      </div>

      <div
        className="warehouse-3d-canvas"
        style={{
          width: '100%',
          height: '520px',
          touchAction: 'none',
        }}
      >
        <Canvas
          key={cameraVersion}
          shadows
          dpr={[1, 2]}
          camera={{
            position: [
              size * 0.95,
              size * 1.25,
              size * 1.3,
            ],
            fov: 45,
            near: 0.1,
            far: 250,
          }}
        >
          <WarehouseScene {...props} />
        </Canvas>
      </div>

      <div className="warehouse-3d-help">
        <span>Left drag: Rotate</span>
        <span>Right drag: Move</span>
        <span>Scroll: Zoom</span>
      </div>
    </div>
  );
}
