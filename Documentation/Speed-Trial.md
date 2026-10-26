# Part 9.3 — Speed trial

Open **Pause → Missions → Speed Trial → Start**. This implements the third mission from the design document: reach a trial start point, then maintain speed continuously. The document's motorcycle target is replaced with bicycle-scale speeds.

## Level 1

1. Spawn at the highway destination and wait for the existing three-second countdown.
2. Reach the start marker at (850, 0, -180).
3. Hold at least **18 km/h for 10 continuous seconds** while grounded inside the marked highway zone.
4. Finish within the overall **120-second** mission limit.

The trial zone is 20 m wide and 580 m long, from x=840 to x=860 and z=-180 to z=400. Its vertical bounds run from y=-4 to y=4; grounded contact is also required. Four gold lines mark its boundary without adding collision obstacles. The existing minimap shows the nearby lines and entry marker.

The objective HUD shows actual/required speed, current/required hold duration, and time remaining. It tells the rider to build speed, return inside the zone, or land when those requirements are not met. **PEDAL + SPRINT** is the suggested input; the objective measures actual horizontal speed, not button presses.

Dropping below the target, becoming airborne or leaving the zone resets the continuous hold to zero. The overall clock continues. Pausing freezes both clock and hold. Speed achieved before reaching the entry marker does not count.

## Repeated difficulty

- Target speed: 18 + 0.5 × (level − 1) km/h, capped at 28 km/h.
- Continuous duration: 10 + 2 × (level − 1) seconds, capped at 35 seconds.
- Overall time: 120 − 2 × (level − 1) seconds, with a 65-second minimum.

Level 2 requires **18.5 km/h for 12 seconds within 118 seconds**. All target speeds remain below every stock bicycle's configured sprint cap; the duration cap keeps the run within the available highway. Final difficulty values are bounded, rather than scaling beyond the bicycles or map.

Each newly completed tier grants **150 + 25 × (level − 1) career points**, once per tier. Existing retry, abandon, crash/reset/travel interruption, 100-tier progression and final-tier replay rules apply. Completion/interruption hides the zone lines.

## Menu and save compatibility

The mission menu now has two pages of three entries, retaining larger buttons:

- Page 1: Riding Basics, Pizza Delivery, Speed Trial.
- Page 2: Yard Explorer, Highway Checkpoint Run, Wheelie Practice.

Speed Trial is appended at catalog index 5 (`speed_trial`). Loading earlier five-contract saves retains points, previous tiers and best times while initializing the new trial as uncompleted.

`MissionObjective` now supports optional validated speed-zone bounds. They are copied into the running attempt, preventing later catalog edits from changing an active mission. Existing unrestricted speed-hold objectives retain their previous behavior. Zone geometry shares the mission marker material and is reused across attempts, then released during teardown.

## Verification

- `python3 Tests/CoreCycling/run.py`: **186 checks**, including 89 mission/progression checks. Covers entry ordering, speed dips, grounded/zone requirements, pause, completion, unique rewards, tier scaling, invalid bounds, snapshot isolation and save expansion.
- `python3 Tests/Missions/run.py`: **65 full-scene checks**. Covers mission pagination and previous missions plus speed-trial selection, entry ground, non-colliding boundary visuals, HUD guidance, completion/results, next level and cleanup.
- `python3 Tests/TouchControls/check_layout.py`: touch layout and serialized references.

Integration tests use deterministic injected telemetry. Human balancing, visual inspection and iPhone/iPad performance/readability testing remain necessary.
