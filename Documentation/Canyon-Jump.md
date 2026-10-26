# Part 9.9 — Canyon Jump

Open **Pause → Missions → page 4 → Canyon Jump → Start**. The rider starts on the canyon run-up at (1000, 0, 245). After the three-second countdown, hold **PEDAL + SPRINT**, charge **JUMP**, and release it in the gold takeoff zone. Keep straight across the gap. Land upright in the green target and brake to settle both wheels.

## Course

The permanent course connects to the desert return road at z = 220:

- Flat approach to z = 250.
- A 12 m wide ramp rises from ground level to 3 m over z = 250…270.
- Takeoff deck at y = 3, z = 270…280.
- Six-metre gap at z = 280…286, with a canyon floor at y = −6.
- Landing deck at y = 3, z = 286…326.
- Exit ramp descends to ground at z = 346.

The gap has no bridge or ground-level riding surface. The original five world destinations remain unchanged; starting this mission sets a safe recovery point on the canyon run-up. The course remains available in free ride and the area label identifies it as Canyon Course.

## Jump validation

The rider must be grounded within 3 m of the takeoff target (1000, 3, 279) to arm the attempt. Leaving the zone while still grounded disarms it. Losing ground contact after arming starts the flight and moves the marker to the green landing target at (1000, 3, 289).

- At least 0.25 seconds of airtime is required before touchdown.
- The first supported touchdown must be inside the landing target's three-dimensional radius. Returning to the takeoff bank or landing outside the zone fails.
- Completion requires both wheels supported, upright alignment of at least 0.8 relative to the ground normal, and continuous stable time inside the zone.
- Losing stability resets the settle timer. Failure to settle within two seconds after first touchdown fails the attempt.
- Falling below y = 1 during the jump, or leaving the course around the landing, fails. Ground-level riding on the initial run-up is allowed.
- Walking onto the landing deck, arriving airborne without arming at takeoff, or briefly losing wheel contact cannot complete the mission.

The mission uses its own position-aware landing validation. Stunt scoring remains independent and follows its existing banking/landing rules.

## Repeated levels and lifecycle

| Setting | Level 1 | Repeat progression |
| --- | --- | --- |
| Time allowance | 90 seconds | −2 seconds per level, minimum 45 seconds |
| Landing radius | 3 m | −0.05 m per level, minimum 2 m |
| Continuous stable landing | 0.3 seconds | +0.02 seconds per level, maximum 0.8 seconds |
| Reward | 225 career points | +25 per level, once per newly completed tier |

The physical gap stays six metres across all tiers. Pause freezes flight, landing and deadline timers. Crash, reset, world travel, bicycle changes and abandonment follow the shared interruption rules. Retry restores the run-up and clears takeoff/flight state at the same uncompleted tier. Next Level loads harder landing requirements. Results hide the target marker.

`canyon_jump` is appended at catalog index 11; older saves retain all previous eleven mission records. Page 4 now contains Police Escape, Mountain Ascent and Canyon Jump. The 100-tier career and final-tier reward cap apply.

## Verification and limits

Core tests cover grounded arming/disarming, airborne eligibility, pause, clean landing, landing instability, wrong-bank/outside-zone landings, falling, short hops, deadline, retry, repeated rewards, snapshot validation and save migration. Scene tests verify menu access, run-up spawn, deck support, an unobstructed physical gap, marker transitions, rewards, harder retries and cleanup. World regression checks cover existing destination travel and geometry.

Run `python3 Tests/CoreCycling/run.py`, `python3 Tests/Missions/run.py`, `python3 Tests/World/run.py`, and `python3 Tests/TouchControls/check_layout.py`. Mission completion tests inject physical-state samples; they do not establish reliable human jumps with every bicycle or validate the higher-tier braking window. Course art, launch timing, landing difficulty and iPhone/iPad performance need hands-on playtesting. iOS Build Support is absent from the available editor, so device export/testing remains pending.
