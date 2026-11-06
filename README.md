# Drone Flight Module

A prototype of a drone fleet's core flight loop in Unity, built for hardware, control schemes
and services that change often. Three drones share the sky; a coordinator decides who flies
which, with authorization and priority takeover. The visuals are deliberately primitive (cubes
on a plane); the work is in the boundaries, the failsafe and the tests.

The full design is in
[Docs/Architecture.md](Docs/Architecture.md).

## Getting started

- **Unity 6000.3.8f1** (Unity 6.3) with URP. Packages restore on first open; VContainer comes
  from its Git URL, so Git must be on the path.
- Open `Assets/Scenes/DronePrototype.unity` and press Play.
- If the scene is missing or out of date, run **DroneFlightModule → Create Prototype Scene**. It
  rebuilds the scene from code.
- Tuning values (drone frame, controller, rangefinder, failsafe, telemetry, pilots, server
  latency, fleet layout, the input devices that get a pilot station) are ScriptableObject assets
  in `Assets/DroneFlightModule/Config`; `FleetSettings` points at the rest.
- Tests: **Window → General → Test Runner → EditMode → Run All**.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Move (forward, back, strafe) | W A S D | Right stick |
| Climb / descend | Space / Left Ctrl | Left stick up / down |
| Yaw | Q / E | Left stick left / right |
| Request any drone | R | South button |
| Request drone 1, 2 or 3 | 1 / 2 / 3 | D-pad left / up / right |
| Release the drone | X | East button |
| Arm or disarm a grounded drone | F | West button |
| Swap this desk between its device and the autopilot | T | D-pad down |
| Seat the next free pilot here (the pilot here leaves and lets go of their drone) | P | Start |
| Make the server revoke your grant | V | North button |
| Cut or restore the station's link | L | Select |
| Add a drone | N | |
| Remove the newest drone | M | |
| Show or hide the fleet dashboard | Tab | |

Bindings live in `Assets/DroneFlightModule/Unity/Input/DroneControls.inputactions`, not in code.
Centred sticks hold position and altitude, so the drone hovers hands-off.

**Every input device is its own pilot station.** The keyboard and each gamepad, including one
plugged in while the scene runs, fly as separate pilots on separate drones, and none reads the
others' buttons. A new desk takes the guest if nobody has them and otherwise starts empty, so the
keyboard is ready to fly and you pick who sits at a new device. Unplugging a gamepad is link loss:
its drone holds and lands, and plugging it back in resumes, with the same pilot and station. A pad
silent for 15 s counts as unplugged until it next sends something, because macOS can drop a
Bluetooth pad without reporting it. A drone that lands with no link is released, and a gone device
with no drone loses its station. Adding and removing drones and the dashboard stay on the
keyboard, since they act on the whole fleet. The HUD lists the stations.

**The screen panel** is one more input device: pick "Screen panel" in the control panel's station
dropdown and on-screen flight buttons appear at the bottom left. Pick a pilot, Request a drone, press
Arm, then hold the buttons to fly. They drive a virtual gamepad, so the panel has its own station like any other
device, once you pick a pilot for it, and pressing a key or pad button elsewhere doesn't affect
it. A mouse holds one button at a time, so moves can't be combined.

You start as the **guest**, authorized at once by local guest mode, without a drone. Each device
is a desk with at most one pilot. **P** seats the next pilot here: first nobody, then **John** (60),
**Ace** (50) and the guest, in turn, skipping a pilot at another desk. John and Ace sign in through a simulated server that answers
after 0.5 s. Changing the pilot at a desk means the old pilot leaves: their drone is released and
holds, their pending request is cancelled, and the new pilot requests a drone like anyone else.
Requesting any drone while you fly one keeps you on it. A pilot with no link can't request a drone
at all. To move a pilot to another device, pick them there: their session and drone go with them
and the flight carries on, and the desk they left is empty.

The control panel and the dashboard both track takeovers: the dropdowns say who flies what,
the panel's status shows the current pilot's takeovers and preemptions, and a pilot who loses
their drone is told who took it.

Every action also has a button in the **control panel** at the bottom right: a station
dropdown (the panel acts for the chosen station, and the camera follows its drone), a pilot
dropdown, a drone dropdown ("Any drone" or one of the fleet), and Request, Release, Revoke
grant, Cut/Restore link, Add drone and Remove drone (for the chosen drone). Its status line is
where the latest outcome shows: what you asked for, a denial, or that someone took your drone.

