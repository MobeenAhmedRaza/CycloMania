# Part 7 — Open world

The gameplay scene now enables `RideWorld` through `GameplayHud.enableWorld`. The imported Downtown city remains the starting area. Four connected riding areas extend its eastern side using lightweight, procedural geometry. Enter Play mode to see the generated areas; source assets and imported city meshes are not modified.

| Area | Arrival position | Riding features |
| --- | --- | --- |
| City Centre | 76.64, 0, -172.52 | Existing streets, parks, buildings, river and port |
| Industrial Yard | 620, 0, -170 | Beginner ramp, takeoff/landing jump, raised balance line, flat practice loop |
| Mountain Roads | 730, 0, 80 | Graded climbs, switchbacks, 24 m summit bridge, descent, rock formations |
| Highway | 850, 0, -240 | 760 m straight with lane markings for bicycle sprint practice |
| Desert Trails | 1000, 0, 0 | Sand-colored riding surface, three rolling dunes, return route |

The east connector starts beyond the city buildings at (380, 0, -300). It joins the yard and highway. The mountain route and desert trail connect to the highway at both ends. No scene load is needed. Elevated roads have edge barriers with openings at junctions. Ramp meshes supply sloped collision surfaces; decorative paint and destination markers have no collision.

Part 9.9 adds a permanent canyon jump course north of the desert trails, connected at z = 220: a run-up and ramp, 3 m high decks separated by a 6 m gap, a lower canyon floor and an exit ramp. See `Canyon-Jump.md` for course coordinates and mission rules. The original five destination indices remain stable.

## Navigation and recovery

- **Pause → World → Ride Here** travels directly to a destination and resumes riding.
- A location label sits below the minimap. Physical cyan markers identify arrivals in the world and on the overhead minimap.
- Travel resets velocity and unfinished stunts while preserving trip distance and banked score.
- Traveling, or riding upright on the ground within 8 m of a destination, sets that location as the session's safe recovery point.
- Crash recovery, Reset, and Restart Ride use the current safe point. Restart still clears the ride's distance and session score.
- Garage bicycle changes now use the active recovery point rather than sending the rider back downtown.
- Falling below the recovery floor or leaving the playable envelope returns the complete bicycle assembly to its safe point.

`CycleRecovery.SetCheckpoint(position, heading)` changes the recovery pose without disturbing the stored wheel/frame offsets. It rejects nonfinite values. `RideWorld.Travel(index)` validates destinations and requires an initialized recovery component. The five destination indices are also available for later mission integration.

Safe points are session state. Relaunching begins downtown with the saved bicycle; garage upgrades and progression remain persistent.

## Scope and validation

These extensions provide playable course geometry and collision, not finished environment art. The imported city vehicles remain scenery; moving traffic, opponents, mission checkpoints/objectives, weather, and freestyle progression are not part of this implementation. Speed and ramp dimensions are intended for bicycles rather than the document's motorcycle speeds.

- `python3 Tests/World/run.py` opens a disposable copy of the full city and checks all five travel buttons, arrival ground/clearance, score/distance preservation, checkpoint assignment, ramp and bridge collision, invalid inputs and out-of-world recovery. The test never reads/writes the player's garage save.
- `python3 Tests/CoreCycling/run.py` covers existing cycling, touch, HUD, stunts and garage behavior.
- `python3 Tests/TouchControls/check_layout.py` checks touch layout and serialized references.

The generated geometry shares four materials and uses static colliders. Runtime meshes/materials are released when the world owner is destroyed. Device performance, visual polish and riding feel require editor/device playtesting; iOS Build Support is still needed for export.
