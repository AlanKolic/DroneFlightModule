# Drone Flight Module

A prototype of a drone fleet's core flight loop in Unity, built for hardware, control schemes
and services that change often. Three drones share the sky; a coordinator decides who flies
which, with authorization and priority takeover. The visuals are deliberately primitive (cubes
on a plane); the work is in the boundaries, the failsafe and the tests.

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
| Request any drone | R | South button |
| Request drone 1, 2 or 3 | 1 / 2 / 3 | |
| Release the drone | X | East button |
| Switch to the next pilot | P | Start |
| Make the server revoke your grant | V | |
| Cut or restore the pilot's link | L | Select |
| Add a drone | N | |
| Remove the newest drone | M | |
| Show or hide the fleet dashboard | Tab | |

Bindings live in `Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions`, not in code.
Centred sticks hold position and altitude, so the drone hovers hands-off.

You start as the **guest**, authorized at once by local guest mode, without a drone. **P**
switches to the next pilot in the registry, such as **Ace**, a registered pilot with a higher
priority who signs in through a simulated server that answers after 0.5 s. Switching leaves
the previous pilot on their drone with their hands off the sticks, so it hovers, and only the
selected pilot steers. That lets Ace take over the guest's drone from the same keyboard.

The control panel and the dashboard both track takeovers: the dropdowns say who flies what,
the panel's status shows the current pilot's takeovers and preemptions, and a pilot who loses
their drone is told who took it.

Every action also has a button in the **control panel** at the bottom right: a pilot
dropdown, a drone dropdown ("Any drone" or one of the fleet), and Request, Release, Revoke
grant, Cut/Restore link, Add drone and Remove drone (for the chosen drone).

## What to try

1. Press **R**: the guest gets a free drone, grounded ones first. The camera follows it.
2. Take off with Space, fly around, let go: the drone stops and hovers.
3. Press **2** or **3** to switch drones. You get the new one first, then the old one is
   released, so an airborne drone you leave holds for 10 s and lands by itself.
4. Press **L** to simulate losing the pilot's link. The HUD switches to **Holding** and counts
   down 10 s while the drone hovers in place and ignores input.
5. Press **L** again within 10 s to take back control, or wait: the drone descends at 1 m/s
   (0.5 m/s above the rangefinder's 10 m range), slows to 0.3 m/s below 2 m, lands and cuts its
   motors. Cutting the link again soon after restoring it resumes the countdown rather than
   restarting it.
6. With the link cut on the ground, the drone can't take off.
7. Press **N** to add a drone next to the others. It joins the fleet at once and is free, so
   **R** can give it to you. **M** removes the newest drone; if you were flying it, it's
   released first and the HUD tells you.
8. Press **P**, then **R**, to fly as Ace through the server. **V** makes the server revoke
   Ace's grant: the drone is released at once and the failsafe takes over.
9. Take a drone over: as the guest press **1**, then **P** to become Ace, who outranks the
   guest, and press **1** again. Ace takes drone-1, the guest is preempted, and the dashboard
   counts one takeover for Ace and one preemption for the guest.
10. Watch the fleet dashboard on the right: every drone's pilot, mode, measured and true
   height, position and heading; each pilot's stats; and the latest events, denials
   included. **Tab** hides it.
11. Or skip the keys: in the control panel, pick a pilot and a drone and press **Request**.

## How it fits together

```
Fixed tick ─► altitude sensor ─► pilot input (router) ─► flight controller
           ─► flight supervisor (gate open? pilot's command : failsafe command)
           ─► integrator ─► Ticked event ─► view, HUD (read-only)

Request ─► pilot registry ─► authorization provider ─► assignment policy (fresh fleet snapshot)
        ─► grant installed and router switched in one step ─► gate opens for that pilot
```

Each drone has its own child scope and ticks independently. The fleet scope holds what they
share: the coordinator, pilots and stats, the providers, and the local pilot's input.

| Assembly | Depends on | Role |
|---|---|---|
| `Core` | nothing (`noEngineReferences`) | Math, state, input routing, controllers, gates, failsafe supervisor, rangefinder, integrator, `FlightLoop` |
| `Unity` | Core, Input System | Input-actions provider, drone view with interpolation, debug HUD, follow camera |
| `Pilots` | Core | Pilot profiles and priorities, registry with a built-in guest, stats and their recorder |
| `Authorization` | Core | Providers (local guest, simulated server), revocable grants, per-drone grant holder gate, one drone per pilot |
| `Telemetry` | Core | A recorder per drone sampling its flight at 10 Hz, ring buffers, sinks (fleet-wide in-memory sink) |
| `Coordination` | Core, Pilots, Authorization | `FlightCoordinator`: pilot sessions, drone registry, request pipeline, priority assignment policy, takeover and release, stats from its events |
| `Composition` | everything above, VContainer, Input System | Fleet scope and installer, `DroneInstantiator` (adds and removes drones, one child scope each), the local pilot's keys, runners (the only places Unity's clock reaches the sim) |
| `UI` | Core, Pilots, Coordination, Telemetry, VContainer, Input System | Fleet dashboard in its own child scope; nothing references it, so it can be deleted |
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
  same hold-then-descend behaviour. The countdown runs on simulation time, and flying earns it
  back at the same rate, so a flapping link can't keep the drone hovering forever.
