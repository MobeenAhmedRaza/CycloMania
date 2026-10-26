# Part 9.1 — Riding basics tutorial

Subpart 9.1 implements the first mission from the design document: learning acceleration, braking and steering, adapted to the existing bicycle touch controls.

Open **Pause → Missions → Riding Basics → Start**. The tutorial is listed first and starts on the clear highway straight. The existing countdown, pause, retry, results and career rewards apply.

## Level 1 sequence

1. Hold **PEDAL** at or above 8 km/h for 2 continuous seconds.
2. Reach 6 km/h, then hold **BRAKE** below 1 km/h for 1 second.
3. Ride at least 4 km/h and hold **LEFT** to turn 20 degrees left.
4. Ride at least 4 km/h and hold **RIGHT** to turn 20 degrees right.
5. Reach 6 km/h again, then hold **BRAKE** below 1 km/h for 1 second to finish.

The objective HUD names the current control and shows time/angle progress. During braking lessons it first shows the speed needed to arm the stopping requirement. Steering uses the existing left/right buttons (keyboard arrows/A-D also work); no new tilt-control mode is introduced.

Pedaling requires grounded motion and positive controller input. Braking requires first moving at the specified speed, then grounded low speed with brake input. Pressing Brake while stationary cannot skip the lesson. Steering uses actual rigidbody heading change and matching steering input while grounded and moving; pressing a direction without turning does not count. Opposite physical rotation while holding that direction subtracts from turn progress. Countdown, paused, airborne and unrelated inputs cannot satisfy these lessons.

## Repeated levels

Level 2 raises the pedal requirement to 9 km/h for 2.5 seconds, turns to 25 degrees, and the final stop to 1.25 seconds; the time limit falls from 180 to 175 seconds.

Later levels increase these requirements up to 14 km/h for 6 seconds, 35-degree turns and a 2-second final stop, with a minimum 90-second limit. These bounds keep the lessons feasible on the starter bicycle. The mission framework's existing 100-tier limit, once-per-tier career rewards, retry and final-tier replay rules remain in effect.

## Progress compatibility and implementation

The tutorial is appended at catalog index 3 (`riding_basics`) while displayed first. Existing indices 0–2 remain unchanged. Loading the previous three-contract save extends its arrays with an uncompleted tutorial and retains prior points, completion tiers and best times.

New objective kinds are `PedalHold`, `BrakeStop` and `SteerTurn`. `RideMissions` supplies acceleration, steering and signed physics-heading deltas to `MissionSession`. The mission menu now fits four entries inside its existing safe-area panel. No changes are made to steering physics or touch bindings.

## Verification

- Core suite includes lesson ordering, false-positive prevention, real turn direction, continuous inputs, repeated difficulty and save expansion.
- Full-scene mission tests cover tutorial selection, countdown release and ground settling, controller telemetry through all five steps, completion/results and the harder next level. Telemetry is injected deterministically; this does not replace human ride-feel testing.
- Existing mission, garage, HUD, stunt, physics and touch checks remain in the suite.

Run `python3 Tests/CoreCycling/run.py`, `python3 Tests/Missions/run.py` and `python3 Tests/TouchControls/check_layout.py`. iPhone/iPad readability and riding feel still require device playtesting.
