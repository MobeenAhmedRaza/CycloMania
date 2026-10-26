# Part 8 — Mission system

Open **Pause → Missions**, choose a contract, and press **Start**. The rider travels to the contract's starting destination. A three-second countdown holds the entire bicycle still; controls and physics resume when the attempt starts. The existing objective HUD shows the instruction, progress, current step and remaining time.

## Playable integration contracts

These three contracts exercise the framework. Part 9.1 adds Riding Basics, Part 9.2 adds Pizza Delivery, and Part 9.3 adds Speed Trial on the first menu page (see `Riding-Tutorial.md`, `Pizza-Delivery.md`, and `Speed-Trial.md`). The original three practice contracts are on page 2. Parts 9.4–9.6 add Rival Sprint, First Wheelie and Checkpoint Run on page 3 (see `Rival-Sprint.md`, `First-Wheelie.md` and `Checkpoint-Run.md`); Parts 9.7–9.9 add Police Escape, Mountain Ascent and Canyon Jump on page 4 (see `Police-Escape.md`, `Mountain-Ascent.md` and `Canyon-Jump.md`). Part 9.10 adds Boss Race on page 5 (see `Boss-Race.md`), completing the ten Part 9 mission implementations.

| Contract | Level 1 | Subsequent levels |
| --- | --- | --- |
| Yard Explorer | Ride 200 m within 90 seconds | +100 m target and +10 seconds allowance per level |
| Highway Checkpoint Run | Pass three checkpoints in order within 120 seconds | Time allowance drops by 3 seconds per level, down to 65 seconds |
| Wheelie Practice | One continuous 10-second wheelie | +25 seconds per level: 10, 35, 60…; target duration plus 90 seconds allowance |

All contracts are available without purchases. Completing a level unlocks the next level for that contract. The current framework supports 100 levels per contract; the final level remains replayable without additional rewards. Speed and distance requirements use bicycle-scale units.

## Lifecycle

`MissionSession` implements Idle → Countdown → Running → Completed / Failed / Abandoned. A running definition is copied at start so catalog edits do not change the attempt. Starting a second active attempt is rejected.

- Pause and background pause freeze both countdown and mission time.
- Crash, Reset, Restart Ride, world travel and bicycle changes end an active attempt.
- **Retry** returns to the mission start and uses the same uncompleted level.
- **Next Level** starts the newly unlocked difficulty after completion.
- **Abandon** ends an active attempt without progression or reward.
- **Free Ride** dismisses results, clears the mission objective and resumes riding.
- Results automatically pause the game. Completion only occurs once per attempt.

Stunt score, trip distance and garage progress are not reset when starting a contract. Active attempts are intentionally not restored after app termination; completed career progress is saved.

## Objectives and scene integration

The independent session engine supports ordered sequences of:

- Reach a 3D point within a radius while grounded.
- Accumulate riding distance from velocity, excluding teleport displacement.
- Hold a minimum ground speed continuously.
- Hold a wheelie continuously; touching down or losing the wheelie resets that hold.
- Race a deterministic AI cyclist to a finish line, with interpolated finish ordering.
- Escape an AI police cyclist by maintaining a gap while avoiding sustained capture proximity.
- Earn a target amount of banked stunt score. Pending or crashed combos do not count.

`RideMissions` samples the equipped bicycle and connects crash, respawn and banked-score events. It rebinds after garage swaps. It releases countdown physics locks on every terminal path and on teardown. A gold, non-colliding marker shows the next route checkpoint in the world and on the minimap; future checkpoints do not count out of order.

`GameplayMissions` supplies selection, active-attempt and results pages using the existing safe-area layout. `MissionCatalog.Create(index, level)` is the extension point for Part 9 definitions. Keep existing catalog indices stable when appending contracts, or migrate the career save when replacing them.

## Progress and rewards

`MissionCareer` stores completed tiers, best completion time per contract and career points as versioned JSON in PlayerPrefs under `Cyclomania.Career.v1`. Each newly completed tier grants its catalog reward exactly once: a base of 100 for practice/tutorial, 150 for delivery/speed trial/First Wheelie, or 175 for Rival Sprint/Checkpoint Run, 200 for Police Escape/Mountain Ascent, or 225 for Canyon Jump, plus 25 × (level − 1). Boss Race grants 300 + 50 × (level − 1). Failed, abandoned and repeated final-tier attempts grant no points. These points are separate from garage lifetime stunt points and from the coins/gems economy implemented in Part 12 (see `Economy.md`).

Missing/corrupt saves, invalid arrays, unsupported versions and out-of-range values recover to valid/default data. Saves occur on completion; save errors appear on the result page. This is local device storage, not cloud synchronization or tamper-proof progression.

## Validation

- `python3 Tests/CoreCycling/run.py`: 369 checks, including 20 settings checks, 26 freestyle checks, 170 mission-engine/progression checks, 22 police pursuit checks, 21 canyon jump checks, 13 handling checks and 97 existing regressions.
- `python3 Tests/Missions/run.py`: 256 integration checks in a disposable copy of the full gameplay scene. Covers menu starts, countdown physics, pause, crash/retry, completion/results, harder levels, world/reset/garage interruption, target markers and replacement-bicycle event binding.
- `python3 Tests/World/run.py`: existing world integration checks.
- `python3 Tests/TouchControls/check_layout.py`: touch geometry and serialized wiring.

Tests disable player-save access. Headless checks do not replace visual review, difficulty balancing or iPhone/iPad testing.

## Part 10 integration

`RideMissions` also runs Freestyle challenges with separate point/save records and zone unlocks. Career catalogue indices and points remain unchanged. See `Freestyle-Mode.md` for the 15 challenges, scope adaptations and deferred systems.
