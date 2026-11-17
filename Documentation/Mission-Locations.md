# Mission locations and handbrake

## Handbrake

The additional round **(!)** button beside Sprint is a hold-to-brake control. It applies 1.5 times the equipped bicycle's normal braking (minimum 14 m/s²), overrides pedaling, and never engages reverse. Hold **H** in the Editor/keyboard build. Space remains Jump. Existing brake/down still stops and then reverses as before.

Touch pointers remain independent, so steering and handbrake can be held together. Pause, respawn, bicycle changes, focus loss and touch cancellation release the input. The handbrake counts as braking in tutorial objectives. The button belongs to the existing safe-area panel and is passed to replacement garage bicycles along with the other controls.

## Different routes by mission and level

All 13 Career entries (the ten named missions plus three practice contracts) now have distinct starts across their 100 levels. Replaying/retrying the same mission level selects the same route. Advancing the level selects a different start and route; saved completion tiers continue to determine the next level without save migration.

Ordinary missions use a baked street graph sampled from the current map's road colliders. The bake checks flat ground, rider clearance, neighbor connections and continuous ground between nodes. The current data contains 12,730 road nodes and 11,940 eligible starts. It includes separate street districts, not just one highway. A deterministic permutation selects a unique street start for each mission/level pair. Connected routes are at least 650 m long, supporting the existing maximum race distances and difficulty values. Route guides follow the actual streets, including turns.

- Delivery pickup and drop-off points follow route distance.
- Checkpoints follow the route and their gates face its direction.
- Rival/Boss races measure progress and finish position along the route; AI riders follow it.
- Speed-hold zones follow the route instead of a fixed rectangular highway.
- Police follow the same route, with route-based pursuit boundaries.
- Tutorial, wheelie and distance contracts begin at distinct street locations.

As requested, **Mountain Ascent and Canyon Jump retain their authored terrain**. Mountain levels vary the starting approach, lateral line and checkpoint positions within the existing climb/bridge. Canyon levels vary the run-up, takeoff and landing line on the existing ramps/decks. Each level has a distinct start, while avoiding unsupported floating checkpoints. Canyon bounds are now relative to the placed jump instead of the original map's absolute coordinates.

Freestyle challenges retain their zone-specific terrain and zone-unlock rules; this change targets Career missions and practice contracts.

## Map data and authoring

`Assets/Cyclomania/Resources/MissionLocations.json` contains the checked road graph and eligible start indices. Regenerate it after changing road geometry via **Cyclomania → Bake mission street locations**. The menu prompts to save modified scenes before opening gameplay. Runtime route construction uses this data rather than scanning the entire city when a mission starts. Missing/inadequate data produces an explicit mission-menu error instead of silently starting at an unsafe fallback location.

The baked graph validates static geometry, not future moving traffic occupancy. City traffic can still cross routes. The map has no navmesh requirement. Starts and paths remain inside the authored playable bounds.

## Validation

`python3 Tests/CoreCycling/run.py` covers handbrake priority/reset, stopping and reverse exclusion; route interpolation, progress, boundaries, immutable mission snapshots, malformed-route rejection and translated canyon checks, plus existing gameplay logic.

`python3 Tests/TouchControls/check_layout.py` covers nine control bounds, non-overlap and eight direct input bindings.

`python3 Tests/Missions/run_locations.py` checks every Career mission at every level against actual scene geometry, unique starts, bounds, terrain support and retry determinism, then tests actual touch handbrake/pause behavior and mission start/retry/next-level placement in Play mode. Headless scene tests disable the GPU traffic manager; traffic interactions and touch presentation still require device playtesting. Older scene tests with fixed world-coordinate expectations are superseded for placement by this current-map suite.
