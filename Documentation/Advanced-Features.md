# Part 13 — Advanced features

Open **Pause → Advanced**. The in-ride **View** button also switches camera perspective. Preferences, completed school lessons and learned skills use `Cyclomania.Advanced.v1`. Existing Career, Freestyle, Garage and Economy saves remain separate.

## 13.1 First-person camera

The eye-height view follows heading with a level horizon to avoid rolling the screen during flips. Rider skinned meshes are hidden while this view is active and restored on returning to third person or changing bicycle. The existing third-person collision avoidance remains intact. The view preference persists; both perspectives follow garage swaps and respawns.

## 13.2 Weather

The weather button cycles Clear → Rain → Fog → Snow. Rain and snow use bounded, local particle systems following the rider (maximum 500 particles). Fog and lighting change visibility. Weather deliberately does not alter grip, acceleration or braking, preserving the handling requested earlier. These are visual conditions, not a weather forecast or an automatic weather scheduler.

## 13.3 Day/night

Choose Noon, Dusk, Midnight or Dawn. Optional Day Cycle advances one game day in 20 minutes of unpaused play. Lighting, ambient color and fog color follow the sun. The chosen starting preset and cycle preference persist; loading starts from that preset. Previous render settings are restored when the environment service is destroyed.

## 13.4 Cycling school

A dedicated practice course provides three sequential lessons:

1. Keep in the right-hand lane, below 20 km/h, and reach the finish.
2. Stop inside the red approach box for two continuous seconds before crossing the line, then finish.
3. Wait behind the line while the signal indicator is red (first ten seconds), then cross on green and finish.

The same lane and speed rules apply throughout all lessons. Speed checking allows 1 km/h tolerance. Crashes, resets, leaving the lane, or teleporting interrupt the lesson. Pausing freezes lesson progress and the signal timer. Instructions and the current signal appear above the bottom controls. School replaces the current mission attempt and supplies a temporary respawn point. **Advanced → Leave School** returns to the previous checkpoint. Leave and re-enter School to retry or choose another lesson.

Each lesson's first completion earns one skill point; replays award none. These are simplified closed-course rules, not jurisdiction-specific traffic instruction. Citywide traffic-law enforcement, pedestrian right-of-way and turn-signalling are outside this implementation.

## 13.5 Skill tree

Earn one point per school lesson and every five completed Career tiers. Three branches have three tiers each:

- Balance: adds 2 to grounded balance assistance per tier (base 8).
- Recovery: reduces crash recovery by 0.35 seconds per tier (base 2 seconds).
- Air Control: adds 10% aerial rotation speed per tier, on top of garage handling upgrades.

Learn one Balance tier before Recovery, and one Recovery tier before Air Control. No currency is charged. Effects are assigned from saved tiers, so reopening screens never compounds them. Bicycle changes and garage upgrades reapply them. Skill learning saves before applying the purchase; save errors appear in the skill screen.

## 13.6 Replay video export

On supported iPhones/iPads, Record starts a screen-video recording with game audio, microphone and selfie camera disabled. Resume riding, return to Advanced, and Stop. Preview/Share opens the native video preview, where the player chooses whether to save/share. Discard removes the pending clip; a pending clip must be discarded before starting another. A recording indicator remains visible during gameplay. Backgrounding or destroying the recorder stops recording. Recording and export are disabled in the Editor/unsupported platforms.

This records the ride as video; it does not reconstruct an earlier ride from telemetry or provide an in-engine ghost replay. Native video permission, cancellation, interruption, storage limits, preview and share-sheet behavior need physical-device testing. The existing Unity 6000.0 ReplayKit wrapper is used; Unity documents its future removal, so a future engine upgrade needs a recording-backend review.

API references: [Unity StartRecording](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/apple/replaykit/replaykit/startrecording), [Unity StopRecording](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/apple/replaykit/replaykit/stoprecording), [Apple native preview](https://developer.apple.com/documentation/replaykit/rpscreenrecorder/stoprecording(handler:)).

## Validation

Core tests exercise corrupted saves, skill prerequisites/spending, save round trips, school rules/failures/pause/teleport rejection, weather presets and render-state cleanup. Scene tests exercise Advanced navigation, both cameras, weather/time controls, skill application, school start/reset/return and unsupported recording controls. Existing physics, missions, economy and settings suites are also run.

Headless validation cannot establish visual quality, comfort, precipitation performance or native iOS video export. Validate those on a device before release.
