# Authored map levels

`Assets/Scenes/gameplay.unity` contains the saved `Level Layout` hierarchy. Its five course groups, arrival transforms, canyon start, and cycling school exist before Play mode. Imported road meshes have saved mesh colliders. The disabled original city remains in the scene.

Move a whole course group to translate its geometry and mission coordinates together. Keep course group rotation at identity and scale at one: race, pursuit, and jump rules use northbound map coordinates. Adjust `Arrival` children to change travel/respawn points. `RideLevelLayout` stores the playable bounds and local freestyle bounds. Individual course geometry can be edited normally; moving a target-bearing obstacle individually also requires updating the corresponding catalog target.

`RideLevelAuthoring` is editor-only and writes persistent meshes/materials to `Assets/Cyclomania/LevelAssets`. The menu refuses to overwrite an existing layout. Runtime `RideWorld` handles travel and recovery; it does not build or destroy course geometry. Mission indicators, riders, UI, and weather remain dynamic.

Run `python3 Tests/World/run.py` with Unity 6000.0.79f1 installed. The runner copies all map dependencies to a disposable project and checks saved course surfaces, clear spawn footprints, full climb/trail/highway/school routes, low/high-tier mission targets, freestyle targets, travel, mission placement, and school startup without geometry creation. GPU traffic is disabled only inside the headless validation copy; normal game traffic is unchanged. Visual appearance and GPU traffic require a normal Unity Play-mode check.
