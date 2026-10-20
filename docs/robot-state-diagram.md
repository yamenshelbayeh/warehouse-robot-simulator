
# Robot State Diagram

```mermaid
stateDiagram-v2
    [*] --> Idle
    Idle --> MovingToPickup: Task assigned
    MovingToPickup --> PickingUp: Arrived
    PickingUp --> MovingToDropoff: Item collected
    MovingToDropoff --> DroppingOff: Arrived
    DroppingOff --> Idle: Delivery completed
```