- **VContainer, plain `Task`.** VContainer composes the scene; Core never references it.
  UniTask was left out because it would pull `UnityEngine` into the async services planned for
  Core-side code.
- **One pilot, one drone.** A pilot controls at most one drone at a time, enforced where
  control is granted: each drone's grant holder claims the drone for its pilot in a shared
  `PilotClaims`, and refuses a pilot who already holds another. Revocation releases the claim.
  A pilot who asks the coordinator for another drone is switched to it, letting go of the first
  only once the second is secured, so a denied request leaves them where they were.
- **IMGUI for the debug HUD and the dashboard.** The HUD is a developer readout. The fleet
  dashboard lives in its own `UI` assembly and child scope, which nothing else references (the
  scene builder adds it by type name), so deleting the UI folder leaves flight, the scene and
  every other test intact.
- **No per-tick allocations** in the simulation path, telemetry included: samples go into
  preallocated ring buffers. The HUD rebuilds its text at most once per rendered frame.
- **Telemetry can't break flight.** It only reads, and a sink that throws is logged and
  skipped, so a broken sink never stops a tick.

## Tests

- **Core**: 117 tests run headless in milliseconds. Unit tests for every class, plus
  closed-loop flights: hover, climb rate, top speed, hold for exactly 10 s, flared landing below
  0.3 m/s, a closed gate on the ground blocking takeoff.
- **Unity**: input bindings through `InputTestFixture` (keyboard and gamepad, the shipped
  asset), the interpolator, the chase camera and the HUD text.
- **Pilots**: 24 tests for profiles, the registry's guest and priority rules, and stats
  recording across flights and takeovers, one drone per pilot at a time.
- **Authorization**: 50 tests for grants and revocation, the grant holder (a grant for one
  pilot never opens the gate for another, and a pilot controls at most one drone), the guest's
  local-only rule, and the server's latency, policy changes, cancellation and disposal. Async
  code runs with no synchronization context and a hand-completed delay, so the tests are
  synchronous and never sleep.
- **Coordination**: 51 tests on real flight loops: assignment preferring grounded drones,
  priority takeover with the gate open on every tick, equal priority never taking over,
  switching drones, denied switches, revocation, release, ending a session, cancelled and
  superseded requests, decisions on the fleet as it is once authorization completes, the
  drones joining and leaving at any time, the policy on its own, and stats timed by the simulation
  clock.
- **Dashboard**: 22 tests for the event log, the readout's text (run in a comma-decimal
  culture to keep the formatting invariant), Tab toggling the dashboard, and the control
  panel's pilot and drone choices, labels that follow a takeover, and each pilot's record.
- **Telemetry**: 18 tests for the ring buffer, sampling on simulation time, sink fan-out, a
  failing sink never breaking the flight, streams ending with the drone, and the in-memory
  sink.
- **Composition**: the real fleet container with two drone scopes, driven by key presses:
  each drone in its own scope, drones added and removed while the fleet is in use, free
  drones holding and landing, the guest getting a grounded drone, flying with the keyboard,
  number keys switching drones, release, signing in through the server, revocation, link
  loss, stats on simulation time, telemetry reaching the fleet sink, and disposal disabling the
  controls. The drone instantiator
  on its own: ids and spawn spots from the prefab, refused ids, removal and disposal.

## Status and roadmap

Done (see the commit history): Core flight model, rangefinder, input routing, Unity runtime,
gating and failsafe, velocity controller, Input System controls, debug HUD, follow camera and
the pilot registry with stats, authorization (guest and simulated server providers, revocable
grants, the grant holder gate), the flight coordinator (sessions, priority assignment and
takeover, switching, release, stats), and the fleet scene (a fleet scope with one child scope
per drone, three drones and more added or removed at runtime, the local pilot's keys),
telemetry (a recorder per drone, a fleet-wide in-memory sink), and the deletable fleet
dashboard.

Planned, in order (details in the architecture doc): ScriptableObject config assets, and a phone remote (protocol, host, UDP, iOS and Android app).

## Known limitations

- The drone doesn't pitch or roll; lateral force is applied directly. Good enough to exercise
  the loop, not a multirotor model.
- Vertical speed for the failsafe comes from the true state, standing in for an IMU.
- No PlayMode tests yet; the scene is covered through the container and component tests.