## What to try

1. Press **R**: the guest gets a free drone, grounded ones first. The camera follows it.
2. Press **F** to arm it, then take off with Space, fly around, let go: the drone stops and
   hovers. Until you arm, a grounded drone ignores the sticks, and touchdown disarms it again.
3. Press **2** or **3** to switch drones. You get the new one first, then the old one is
   released, so an airborne drone you leave holds for 10 s and lands by itself.
4. Press **L** to simulate losing the pilot's link. The dashboard shows **Holding** and counts
   down 10 s while the drone brakes to a stop, hovers and ignores input.
5. Press **L** again within 10 s to take back control, or wait: the drone descends at 1 m/s
   (0.5 m/s above the rangefinder's 10 m range), slows to 0.3 m/s below 2 m, lands and cuts its
   motors. Cutting the link again soon after restoring it resumes the countdown rather than
   restarting it.
6. A drone that lands with no link is released, and so is a grounded one whose link you cut. With
   the link cut, a request is refused, so restore it first.
7. Press **N** to add a drone next to the others. It joins the fleet at once and is free, so
   **R** can give it to you. **M** removes the newest drone; if you were flying it, it's
   released first and the control panel tells you.
8. Press **P** twice, then **R** and **F**, to fly as John through the server. **V** makes the server revoke
   John's grant: the drone is released at once and the failsafe takes over.
9. Take a drone over: press **P** twice to get back to the guest, then press **1**. In the control panel choose the Screen panel
   station, pick John, choose drone-1 and press **Request**. John outranks the guest, so he takes
   it, the guest is preempted, and the dashboard counts one takeover for John and one preemption
   for the guest.
10. Plug in a gamepad: it appears in the HUD as `Gamepad`, empty. Press Start to seat a pilot,
    then **R** and **F** on the keyboard and **South** and **West** on the pad, and the two fly
    separate drones at once.
    Unplug the pad and its drone holds, then lands; plug it back in within the hold to take over
    again.
11. Watch the fleet dashboard on the right: every drone's pilot, authorization state, mode,
   measured and true height, speed, position and heading; each pilot's authorization state
   and stats; and the latest events, denials included. **Tab** hides it.
12. Or skip the keys: in the control panel, pick a pilot and a drone and press **Request**, then
    **Arm**.
13. See the gate refuse: release your drone with **X** and press **F**. Nothing arms, the desk says
    why, and the dashboard's events show the refusal.
14. Swap the input in mid-flight: while flying, press **T**. The same pilot and drone now fly a
    climbing square from the autopilot, with nothing rebuilt. **T** gives you back your device,
    **L** still cuts the link, and unplugging the device turns the autopilot off, so the failsafe
    lands the drone. The autopilot can't arm: on the ground it waits for **F**, then takes off.

## How it fits together

```
Fixed tick ─► altitude sensor ─► pilot input (router) ─► flight controller
           ─► flight supervisor (gate open? pilot's command : failsafe command)
           ─► integrator ─► Ticked event ─► view, telemetry (read-only)

Request ─► pilot registry ─► authorization provider ─► assignment policy (fresh fleet snapshot)
        ─► grant installed and router switched in one step ─► gate opens for that pilot
```

Each drone has its own child scope and ticks independently. The fleet scope holds what they
share: the coordinator, pilots and stats, the providers, and a pilot station per input device.

| Assembly | Depends on | Role |
|---|---|---|
| `Core` | nothing (`noEngineReferences`) | Math, state, input routing, controllers, gates, failsafe supervisor, rangefinder, integrator, `FlightLoop` |
| `Unity` | Core, Input System | Input-actions provider, drone view with interpolation, follow camera |
| `Pilots` | Core | Pilot profiles and priorities, registry with a built-in guest, stats and their recorder |
| `Authorization` | Core | Providers (local guest, simulated server that can revoke), revocable grants, per-drone grant holder gate |
| `Telemetry` | Core | A recorder per drone sampling its flight at 10 Hz, sinks (a fleet-wide in-memory sink of the latest samples) |
| `Coordination` | Core, Pilots, Authorization | `FlightCoordinator`: pilot sessions, drone registry, request pipeline, priority assignment policy, takeover and release, stats from its events |
| `Composition` | everything above, VContainer, Input System | Fleet scope and installer, config assets mapped to `FleetSettings`, `DroneInstantiator` (adds and removes drones, one child scope each), a pilot station per input device, `FleetKeys` (fleet-wide keys), the `IFleetConsole` a panel drives, runners (the only places Unity's clock reaches the sim) |
| `UI` | Core, Unity, Pilots, Coordination, Telemetry, Composition, VContainer, Input System | Fleet dashboard, debug HUD, control panel and on-screen flight buttons in their own child scope; nothing references it, so it can be deleted |
| `Editor` | the above | Menu item that builds the prototype scene |
| `*.Tests` | their target | EditMode tests |

**Extension points.** Each is an interface with a constructor-injected implementation, so a new
variant is a new class plus one registration in `DroneInstaller`:

- `IFlightController`: flight models (`VelocityFlightController`)
- `IFlightGate`: conditions for flight (`PilotInControlGate`, combined with `AllOfFlightGate`)
- `IAltitudeSensor`, `ITerrain`: sensors and ground

Three others are added elsewhere:

- A new Input System device type is data only: a control scheme in the controls asset and one
  entry in `Config/InputDevices.asset`. An input source that isn't an Input System device, such
  as a network remote, needs its own code that makes `PilotSession`s.
- A new `IAuthorizationProvider` is a new class, one registration, and the rule for who uses it
  in an `IAuthorizationProviderSelector`.
- A new `ITelemetrySink` is a new class and one registration, as `ITelemetrySink`, in
  `FleetInstaller`.

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
  is allowed. Losing the pilot and losing authorization both close the gate and get the
  same hold-then-descend behaviour. The countdown runs on simulation time, and flying earns it
  back at the same rate, so a flapping link can't keep the drone hovering forever. Holding and
  descending brake horizontal drift actively, so a drone that loses its link at full speed
  stops within a few metres.
- **VContainer, plain `Task`.** VContainer composes the scene; Core never references it.
  UniTask was left out because it would pull `UnityEngine` into the pure C# async services.
- **One pilot, one drone, one owner.** A pilot controls at most one drone at a time, whichever
  session they use, and the coordinator alone keeps that record; grant holders only gate. A
  pilot who asks for another drone is switched to it, letting go of the first only once the
  second is secured, so a denied request leaves them where they were.
- **Events after the fact, and isolated.** The coordinator raises its events only once a change
  is complete, and a listener that throws is reported through `ListenerFailed`, so a broken
  stats or UI listener can't leave a pilot half switched.
- **One timebase.** A drone that joins a running fleet starts its loop at the fleet's
  simulation time, so telemetry, stats and the event log all share one clock.
- **IMGUI for the debug HUD, the dashboard and the control panel.** The HUD is a developer readout
  of the stations and the controls; the drones are on the dashboard. The dashboard and the control
  panel, which is the one place a station's full status shows, live in their own `UI` assembly and
  child scope, which nothing else references (the scene builder adds it by type name), and whose
  tests sit inside it, so deleting the UI folder leaves flight, the scene and every other test
  intact.
- **Input sources don't matter.** A pilot station per input device, each on its own copy of
  the controls restricted to that device, so the coordinator only ever sees sessions. Which
  devices get one is configured in `Config/InputDevices.asset` (a layout and a binding group
  each), so a new device type is a control scheme and one entry there, with no code.
- **Keys in two places.** `StationKeys` binds a station's buttons and `FleetKeys` the
  fleet-wide ones; the keys and the control panel drive the same objects. Only the dashboard's
  Tab toggle stays with the dashboard, so it goes when the UI is deleted.
- **No per-tick allocations** in the simulation path, telemetry included. The UI builds its text a
  few times a second, and OnGUI only draws.
- **Telemetry can't break flight.** It only reads, and a sink that throws is logged and
  skipped, so a broken sink never stops a tick.

## Tests

- **Core**: 143 tests run headless in milliseconds. Unit tests for every class, plus
  closed-loop flights: hover, climb rate, top speed, hold for exactly 10 s, braking to a stop
  when the link drops at full speed, flared landing below 0.3 m/s, a closed gate on the ground
  blocking takeoff, sampling that averages its rate on any tick step, and a loop that starts
  at the fleet's time.
- **Unity**: input bindings through `InputTestFixture` (keyboard and gamepad, the shipped
  asset), the interpolator and the chase camera.
- **Pilots**: 24 tests for profiles, the registry's guest and priority rules, and stats
  recording across flights and takeovers.
- **Authorization**: 38 tests for grants and revocation, the grant holder (a grant for one pilot
  never opens the gate for another), guest mode, and the server's latency, policy changes,
  cancellation and disposal. Async code runs with no synchronization context and a hand-completed
  delay, so the tests are synchronous and never sleep.
- **Coordination**: 90 tests on real flight loops: assignment preferring grounded drones, priority
  takeover with the gate open on every tick, equal priority never taking over, switching drones,
  denied switches, keeping your drone on "any drone", a second session for the same pilot, ending
  and re-routing sessions, a throwing listener that can't interrupt a switch or reach a revoker,
  events that see the change complete, revocation, release, a pilot with no link refused and their
  landed drone released, cancelled and superseded requests, decisions on the fleet as it is once
  authorization completes, the drones joining and leaving at any time, the policy on its own, and
  stats timed by the simulation clock.
- **Dashboard**: 42 tests for the event log, the readout's text (run in a comma-decimal culture to
  keep the formatting invariant), Tab toggling the dashboard, and the control panel's pilot and
  drone choices, labels that follow a takeover, the status line, the screen panel's virtual
  gamepad, and the HUD's text.
- **Telemetry**: 14 tests for sampling on simulation time, sink fan-out, a failing sink never
  breaking the flight, streams ending with the drone, and the in-memory sink.
- **Composition**: the real fleet container with two drone scopes, driven by key presses: each
  drone in its own scope, drones added and removed while the fleet is in use, free drones holding
  and landing, the guest getting a grounded drone, flying with the keyboard, number keys switching
  drones, release, signing in through the server, changing the pilot cancelling a pending request,
  revocation, link loss, stats on simulation time, drones
  added later sharing the fleet's timebase, telemetry reaching the fleet sink, and disposal
  disabling the controls. The drone instantiator on its own: ids and spawn spots from the prefab,
  refused ids, a late drone starting at the fleet's time, removal and disposal. The config assets:
  defaults matching Core, edits and invalid values reaching the Core types, a missing part named,
  and the shipped settings making a valid fleet. Input devices with virtual gamepads: a station
  per device and its starting pilot, the keyboard and pads flying separate drones at once,
  unplugging as link loss and plugging back in, one pilot on two devices, takeovers between
  stations, per-station links, a station with no link refused and its landed drone freed, starting
  pilots, the console's station choice, the camera following it, and disposal. The control panel's
  IMGUI drawing itself is not tested; its text and choices are.

## Status and roadmap

Done (see the commit history): Core flight model, rangefinder, input routing, Unity runtime,
gating and failsafe, velocity controller, Input System controls, debug HUD, follow camera and
the pilot registry with stats, authorization (guest and simulated server providers, revocable
grants, the grant holder gate), the flight coordinator (sessions, priority assignment and
takeover, switching, release, stats), and the fleet scene (a fleet scope with one child scope
per drone, three drones and more added or removed at runtime), telemetry (a recorder per
drone, a fleet-wide in-memory sink), the deletable fleet dashboard, ScriptableObject config
assets for every tuning value, and a pilot station per input device, so the keyboard and each
gamepad fly as separate pilots.

Every planned commit is done.

## Known limitations

- The number keys and D-pad ask for `drone-1` to `drone-3` by id, so they don't reach drones with
  other ids; the control panel reaches every drone.
- macOS can drop a Bluetooth pad without reporting it, and a silent pad can't be told from an
  idle one. So a pad that sends nothing for its silence timeout (15 s, in
  `Config/InputDevices.asset`) counts as disconnected until it next sends something. A pilot who
  leaves the pad untouched that long sees the drone hold and descend, and a grounded drone is
  released; touching the pad restores the link. Until then a dropped pad's last input stays in
  force, and a stick held perfectly still may count as silence.
- A pad with no serial that returns as a new device gets a new station.
- The drone doesn't pitch or roll; lateral force is applied directly. Good enough to exercise
  the loop, not a multirotor model.
- Vertical speed for the failsafe comes from the true state, standing in for an IMU.
- No PlayMode tests yet; the scene is covered through the container and component tests.
