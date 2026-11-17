# Steering and speed update

Steering now changes the bicycle's grounded heading directly, instead of relying on front-wheel rotation to overcome the balance controller. A speed-dependent turn-rate limit keeps low-speed turns tight and reduces abrupt rotation at high speed. Ground grip aligns the frame and both wheels' velocities toward the new heading while preserving their speed and ground-normal velocity. Steering response rises from 5 to 12 input units per second, reducing time to full steering from about 0.2 seconds to 0.083 seconds.

The assist operates only during supported, balanced riding. Airborne movement and wheelie/stoppie pitch control retain their existing physics. Stationary input cannot pivot the bike in place. Steering release removes assisted turning and cancels residual frame yaw. No extra forward force is applied by the assist.

The requested **200% increase** is applied as **three times the original cruise and sprint caps**. Braking deceleration is also tripled, preserving approximately the old stopping time at the new top speed (stopping distance increases with speed). Pedal force, wheel torque, acceleration curves, rolling resistance and existing upgrade multipliers are unchanged.

| Bicycle | Cruise | Sprint | Base braking |
| --- | ---: | ---: | ---: |
| City | 67.5 km/h | 135 km/h | 21 m/s² |
| BMX | 59.4 km/h | 118.8 km/h | 22.5 m/s² |
| Mountain | 64.8 km/h | 129.6 km/h | 27 m/s² |
| Trials | 48.6 km/h | 97.2 km/h | 30 m/s² |
| Fatbike | 54 km/h | 108 km/h | 24 m/s² |

Garage stats and controller settings use the same scaling functions. Existing saves and purchased/unlocked upgrade tiers are retained, and applying a setup does not compound the multiplier. These are speed caps, not a promise that every slope or launch situation reaches that speed with unchanged drive force.

Validation includes simulated left/right turns, speed preservation during steering, high-speed braking, the existing touch/stunt/garage/mission regressions, and turn checks with the imported City and BMX bicycles in the full scene. Headless validation still needs hands-on confirmation of control feel on iPhone/iPad.

## October 27 — new-map traffic and reverse

The gameplay scene’s Megacity TrafficSpawner probability is reduced from 0.45 to 0.1125, giving 25% of the previous expected traffic density with the existing 10 m spawn spacing. Spawning is randomized, so exact car counts vary between runs.

Holding brake/down first stops forward motion, then engages reverse after 0.35 seconds near rest. Reverse drive is 2.5 m/s², capped at 3 m/s, and requires rear-wheel ground contact. Pedaling brakes backward movement before moving forward. Wheelie + brake does not engage powered reverse. Clearing controls resets the reverse hold timer. Forward speed, acceleration and existing brake strength are unchanged.

Core physics checks cover brief brake taps, sustained reverse and its speed limit, forward recovery, and stoppie input exclusion alongside existing handling tests. New-map traffic counts and device driving still need visual playtesting.

## Low-curb assistance

Pedaling into a static, flat-topped curb up to 0.30 m high now steps the bicycle onto it. The assist checks the curb height and upward/forward clearance, then moves the frame and both wheel bodies together without adding velocity or changing acceleration. It also works once reverse is engaged. It is disabled while airborne, suspended, tipped, or using wheelie/jump input. Tall walls, dynamic objects and overhead obstructions remain blocking. The `curbHeight` controller setting controls the maximum height.

Tests cover a 20 cm sidewalk, unchanged joint spacing/velocity, tall-wall rejection, idle/stunt exclusion and overhead clearance. Actual new-map curb geometry still needs in-game/device playtesting.
