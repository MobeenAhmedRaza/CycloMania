# Part 9.4 — Rival Sprint

Open **Pause → Missions → page 3 → Rival Sprint → Start**. Race a single AI cyclist along the highway. Hold **PEDAL + SPRINT**, stay between the orange course boundaries, and cross the finish first. This is a local single-player mission; no multiplayer is involved.

Both riders start at z = −240 after the same three-second countdown. The player starts at x = 850; the rival uses a parallel lane at x = 854. The HUD reports rank, player distance, rival distance and time remaining. The course has a start line, side boundaries and a finish line with posts. The rival uses a lightweight procedural bicycle and rider with rolling wheels and animated pedaling; it does not run player controllers, physics, cameras or globally bound rider scripts.

| Setting | Level 1 | Repeat levels |
| --- | --- | --- |
| Course distance | 300 m | +15 m per level, capped at 600 m |
| Rival cruising speed | 18 km/h | +0.4 km/h per level, capped at 28.8 km/h |
| Rival acceleration | 2 m/s² from rest | Unchanged |
| Time limit | 140 seconds | Unchanged |
| Career reward | 175 points | +25 per level, once per newly completed tier |

The rival follows a deterministic straight route without rubber banding or collisions. Its maximum speed is below every stock bicycle's sprint speed. Difficulty caps preserve road space and bicycle feasibility; later tiers remain replayable through level 100, with no additional rewards for replaying an already completed final tier.

## Race rules

- Race progress comes from forward world position, not accumulated riding distance. Riding backwards reduces progress.
- Leaving the 20 m wide course, going more than 10 m behind the start or more than 5 m above/below road level ends the attempt. Small jumps are allowed.
- The rival accelerates from rest and then cruises. Finish timing is independent of frame rate; player crossing time is interpolated between physics samples, and the rival crossing time is calculated analytically. If both finish within one tick, the earlier crossing wins. Exact ties within 0.1 ms favor the player.
- A win shows **1st place**, records career progress and offers **Next Level**. A loss shows **2nd place** and offers **Retry** without rewards.
- Pause freezes the clock, rival movement and pedaling pose. Crash, reset, world travel, garage changes and abandonment use the existing mission interruption rules.
- Retry resets both racers and the countdown. Terminal results hide the rival and course; owned objects and materials are released when the mission component is destroyed.
- Rival Sprint is appended at catalog index 6. Earlier mission indices and saved progression remain intact. Selection supports three pages; the first two retain their previous order.

## Validation

`Tests/CoreCycling/run.py` covers rival acceleration, countdown, pause, win/loss order, ties, off-course failure, retry, tier scaling, stock bicycle speed limits and save migration, alongside previous regressions. `Tests/Missions/run.py` covers menu selection, rival spawn/motion, ground along the lane, rank HUD, pause, results/rewards, harder retries, reset cleanup and visual isolation from player scripts. Touch layout checks remain unchanged.

Headless validation does not establish ride feel or final visual quality. The procedural rival is placeholder art; device performance, visual review and difficulty balancing still require iPhone/iPad playtesting. iOS Build Support is not installed in the available Unity editor, so this change has not been exported to a device.
