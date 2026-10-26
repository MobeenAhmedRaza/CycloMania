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
