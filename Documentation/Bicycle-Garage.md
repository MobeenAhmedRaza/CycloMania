# Part 6 — Bicycle garage

Open **Pause → Garage**. Browse with the arrows; equip an unlocked bicycle, install upgrades, or cycle through frame paint colors. Equip returns the bicycle to the current safe recovery point (Part 7) and keeps the trip distance and banked session score. An unfinished stunt/combo is canceled. The menu stays paused until Resume.

## Bicycles and progression

The catalog uses five existing physical bicycle/rider prefabs, not just renamed versions of the same model. Stable catalog indices and prefab references are stored on the gameplay HUD in `gameplay.unity`.

| Bicycle | Lifetime stunt points | Base cruise / sprint | Characteristics |
| --- | ---: | --- | --- |
| City | 0 | 67.5 / 135 km/h | Balanced |
| BMX | 500 | 59.4 / 118.8 km/h | Quick steering, stronger hops |
| Mountain | 1,500 | 64.8 / 129.6 km/h | Strong drive and braking |
| Trials | 3,000 | 48.6 / 97.2 km/h | Low-speed steering and highest hop |
| Fatbike | 5,000 | 54 / 108 km/h | Steady steering, relaxed speed |

The handling update triples cruise/sprint caps and braking deceleration while keeping drive force and torque unchanged. See `Handling-Update.md` for steering behavior and brake values.

Only **banked** stunt combos earn lifetime garage points. Crashed or unfinished combos earn nothing. Restarting a ride does not erase garage progress. These are progression milestones, not coins/gems or purchases; Part 12 adds an alternative coin purchase path and gem paint in the Shop; see `Economy.md`.

Three upgrade tiers unlock at 300 / 1,000 / 2,500 lifetime points. Each bicycle saves its own installed tiers. Installation does not spend lifetime points.

- Drivetrain: +6% sprint/cruise speed and +12% drive force/torque per tier.
- Brakes: +15% deceleration per tier.
- Handling: +4% steering angle/hop strength and +5% airborne flip speed per tier.

The stats displayed on the card are the same values applied to the controller. Reapplying a setup calculates from base stats, so bonuses cannot compound accidentally.

## Paint and saving

Stock, cyan, red, gold, and white frame finishes are available on unlocked bicycles. A renderer property block tints the frame without modifying shared materials, wheels, or rider skin. Stock clears the override.

A versioned JSON save in `PlayerPrefs` under `Cyclomania.Garage.v1` stores lifetime points, the equipped bicycle, and per-bicycle upgrades/paint. Saves happen when a combo banks or a setup changes. Saved selection is equipped on startup. Invalid arrays, out-of-range values, unearned tiers, corrupt JSON, and unsupported versions normalize to valid/default data. Save failures leave the running setup intact and display a retry notice. PlayerPrefs is local device storage, not cloud synchronization or cheat-resistant storage.

## Integration

`GameplayGarage.cs` implements the garage portion of `GameplayHud`; `GarageProgress.cs` owns catalog, progression, validation and tuning. A switch creates the selected full prefab, carries over touch bindings and the same stunt-scoring instance, rebinds camera/minimap/reset, and removes the old bicycle. Existing scene-level bicycle audio reacquires the active controller. Recovery owns crash handling; the legacy ragdoll controller stays disabled.

The garage waits one frame for the new rig to initialize before Resume. This works while time is paused. The UI fits the existing 1100 × 570 safe-area panel.

## Verification

- `python3 Tests/CoreCycling/run.py`: existing cycling/touch/HUD/stunt coverage plus garage progression, serialization, actual controller tuning, paint, menu navigation, and swapping a synthetic rig.
- `python3 Tests/Garage/run_assets.py`: isolated Unity play-mode smoke check using all five actual prefabs, including initialization, paint, upgrade, riding, camera target and reset. Uses cached Unity packages; does not read or write player saves.
- `python3 Tests/TouchControls/check_layout.py`: touch bounds, overlap and scene wiring.

Headless tests cannot judge appearance or riding feel. Inspect paint, rider poses, garage readability and tuning in Unity and on an iPhone/iPad. iOS Build Support is still required before exporting a device build.
