# Part 9.6 — Checkpoint Run

Open **Pause → Missions → page 3 → Checkpoint Run → Start**. After the three-second countdown, ride through each checkpoint in order before the deadline. The active checkpoint has a gold ground marker and a raised gate; the finish turns green. The HUD shows distance, steering direction, checkpoints passed and remaining time. Only the next required gate is shown, with its ground marker also visible when within the minimap camera's range.

## Course and difficulty

The player starts on the highway at (850, 0, −240). The first gate is at z = −160; intermediate gates alternate between x = 847 and x = 853, and the finish is centered at x = 850. This introduces gentle steering changes while keeping the complete course within the existing road.

| Setting | Level 1 | Repeat progression |
| --- | --- | --- |
| Ordered checkpoints | 5 | One extra every two levels, capped at 10 |
| Finish position | z = 160, about 400 m from spawn | +20 m per level, capped at z = 360 |
| Checkpoint radius | 6 m | −0.15 m per level, minimum 3 m |
| Time allowance | 120 seconds | −2 seconds per level, minimum 95 seconds |
| Career reward | 175 points | +25 per level, once per newly completed tier |

Level 2 retains five checkpoints but extends the course, reduces the radius to 5.85 m and reduces the deadline to 118 seconds. Level 3 adds the sixth checkpoint. Difficulty caps keep later tiers on the available highway. The existing career supports 100 tiers; replaying a completed final tier awards no additional points.

## Rules and integration

- Checkpoints use the existing grounded, three-dimensional radius check. The bicycle must be supported by the ground and inside the current target's radius; jumping over a gate does not count.
- Future checkpoints do not count out of order. Returning to a completed checkpoint does not advance progress. If a target is missed, the HUD guides the player back to it.
- Gate posts and crossbar are visual guides without active colliders. The ground marker indicates the target radius; the actual detection is spherical around the road point.
- Pause freezes collection and the deadline. Crash, reset, travel, bicycle changes and abandonment follow the shared interruption rules.
- Retry resets to the first checkpoint of the same uncompleted level. Next Level starts the harder course. Results hide both the gate and ground marker; owned visuals are destroyed with the mission component.
- `checkpoint_run` is appended at catalog index 8. Existing career arrays expand automatically, preserving all earlier mission completion and points. The original three-checkpoint Highway Checkpoint Run practice contract remains on page 2.

## Verification

Core checks cover ordering, duplicate visits, airborne/outside-radius rejection, pause, countdown, expiry, rewards, retry, save migration and all 100 tiers' bounds and checkpoint ordering. Full-scene checks cover menu selection, ground and obstacle clearance at level 1 and maximum difficulty, non-colliding gate visuals, marker/HUD advancement, green finish, results, harder levels and reset/abandon cleanup.

Run `python3 Tests/CoreCycling/run.py`, `python3 Tests/Missions/run.py` and `python3 Tests/TouchControls/check_layout.py`. Headless checks do not replace visual review or riding the course on iPhone/iPad. iOS Build Support is absent from the available editor, so device export and testing remain pending.
