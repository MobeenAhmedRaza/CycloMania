# Part 2 — Core cycling

The existing Simple Bicycle Physics controller and rider/pedal geometry are retained. The active standard bicycle in `Assets/Scenes/gameplay.unity` now has crash recovery, and the camera explicitly follows that bicycle instead of the inactive mountain bicycle.

## Riding

- W / Up arrow or Forward: pedal. A/D or Left/Right arrows and the existing touch buttons: steer.
- S / Down arrow or Backward: brake. This button no longer propels the bicycle backward. Natural rollback under gravity remains possible.
- Left Shift / Sprint: sprint. The active bicycle's target is 6.25 m/s (22.5 km/h) normally and 12.5 m/s (45 km/h) sprinting. Targets limit powered acceleration, not downhill gravity.
- Keyboard and touch share one smoothing path. Braking takes priority over pedaling. Missing mobile buttons no longer cause startup exceptions.
- Wheel probes ignore the bicycle itself and triggers, report airborne state even when no ground is found, and supply a terrain normal for slope-aligned propulsion and assisted balance.
- Grounded resistance slows coasting. Braking opposes motion without applying reverse propulsion. Pedaling in midair provides no thrust.
- Frame/wheel continuous collision detection and frame interpolation are enabled. Tire materials are cloned per bicycle instead of modifying imported shared materials.
- Existing basic wheelie/hop inputs remain; their full stunt system and scoring belong to Part 5.

## Crash and recovery

`CycleRecovery` listens to frame and wheel impacts. Normal impact speed at or above 9 m/s causes a crash; shallow glancing contacts use their normal component. A sustained grounded tip-over or falling 40 m below the start also triggers recovery.

A crash suppresses controller forces/balance and clears held inputs. The physical bicycle can fall; this phase does not add a separate rider ragdoll. After two seconds, the entire rigidbody/joint assembly resets to the starting location with zero velocities and brief impact protection. R resets manually in the editor; `CycleRecovery.Respawn()` is available for later UI/mission integration. This part uses the initial spawn rather than unverified roadside checkpoints.

The legacy BicycleStatus recovery routine is disabled on this bicycle so two systems cannot drive recovery at once. New `Crashed` and `Respawned` events are available for future HUD/mission logic.

## Camera

Third-person follow uses frame-rate-independent smoothing and a stable world-up horizon. A sphere cast pulls the camera forward when an obstacle blocks the view, ignores the bicycle and triggers, and snaps after respawn. Device testing is still needed for tight spaces and final camera tuning.

## Validation

All runtime scripts and editor foundation scripts compile against Unity 6000.0.79f1. Existing asset deprecation/unused-field warnings remain.

Nine checks passed in a disposable Unity project using actual Unity physics: flat ground, pedaling, braking without reversing, losing ground contact, no airborne thrust, slope normals, crash input suppression, assembly/velocity reset, and camera wall avoidance.

The reproducible runner is `python3 Tests/CoreCycling/run.py` (requires an imported project with Library/ScriptAssemblies/UnityEngine.UI.dll and a licensed Unity editor). Use `--unity /path/to/Unity` for another installation. Test fixtures isolate controller behavior; they do not certify the complete city scene or replace playtesting on iPhone/iPad.

Manual acceptance: ride the gameplay scene on flats and ramps, coast, brake from speed, steer while pedaling, hit a wall, wait for automatic recovery, press R, and move the camera near buildings. Final handling/performance tuning needs in-editor and on-device playtesting. iOS Build Support remains absent from the current installation, as noted in Part 1.
