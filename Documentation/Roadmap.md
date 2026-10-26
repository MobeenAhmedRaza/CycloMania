# Cyclomania implementation roadmap

User-confirmed breakdown, restored October 24, 2026. This defines scope, not a claim that every item is already complete. Single player only; bicycles replace motorcycles. Career mission types repeat at increasing difficulty.

| Part | Scope |
| --- | --- |
| 1 | iOS foundation: landscape, scaling, safe areas, build settings |
| 2 | Core cycling: movement, balance, terrain, crashes, respawn, third-person camera |
| 3 | Screenshot-style multi-touch steering, pedal, brake and action controls |
| 4 | HUD, minimap, objectives, currency, pause/resume, restart, garage/settings entry |
| 5 | Wheelies, stoppies, jumps, flips, landing validation, tracking, combos, score |
| 6 | Bicycle garage: categories, selection, stats, unlocks, upgrades, cosmetics, saves |
| 7 | World: downtown, industrial, mountain, highway, desert; routes, ramps, checkpoints, traffic, spawn points, markers |
| 8 | Mission lifecycle, objectives, timers, failures, rewards, levels, scaling and saves |
| 9 | Ten Career mission types, individually requestable as 9.1–9.10 |
| 10 | Freestyle riding, zone challenges, hidden tokens, points, records, rewards, zone unlocks |
| 11 | Splash/main menu, mode/map selection, loading, profile, achievements, audio and graphics settings |
| 12 | Coins/gems, reward tuning, prices, unlock requirements, purchase/reward persistence |
| 13 | Advanced: first-person camera, weather, day/night, cycling school, skill tree, replay export |
| 14 | Daily/weekly challenges, login rewards, rotating events, optional rewarded ads, cosmetics purchases, season pass |

Part 13 implementation and device-validation limits: `Advanced-Features.md`. Consult each feature document for implemented scope and remaining gaps; for example, the existing Part 11 document describes Settings, and the Freestyle document identifies content still deferred.

Part 14 implementation and limits: `Recurring-Content.md`. Rewarded ads remain unavailable by explicit user preference.
