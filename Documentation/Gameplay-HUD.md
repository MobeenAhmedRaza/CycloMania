# Part 4 — Gameplay HUD and pause

`GameplayHud` is attached to the gameplay Canvas and explicitly references the active bicycle and existing touch-control panel. It builds its Unity UI at runtime, inside safe-area containers.

## In-game display

- Speed in km/h, refreshed ten times per second.
- Session distance in km, measured from horizontal bicycle velocity during physics steps. Paused/crashed riding does not accumulate distance. Respawn teleportation is never counted, and the touch reset retains the trip distance.
- Circular north-up minimap with a cyan heading marker. A 256 × 256 overhead camera renders the local world at 10 Hz, excluding UI, with a 130 m view across. It stops refreshing while paused and releases its render texture/camera on destruction.
- Objective title/detail and optional progress bar. Defaults to FREE RIDE; crash recovery temporarily displays RECOVERING.
- Coin/gem counters default to zero; no fabricated rewards or purchases are added.
- Top-right PAUSE button. Escape also toggles pause, or returns from a submenu to the pause page.

## Pause flow

Pause freezes game time, pauses audio, clears held riding inputs, hides the riding controls, and shows a full-screen input-blocking overlay. Resume restores the previous time scale, audio pause flag, and control visibility. Backgrounding or losing focus pauses the ride; returning to the app leaves the menu open until the player resumes.

- Resume returns to riding.
- Restart Ride resets the complete bicycle to its current safe recovery point, resets trip distance and the free-ride objective, and resumes. Currency balances are retained. This restarts the current ride without reloading the city scene.
- Garage now provides bicycle selection, unlocks, upgrades, and frame paint (see `Bicycle-Garage.md`). Pause → Shop provides Part 12 currency purchases (see `Economy.md`).
- Missions opens career contracts, active-attempt controls, and mission results (Part 8; see `Mission-System.md`).
- World opens the five riding destinations and immediate travel (Part 7; see `Open-World.md`).
- Part 11 provides independent Sound FX/Music sliders, Low/Medium/High/2K graphics choices, and persisted settings. See `Settings.md`.

Disabling/destroying the HUD restores pause state so time and audio cannot be left frozen by scene teardown.

## Later integration

`GameplayHud.Session` exposes session data:

- `SetObjective(title, detail, progress)` — progress 0–1; omit or pass -1 to hide the bar.
- `SetCurrency(coins, gems)` — supplies actual economy balances, clamped to nonnegative values.
- `DistanceMeters` — session travel total.

The current HUD does not persist money or mission progress. `equippedBicycleName` supplies the Garage's player-facing display name.

## Checks

`python3 Tests/CoreCycling/run.py` runs the isolated Unity suite, including telemetry, pause/resume restoration, Garage/Settings navigation, volume adjustment, restart, background pause, and minimap wiring. Runtime errors now fail the suite even when individual assertions pass.

`python3 Tests/TouchControls/check_layout.py` checks existing control placement and scene wiring. The HUD header and 1100 × 570 pause panel use the same 1920 × 1080 reference scaling and safe-area system.

The automated checks use a headless Unity fixture. Visual inspection of the full city/minimap and device performance still require Unity playtesting and an iPhone/iPad build. iOS Build Support remains required for device export.
