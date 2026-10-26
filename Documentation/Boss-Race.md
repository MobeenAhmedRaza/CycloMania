# Part 9.10 — Boss Race

Open **Pause → Missions → page 5 → Boss Race → Start**. Hold **PEDAL + SPRINT** and beat the gold boss cyclist to the finish. This is a single-player race on the existing highway, with a shared three-second countdown, marked course, rank/distance HUD and named boss victory or defeat results.

## Difficulty

| Setting | Level 1 | Repeat progression |
| --- | --- | --- |
| Course distance | 500 m | +10 m per level, capped at 640 m |
| Boss speed | 24 km/h | +0.2 km/h per level, capped at 30 km/h |
| Boss acceleration | 3 m/s² | Unchanged |
| Deadline | 110 seconds | Unchanged |
| Career reward | 300 points | +50 per level, once per newly completed tier |

The start is (850, 0, −240), with the boss in the parallel lane at x = 854. The finish moves from z = 260 to at most z = 400, inside the existing highway. Boss Race starts longer and faster than Rival Sprint and uses stronger acceleration (3 rather than 2 m/s²). All stock bicycle sprint speeds exceed the maximum boss cruising speed; this speed comparison does not establish human win rates or acceleration balance.

## Race rules

The mission reuses the race engine's physical position progress, course boundaries and analytical opponent motion, with validated, snapshotted acceleration per objective. There is no rubber banding or collision pushing. Going outside the 20 m wide course, more than 10 m behind the start, or more than 5 m above/below road level ends the attempt.

Player finish time is interpolated between physics samples. Boss finish time accounts for acceleration from rest. If both finish within a tick, the earlier crossing wins; exact ties within 0.1 ms favor the player. Pause freezes both riders' race clock and visual motion.

Winning records the career tier once and offers Next Level; losing offers Retry without rewards. Crash, reset, travel, bicycle changes and abandonment use the shared interruption rules. Retry restores both riders to the grid with a fresh countdown. Results hide the opponent and course. Switching to Rival Sprint restores its orange rider and race appearance; police beacons are absent during boss races.

The boss uses the existing procedural cyclist model with gold coloring. It is provisional art, with no separate player controller, physics body or camera. The mission is available directly, like the other current career missions; no purchase or prerequisite unlock has been added.

`boss_race` is appended at catalog index 12. Existing saves preserve the preceding twelve contracts, including the three original practice contracts. Selection expands to five pages. The existing 100-tier career and completed-final-tier replay reward limit apply. This adds the tenth design-document mission in Part 9.

## Validation

Core checks cover boss acceleration, countdown, pause, wins/losses, finish ties, course boundaries, retry, reward uniqueness, progression caps, stock speed comparison, old-save migration and definition snapshots. Full-scene checks cover page 5, gold opponent and course markers, maximum finish ground support, HUD, pause, rewards/results, harder levels, reset cleanup and restoring ordinary rival visuals.

Run `python3 Tests/CoreCycling/run.py`, `python3 Tests/Missions/run.py` and `python3 Tests/TouchControls/check_layout.py`. Headless checks do not replace visual review, race balancing or iPhone/iPad playtesting. The available editor lacks iOS Build Support, so device export/testing remains pending.
