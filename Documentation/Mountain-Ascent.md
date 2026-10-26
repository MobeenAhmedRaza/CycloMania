# Part 9.8 — Mountain Ascent

Open **Pause → Missions → page 4 → Mountain Ascent → Start**. The player travels to the mountain base and waits through the shared three-second countdown. Climb through seven ordered checkpoints, following the gold gate and direction/distance guidance, then reach the green finish on the 24 m summit bridge. The HUD also shows current altitude and checkpoints passed.

## Route

The course uses the existing mountain roads, without adding or changing terrain:

1. Lower ascent midpoint: (730, 5, 120).
2. First corner: (730, 10, 160).
3. Switchback midpoint: (755, 12, 170).
4. Upper ascent entrance: (780, 14, 180).
5. Upper ascent midpoint: (755, 19, 215).
6. Summit entrance: (730, 24, 250).
7. Summit bridge finish: (775, 24, 250) at level 1.

Gate and ground-marker orientation follows the incoming road segment. Gates are visual guides with no active colliders. The checkpoint uses a three-dimensional radius around its road position and requires grounded riding. Passing underneath an elevated marker or jumping through it cannot count. The summit cannot complete the mission before the preceding climb checkpoints. Missed gates remain active so the rider can return to them.

## Repeat difficulty

| Setting | Level 1 | Subsequent levels |
| --- | --- | --- |
| Time allowance | 150 seconds | −3 seconds per level, minimum 75 seconds |
| Checkpoint radius | 4.5 m | −0.1 m per level, minimum 2.5 m |
| Summit finish | x = 775 | +1 m per level, capped at x = 785 on the bridge |
| Career reward | 200 points | +25 per level, once per newly completed tier |

Level 2 allows 147 seconds, uses a 4.4 m radius, and finishes at x = 776. All tiers keep the seven route checkpoints and 24 m summit elevation. The existing 100-tier career and final-tier replay reward limit apply.

Pause freezes checkpoint collection and the deadline. Crash, reset, bicycle change, world travel and abandonment end the attempt through the shared mission lifecycle. Retry returns to the base and first checkpoint at the same uncompleted tier. Next Level starts the harder tier. Results hide the gate and target marker; switching to another mission restores its appropriate marker orientation and appearance.

The catalog appends `mountain_ascent` at index 10. Existing saves expand without changing prior mission indices or losing completion/points. Mission page 4 contains Police Escape and Mountain Ascent.

## Validation

Core checks cover ordered progression, countdown/pause, rejection of direct summit visits and below-road/airborne crossings, deadlines, rewards, repeats, maximum tuning and old-save migration. Full-scene checks verify road support at checkpoint elevations, rider clearance for first and maximum tiers, marker orientation/advancement, HUD, results, harder retries, reset cleanup, and switching back to flat highway checkpoints.

Run `python3 Tests/CoreCycling/run.py`, `python3 Tests/Missions/run.py` and `python3 Tests/TouchControls/check_layout.py`. Scene tests inject rider samples for deterministic mission progression; they do not prove a human can negotiate every switchback within the deadline. Riding feel, visuals and difficulty need in-editor and iPhone/iPad playtesting. The available Unity editor lacks iOS Build Support, so device export/testing remains pending.
