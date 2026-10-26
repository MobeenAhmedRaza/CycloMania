# Part 9.7 — Police Escape

Open **Pause → Missions → page 4 → Police Escape → Start**. After the shared three-second countdown, hold **PEDAL + SPRINT** to build a lead over an AI police cyclist. Stay inside the marked highway pursuit area. The HUD shows the gap, required gap, continuous escape timer, remaining mission time, and a capture warning when police are close.

## Pursuit rules

The player starts at (850, 0, −240). Police start 18 m behind at z = −258, accelerate from rest at 2 m/s², and steer directly toward the player's horizontal position at the tier's maximum speed. The flat, unobstructed highway bounds are x = 840…860 and z = −260…400. Going outside them, or more than 5 m above/below road level, ends the attempt. Four ground lines mark the area.

- **Escape:** remain grounded and at least the required distance from police continuously for the target duration. Dropping below that gap or becoming airborne resets the escape timer.
- **Capture:** police remaining within 3 m continuously for 2 seconds ends the mission. Opening a larger gap resets the capture warning. Jumping does not bypass capture; distance is horizontal.
- **Deadline:** the attempt ends after 120 seconds if the player has not escaped.
- Police have no rubber banding or collision impulses. Their proximity, rather than physical collision, drives capture. This is a single-player bicycle patrol pursuit, without city navigation or multiple police units.

| Setting | Level 1 | Repeat progression |
| --- | --- | --- |
| Police speed | 14 km/h | +0.4 per level, capped at 22 km/h |
| Required lead | 35 m | +2 m per level, capped at 65 m |
| Continuous escape hold | 5 seconds | +0.5 per level, capped at 12 seconds |
| Career reward | 200 points | +25 per level, once per newly completed tier |

The maximum police speed stays below every stock bicycle's sprint speed. Automated constant-speed simulation verifies the slowest stock sprint can escape the hardest tier within the highway bounds; acceleration, input management and steering still require human balancing. The existing 100-tier career and final-tier reward cap apply.

## Runtime and presentation

`MissionSession` owns pursuit position, speed, gap, escape/capture timers and completion. It integrates in steps of at most 0.02 seconds, interpolating player motion between samples so a large tick cannot skip capture. Capture is checked before escape. Pause freezes all mission clocks, movement, pedaling and beacon phase. Countdown gives neither side progress.

`RideMissions` reuses the lightweight procedural cyclist visual, changing it to a blue police uniform with a red/blue beacon. It has no player controller, camera, rigid body or active collider. Race finish visuals are hidden during pursuit. Results and interruption hide both patrol and area lines. Owned materials and objects are cleaned up on teardown. The visual is provisional art.

Crash, reset, world travel, bicycle changes and abandonment use the shared failure rules. Retry resets police, timers and the same uncompleted level. Successful escape records rewards once and offers Next Level. The mission is appended at index 9; saves retain all previous nine mission records and selection expands to four pages.

## Validation

- `Tests/CoreCycling/run.py` includes `PoliceValidation`: countdown, acceleration, pause, escape, capture, capture-warning recovery, hold reset, airborne rejection, area boundaries, deadline, tick-size consistency, maximum-tier feasibility, repeated rewards and old-save migration.
- `Tests/Missions/run.py` covers page 4, countdown spawn, isolated patrol visuals, HUD, beacon pause, escape/capture results, rewards, harder retries, reset cleanup and restoring Rival Sprint's appearance.
- `Tests/TouchControls/check_layout.py` checks existing touch geometry and wiring.

Headless validation is not a visual or iPhone/iPad playtest. Device performance, pursuit feel and final art need hands-on review. The available editor lacks iOS Build Support, so device export/testing remains pending.
