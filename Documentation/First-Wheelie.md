# Part 9.5 — First Wheelie

Open **Pause → Missions → page 3 → First Wheelie → Start**. After the three-second countdown, ride forward and hold **PEDAL + WHEELIE** (keyboard: W / Up + Left Ctrl). Keep the front wheel lifted for one continuous hold. The objective HUD shows elapsed/required seconds and guidance when a broken hold resets the timer.

## Rules and progression

- Level 1 requires 10 continuous seconds; level 2 requires 35; level 3 requires 60. Each later level adds 25 seconds, through the existing 100-level career limit.
- The time limit is the required hold plus 90 seconds, allowing acceleration and recovery from interrupted holds. This preserves the requested duration progression, including long endurance holds at higher tiers.
- Physical detection requires rear-wheel support, no front-wheel contact, nose angle at least 12 degrees relative to the ground normal, and horizontal speed at least 1 m/s. Holding the control alone, lifting a stationary bicycle, or becoming airborne does not count.
- Any break in those conditions resets the continuous timer. Separate wheelies never add together. A new valid wheelie clears the reset message.
- Completion occurs when the continuous target is reached. It does not require banking stunt points or a subsequent landing; the separate stunt score system retains its landing rules.
- Pause freezes both hold duration and mission time. Crash, reset, bicycle change, travel and abandonment follow the shared mission interruption rules. Retry clears the duration and starts the same uncompleted tier with a new countdown.
- A newly completed tier grants 150 + 25 × (level − 1) career points once. Next Level starts the longer hold; replaying completed level 100 grants no further rewards.

The mission uses the highway spawn for an open starting stretch. There is no imposed route or artificial position reset during a hold; the player can steer through the existing world. First Wheelie is appended at catalog index 7, preserving every previous mission's save index, including the separate Wheelie Practice contract on page 2. Older career arrays expand automatically without losing points or tier completion.

## Verification

`python3 Tests/CoreCycling/run.py` tests continuous hold/reset behavior, pause, deadlines, rewards, save migration and the full 100-tier duration formula, alongside previous regressions.

`python3 Tests/Missions/run.py` validates menu access, countdown physics, HUD guidance, controller telemetry eligibility (wheel contact, pitch and speed), pause, completion/results, the harder next tier, crash/retry and abandon/free ride. Telemetry cases inject controlled wheel-contact and motion states to check the mission binding deterministically; they do not establish that a human can sustain each duration with the current bicycle handling.

Headless checks do not replace in-editor or iPhone/iPad ride-feel and endurance balancing. iOS Build Support is absent from the available editor, so device export and testing remain pending.
