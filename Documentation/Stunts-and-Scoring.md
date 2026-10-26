# Part 5 — Stunts and scoring

The active gameplay bicycle now has `BicycleStunts`. It samples physical wheel contact, orientation, rotation changes and velocity after the bicycle's physics update. Button presses alone do not earn points.

## Controls

| Trick | Touch | Keyboard |
| --- | --- | --- |
| Wheelie | Hold Wheelie + Pedal while riding | Left Ctrl + W / Up |
| Stoppie | Hold Wheelie + Brake while riding | Left Ctrl + S / Down |
| Jump | Hold Jump to charge, then release on the ground | Hold/release Space |
| Backflip | While airborne, hold Wheelie + Pedal | Left Ctrl + W / Up in air |
| Frontflip | While airborne, hold Wheelie + Brake | Left Ctrl + S / Down in air |

Release the airborne trick inputs before landing. Flips require enough height and airtime; ordinary terrain is not guaranteed to support every trick. The pause menu includes a brief control guide.

Grounded pitch assistance targets a 35-degree wheelie or 28-degree stoppie. Ground balance returns at very low speeds. Air pitch control targets 7.5 radians/second. These values are exposed on BicycleController for handling tuning.

A fully charged jump takes 0.5 seconds. Its upward velocity change is applied once, in FixedUpdate, to the frame and both wheels. Input cancellation, pause and resets discard queued jumps. Jump charging does not create midair propulsion.

## Detection and landing

- Wheelie: rear wheel supported, front lifted, nose at least 12 degrees above the surface and horizontal speed at least 1 m/s.
- Stoppie: front wheel supported, rear lifted, nose at least 10 degrees below the surface and speed at least 0.5 m/s.
- One-wheel holds must last at least 0.5 seconds to score. The HUD shows ongoing duration and distance.
- A jump must follow ground contact and have at least 0.25 seconds airborne. Initial spawn falls do not score.
- Front/back flips count complete 360-degree signed pitch rotations while airborne. Opposing partial rotations cancel instead of adding false flips; multiple full rotations can count in one flight.
- A clean landing requires both wheels supported and upright alignment of at least 0.8 with the ground normal, held for 0.3 seconds. Brief bouncing does not immediately validate a landing.
- Inverted contact forfeits the combo. An upside-down frame landing also triggers crash recovery, even below the normal impact-speed threshold. A one-wheel touchdown that fails to settle within two seconds loses an airborne combo.

## Score and combos

- Wheelie/stoppie: round(duration × 100 + distance × 10).
- Jump: 100 + round(airtime × 100 + distance × 5) + 500 per complete flip.
- Valid stunt segments chain into a combo. The multiplier increases per segment, capped at ×5; the pending total is the sum of base points multiplied by that multiplier.
- Points bank after 2.5 seconds of stable riding without another trick, following landing validation. The HUD distinguishes banked SCORE from pending COMBO.
- Crashing or manual respawn loses unfinished/unbanked tricks and preserves already banked points.
- Pause freezes stunt progress and combo timing. Restart Ride clears all session stunt scores and the best combo.

The HUD shows current trick, hold duration/distance or airtime, pending combo/multiplier and banked score. The pause screen shows total score and best combo. Part 12 also awards one coin per ten banked points, with remainder carried forward; see `Economy.md`.

## Integration and checks

`StuntScoring.StuntLanded` supplies validated kind, duration, distance, front/back flip count and base points. `ComboBanked` supplies the banked total. Part 10 consumes validated `StuntLanded` results and the new `ComboFinished(points, segments)` event for Freestyle challenges. Combo counts are captured before resetting banked state. Ride score remains session-only; Freestyle challenge progress is saved separately.

Run `python3 Tests/CoreCycling/run.py` for deterministic scoring tests, Unity physics checks, crash/respawn integration and existing HUD/touch regressions. The final suite passed 74 checks (29 stunt checks plus 45 existing physics, touch and HUD checks). Runtime errors fail validation. `python3 Tests/TouchControls/check_layout.py` checks serialized scene/control layout.

The automated fixture is headless and does not reproduce the full city or imported rider rig. Ramp clearances, rider animation, landing feel and final control tuning still need in-editor and iPhone/iPad playtesting. No new world zones or stunt courses are added in this part.
