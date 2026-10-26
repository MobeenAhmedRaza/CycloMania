# Part 3 — iOS touch controls

Implemented in `Assets/Scenes/gameplay.unity`, inside the existing safe-area panel.

| Position | Control | Interaction |
| --- | --- | --- |
| Bottom left | Left / right arrows | Hold to steer; both together cancel touch steering. |
| Right, upper main button | Up arrow | Hold to pedal. |
| Right, lower main button | Down arrow | Hold to brake; braking wins over pedaling. |
| Beside pedal | Sprint | Hold while pedaling. |
| Beside brake | Jump | Hold to charge the existing bunny hop, release to jump. |
| Above left steering | Wheelie | Hold while pedaling. Existing wheelie support is enabled on the active bicycle. |
| Above right steering | Reset | Tap to respawn at the starting location. |

The controls use translucent dark circular backgrounds, white outlines/icons, concise action labels, and cyan pressed feedback. Icons are generated as Unity UI geometry so they stay sharp at different screen sizes. The seven cycling controls use direct scene references to the bicycle; reset calls `CycleRecovery.Respawn()`.

Main buttons are 180 reference pixels, action buttons 156, and reset 128, on the existing 1920 × 1080 scaling setup. They stay anchored to the safe area, with a clear center view. The smallest button is approximately 44 points on a 1334 × 750, 2× iPhone display. Square hit bounds around the visible circles make edge taps forgiving.

## Input behavior

- Each button tracks its own pointer IDs. Multiple fingers can operate different controls or hold the same control; one finger lifting does not cancel the others.
- Sliding off a control cancels its hold. Sliding back requires another press.
- Pause, disabling a control, focus loss, app suspension, and screen-size/orientation changes clear held touches.
- Forced cancellation and intentional release are distinct, so canceling a jump hold does not launch a jump.
- Reset, crash recovery, and the bicycle's input-clear path release pointer ownership as well as input values.
- Keyboard controls remain available for editor playtesting.

The existing Unity StandaloneInputModule remains active, with multitouch enabled by the iOS foundation. Physical iOS event delivery and touch comfort still require testing on an iPhone/iPad.

## Validation

Run `python3 Tests/TouchControls/check_layout.py` for serialized scene checks. It checks 40 control/safe-area bounds and 140 button pairs for overlap across representative phone/tablet layouts, plus direct bindings, reset wiring, and scene IDs.

Run `python3 Tests/CoreCycling/run.py` for the isolated Unity physics and touch suite. Tests cover independent holds, multiple fingers on one button, unrelated pointer releases, drag-off cancellation, brake priority, disabled/paused/focus/rotation cleanup, jump cancellation versus release, one-shot actions, and all eight icon meshes, alongside Part 2's physics checks.

This part adds controls for existing actions. Advanced stunt mechanics and scoring are now implemented in Part 5; see Stunts-and-Scoring.md. Career challenge tracking comes in later parts. The iOS build module is still required before device testing.
