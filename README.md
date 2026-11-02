# Drone Flight Module

A prototype of a drone's core flight loop in Unity, built for hardware, control schemes and
services that change often. The visuals are deliberately primitive (cubes on a plane); the work
is in the boundaries, the failsafe and the tests.

The full design, including the parts that are planned but not built yet, is in
[Docs/Architecture.md](Docs/Architecture.md).

## Getting started

- **Unity 6000.3.8f1** (Unity 6.3) with URP. Packages restore on first open; VContainer comes
  from its Git URL, so Git must be on the path.
- Open `Assets/Scenes/DronePrototype.unity` and press Play.
- If the scene is missing or out of date, run **DroneFlightModule → Create Prototype Scene**. It
  rebuilds the scene from code.
- Tests: **Window → General → Test Runner → EditMode → Run All**.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move (forward, back, strafe) | W A S D | Right stick |
| Climb / descend | Space / Left Ctrl | Left stick up / down |
| Yaw | Q / E | Left stick left / right |
| Cut or restore the pilot's link | L | Select |

Bindings live in `Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions`, not in code.
Centred sticks hold position and altitude, so the drone hovers hands-off.

## What to try

1. Take off with Space, fly around, let go: the drone stops and hovers.
2. Press **L** to simulate losing the pilot's link. The HUD switches to **Holding** and counts
   down 10 s while the drone hovers in place and ignores input.
3. Press **L** again within 10 s to take back control, or wait: the drone descends at 1 m/s,
   slows to 0.3 m/s below 2 m, lands and cuts its motors.
4. With the link cut on the ground, the drone can't take off.

## How it fits together

```
Fixed tick ─► altitude sensor ─► pilot input (router) ─► flight controller
           ─► flight supervisor (gate open? pilot's command : failsafe command)
           ─► integrator ─► Ticked event ─► view, HUD (read-only)
```

| Assembly | Depends on | Role |
|---|---|---|
| `Core` | nothing (`noEngineReferences`) | Math, state, input routing, controllers, gates, failsafe supervisor, rangefinder, integrator, `FlightLoop` |
| `Unity` | Core, Input System | Input-actions provider, drone view with interpolation, debug HUD, follow camera |
| `Composition` | Core, Unity, VContainer | `LifetimeScope`, installer, `FlightLoopRunner` (the only place Unity's clock reaches the sim) |
| `Editor` | the above | Menu item that builds the prototype scene |
| `*.Tests` | their target | EditMode tests |

**Extension points.** Each is an interface with a constructor-injected implementation, so a new
variant is a new class plus one registration in `DroneInstaller`:

- `IInputProvider`: input sources (Input System actions today; phone remote planned)
- `IFlightController`: flight models (`VelocityFlightController`, `ForceFlightController`)
- `IFlightGate`: conditions for flight (`PilotInControlGate`, combined with `AllOfFlightGate`)
- `IAltitudeSensor`, `ITerrain`: sensors and ground

## Design decisions

- **Own physics instead of a Rigidbody.** A small semi-implicit Euler integrator in Core keeps
  the simulation deterministic, frame-rate independent and runnable without Unity, so the tests
  drive whole flights (take off, lose the link, hold, land) in milliseconds. The trade-off is
  no collisions beyond the terrain height, which this prototype doesn't need yet.
- **Core has no engine references.** It has its own `Vec3`, so the flight logic can't come to
  depend on `UnityEngine`; `UnityConversions` maps at the edge. Assembly definitions enforce
  every boundary.
- **Fixed-step simulation, interpolated view.** `FlightLoop` has no clock; `FlightLoopRunner`
  ticks it from `FixedUpdate`. The view blends the last two ticked states, so it renders up to
  one tick (20 ms) behind without stutter.
- **The failsafe is not a special case.** The supervisor only asks `IFlightGate` whether flight
  is allowed. Losing the pilot, and later losing authorization, both close the gate and get the
  same hold-then-descend behaviour. The countdown runs on simulation time.
- **VContainer, plain `Task`.** VContainer composes the scene; Core never references it.
  UniTask was left out because it would pull `UnityEngine` into the async services planned for
  Core-side code.
- **IMGUI for the debug HUD.** It's a developer readout, not product UI; the planned dashboard
  lives in its own assembly that can be deleted without breaking flight.
- **No per-tick allocations** in the simulation path. The HUD rebuilds its text at most once per
  rendered frame.

## Tests

- **Core**: 113 tests run headless in milliseconds. Unit tests for every class, plus
  closed-loop flights: hover, climb rate, top speed, hold for exactly 10 s, flared landing below
  0.3 m/s, a closed gate on the ground blocking takeoff.
- **Unity**: input bindings through `InputTestFixture` (keyboard and gamepad, the shipped
  asset), the interpolator, the chase camera and the HUD text.
- **Composition**: the real container: resolution, gate wiring, hover, link loss to landing,
  and disposal disabling the controls.

## Status and roadmap

Done (see the commit history): Core flight model, rangefinder, input routing, Unity runtime,
gating and failsafe, velocity controller, Input System controls, debug HUD and follow camera.

Planned, in order (details in the architecture doc): pilot registry, authorization providers
(guest and a simulated server), a flight coordinator for several drones with priority
takeover, a multi-drone scene, telemetry, a deletable dashboard, ScriptableObject config
assets, and a phone remote (protocol, host, UDP, iOS and Android app).

## Known limitations

- The drone doesn't pitch or roll; lateral force is applied directly. Good enough to exercise
  the loop, not a multirotor model.
- Vertical speed for the failsafe comes from the true state, standing in for an IMU.
- No PlayMode tests yet; the scene is covered through the container and component tests.
