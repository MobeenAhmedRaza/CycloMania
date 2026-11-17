# Visibility and rendering overhead — October 27, 2026

The gameplay scene referenced an occlusion bake last modified March 14, before the imported replacement map. Its main camera still used that bake. Stale baked visibility can hide the wrong geometry as the camera moves between cells. The obsolete reference is now detached, and the riding camera disables baked occlusion culling. Ordinary camera-frustum culling still works. The old asset remains on disk for reference; it is no longer used by gameplay. A future occlusion bake must be generated for the current map before re-enabling it.

The third-person collision camera previously allowed a wall to push the view as close as 5 cm to the rider's focus point. When its normal arm is obstructed within 1.5 m, it now probes alternate side directions and chooses a clearer view. The chosen direction remains collision-tested; this does not force the camera through walls or enforce an unsafe minimum distance in fully enclosed spaces. First-person view is unchanged.

Performance changes:

- The minimap renders at most 5 Hz while moving, down from 10 Hz. With less than 1 m displacement it refreshes at most once every two seconds. Its rider arrow still updates each frame; changing bicycle forces a fresh render.
- Audio routing caches known sources/channels. Discovery scans run every five seconds instead of every second; changing a volume slider uses registered channels without searching the whole scene. Channels register immediately when bound, preserving immediate music/effects settings behavior.
- The traffic manager now references the moving gameplay camera as its spectator. Its existing 150 m wheel-animation/render-distance rule can work; a null spectator previously made every traffic wheel update at all distances. Traffic remains at the requested 25% expected density.

No forward speed, reverse behavior, braking, mission progress or saved preferences were reset. Removing stale occlusion data prioritizes correct visibility; it is not a measured GPU performance gain. The reduced camera refresh and scene-scan rates remove known overhead, but device frame-time profiling is still needed to establish FPS and remaining GPU bottlenecks.

Validation: core camera tests cover tight-obstacle fallback and wall clearance; minimap tests cover stationary skips, movement refresh and target changes; settings tests cover immediate audio changes, fades and persistence. The current-map suite checks a full viewport and useful camera distance at all 13 Career entry starts, plus all 1,300 mission/level placement checks. Headless tests cannot reproduce the reported on-screen visibility artifact or measure real rendering performance. Physical iPhone/iPad verification remains pending.
