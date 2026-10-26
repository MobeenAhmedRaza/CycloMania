# Part 10 — Freestyle mode

Open **Pause → Freestyle**. Browse the five challenge zones, start a challenge, or choose **Explore** in an unlocked zone for untimed practice. The existing stunt HUD shows active tricks and pending combos; the objective HUD shows challenge progress. Challenges share the three-second countdown, pause, retry, results and interruption behavior of Career while maintaining separate progression.

The current implementation provides the mode, zone unlocks and 15 challenges using the existing riding/stunt systems. It does not implement all 20 original document challenges or their missing systems.

## Zones and challenge set

| Zone | Freestyle Points required | Challenges at level 1 |
| --- | ---: | --- |
| Downtown City | 0 | Land a single 100 m wheelie; bank a 3-stunt combo; bank 1,000 stunt points |
| Industrial Yard | 300 | Land 3 flips in one jump; cross the existing raised balance line; bank a 5-stunt combo |
| Mountain Roads | 600 | Land a 15-second wheelie; cross the summit bridge; complete the existing mountain climb route |
| Highway | 900 | Land a 0.5-second stoppie; hold 45 km/h for 15 seconds; bank a 4-stunt combo |
| Desert Trails | 1,200 | Land a jump with 1 second of airtime; land a double backflip; land 5 clean jumps without ending the attempt |

The industrial balance line adapts the document's container bridge to existing geometry. Mountain climb replaces challenges requiring new night/canyon systems within that zone. Highway targets are scaled to the bicycle fleet and available road rather than the document's 150 km/h for five minutes. Freestyle zone locks govern challenge and Explore access in this menu; they do not remove the established World menu or erect barriers in the already accessible open world.

Successful tiers grant **150 + 25 × (level − 1) Freestyle Points**. Two starting-zone completions can unlock Industrial Yard. Challenges repeat through 100 tiers with bounded higher targets, tighter route deadlines or precision. Triple Flip and Double Backflip retain their flip count while reducing the deadline. Completed level 100 can be replayed without another reward. Explore mode is untimed practice and does not award Freestyle Points; start challenges to earn them.

## Validated stunt rules

- Wheelie distance/duration and stoppie duration count a single validated stunt. Multiple short holds do not add together. The HUD reports the best validated attempt.
- Flip challenges require the flip count in one jump. The backflip challenge ignores frontflips. Airtime is taken from one landed jump.
- Dune Jumps counts validated jumps with at least 0.25 seconds of airtime. Crash/reset still ends the attempt.
- Combo chains count the number of segments in a **single banked combo**. Pending combos and separate banked combos cannot merge to satisfy the target.
- Banked Score accumulates points banked during the active challenge. Unfinished stunts do not count.
- Routes require grounded arrival at each three-dimensional checkpoint in order. Speed Hold requires continuous grounded speed.
- The existing stunt system validates supported, upright landings before emitting results. The new combo callback captures the segment count before the scoring system clears the combo.

Starting a challenge clears unfinished stunt state through the normal travel/respawn path. Pause freezes progression and rejects callbacks. Crash, reset, travel, bicycle changes, abandonment or leaving the selected zone ends the challenge. Only one Career or Freestyle attempt can be active. Results offer Retry/Next Level, Challenges and Free Ride. Returning to Career keeps its existing progress intact.

## Saves and implementation

`FreestyleCatalog` contains 15 stable challenge indices, zone bounds, unlock thresholds and difficulty tuning. `MissionSession` supports validated landing and banked-combo objectives. `RideMissions` runs either mode, routes scoring events, enforces zone access/bounds and records to the active progression object. `GameplayFreestyle` supplies selection, locked-zone messaging, Explore and dedicated results.

Freestyle stores a separate versioned `MissionCareer` record with 15 challenge slots. Its PlayerPrefs key is the configured Career save key plus `.Freestyle.v1` (default `Cyclomania.Career.v1.Freestyle.v1`). Career keeps its existing key and indices. Save normalization and duplicate/final-tier reward rules apply independently. Empty save keys disable both modes' storage for automated tests. Best completion times are saved per challenge. Save failures are reported on results.

Freestyle Points unlock challenge zones only in this part. They are separate from Career points, garage stunt unlock points and the coins/gems economy (see `Economy.md`).

## Deferred document features

Traffic overtakes, drift detection/city-block drift, rooftop stunt geometry, collectible stunt tokens, nitro sequences, night/weather/sandstorm challenges and online leaderboards remain unimplemented. No multiplayer or account service is added. Existing Canyon Jump remains available in Career; this part does not move it onto the mountain map.

## Validation

Reverified on 2026-10-20: 349 core checks and 241 full-scene integration checks passed, plus the touch-layout checks. The existing implementation was retained.

- Core suite: catalogue/tier validity, unlock thresholds, clean stunt requirements, single-attempt versus accumulated progress, combo banking via actual scoring callbacks, save recovery and independent progression.
- Full scene: Pause entry, locked-zone service/UI checks, challenge travel/countdown, paused callbacks, dedicated results, rewards/unlocks, harder retry, Explore, route markers, reset, out-of-zone failure and return to Career.
- Existing physics, steering, touch, garage, stunt and all Career mission regressions remain in the suites. Test save keys are disabled.

Run `python3 Tests/CoreCycling/run.py`, `python3 Tests/Missions/run.py` and `python3 Tests/TouchControls/check_layout.py`. Headless tests validate rules and integration, not reliable execution of demanding multi-flip challenges by a human. Launch locations, difficulty, UI appearance and iPhone/iPad performance need playtesting. iOS Build Support remains unavailable in the installed editor.
