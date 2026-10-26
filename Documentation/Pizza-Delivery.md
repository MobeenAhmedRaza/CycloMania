# Part 9.2 — Pizza delivery

Open **Pause → Missions → Pizza Delivery → Start**. This implements the design document's second mission: a timed package delivery, adapted to bicycles. It uses a clear, verified route along the existing highway rather than requiring traffic or restaurant assets.

## Level 1

- Start at the highway destination, with the existing three-second countdown.
- Ride 20 m forward to the **blue pickup zone** at (850, 0, -220).
- Stay grounded, within 6 m of its center, at no more than 2 km/h for **2 continuous seconds** to collect the pizza.
- Deliver to the **green zone**, 140 m farther along the road at (850, 0, -80).
- Meet the same low-speed hold requirement to hand over the pizza before the **180-second** timer expires.

Leaving the zone, speeding up or becoming airborne resets the handover hold. Visiting the destination before collection does not complete anything. No additional interaction button is needed.

The HUD supplies distance and relative direction to the current stop, the handover hold timer and the package status. The active marker changes from blue pickup to green destination and is visible on the overhead minimap when nearby. Collection attaches a small, non-colliding pizza-box visual to the bicycle; completion or interruption hides it.

## Repeated difficulty

For each subsequent tier:

- Add 20 m to the delivery leg, up to 500 m.
- Reduce the time limit by 3 seconds, down to 100 seconds.
- Reduce the stop radius by 0.15 m, down to 3 m.
- Increase each handover hold by 0.1 seconds, up to 4 seconds.

Level 2 therefore uses a 160 m delivery leg, 177-second deadline, 5.85 m stop radius and 2.1-second handovers. The longest route remains on the highway. Higher tiers encourage efficient riding and sprint use. The existing framework supports 100 tiers, then unawarded final-tier replays.

A newly completed tier grants **150 + 25 × (level − 1) career points**, once per tier. These are the existing career points, not coins or garage stunt points.

## Failure and persistence

Crash, reset, travel, changing bicycles, timeout or abandoning the attempt removes the carried package. Retry begins at the starting destination and requires collecting a fresh pizza. Pausing freezes the mission clock and handover progress. Completion uses the existing results, Next Level and Free Ride flow.

Catalog index 4 (`pizza_delivery`) is appended without moving the earlier four contracts. Old saves retain their points, tutorial progress, contract tiers and best times, and initialize delivery as uncompleted. Active cargo/attempts are session-only and are not restored after quitting.

`PickupPackage` and `DeliverPackage` are ordered objective types in `MissionSession`. Definitions reject dropoff without pickup, duplicate pickup while carrying, and pickup sequences without a handover. The attempt owns package state independently of its visual; UI or marker refreshes cannot grant a reward or advance an objective. `RideMissions` owns the marker colors, guidance and reusable package visual/material cleanup.

## Validation

- `python3 Tests/CoreCycling/run.py`: 164 checks, including 67 mission/progression checks. Delivery coverage includes continuous stop rules, wrong locations/order, pause, cargo lifecycle, timeout, retry, rewards, difficulty limits and save expansion.
- `python3 Tests/Missions/run.py`: 50 checks in a disposable full gameplay scene, including delivery menu layout, road clearances, blue/green markers, cargo attachment and collision exclusion, HUD status, results, higher-tier replay and reset cleanup.
- `python3 Tests/TouchControls/check_layout.py`: touch geometry and serialized wiring.

Integration tests inject telemetry deterministically. The pizza box and route markers are basic geometry; visual polish, human difficulty balancing and iPhone/iPad playtesting remain.
