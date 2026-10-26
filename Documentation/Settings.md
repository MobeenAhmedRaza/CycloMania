# Part 11 — Settings

Open **Pause → Settings**. The page follows the reference controls: separate **Sound FX** and **Music** sliders, plus **Low / Medium / High / 2K** graphics buttons. Sliders show percentages and the selected graphics preset is highlighted gold. Changes apply immediately and save after 0.4 seconds without further adjustment. Back, app background/focus loss, shutdown and component disable flush pending changes.

## Audio

Sound FX controls the existing pedaling, freewheel and impact clips. The gameplay HUD now instantiates the project's `Sounds.prefab`, which was not connected to the gameplay scene before this change. It remains separate from bicycle replacements; its existing script reacquires the active bicycle.

Music has its own looping 2D AudioSource. A quiet, locally generated eight-second ambient chord is supplied as placeholder music. Set the HUD's `backgroundMusic` AudioClip in the Inspector to replace it with a finished track. Music continues while the in-game pause/settings menu is open so its slider can be previewed; app pause or loss of focus pauses music. Game effects retain the existing pause behavior.

`SettingsAudioChannel` restores each source's authored volume before vendor audio scripts update their fades, then applies its channel gain once in LateUpdate. This avoids repeatedly multiplying already reduced volumes and preserves procedural engine/impact levels. It handles multiple AudioSources on one object and restores the unscaled volume when removed. New sources are discovered once per second; sources added outside this service should be explicitly bound when immediate routing is needed. Music sources should use a channel with `music=true`; otherwise discovered sources are treated as effects.

AudioListener volume is initialized to 1; channel gains now control volume instead of the old temporary master-volume slider. Music/effects preferences default to 35%/100% and are independent.

## Graphics

| Preset | Frame-rate target | Maximum render long edge on mobile | Shadows | MSAA | Texture mip reduction |
| --- | ---: | ---: | --- | ---: | ---: |
| Low | 30 FPS | 1280 px | Off | Off | 1 |
| Medium (default) | 60 FPS | 1600 px | Hard, 40 m | 2× | 0 |
| High | 60 FPS | 1920 px | All, 75 m | 4× | 0 |
| 2K | 60 FPS | 2560 px | All, 100 m | 4× | 0 |

Presets also set shadow resolution and LOD bias. The labels describe settings/targets, not guaranteed device frame rates. Resolution preserves aspect ratio and never upscales beyond the display size. Portrait/landscape dimensions are normalized before scaling, and orientation changes reapply the resolution. The game remains configured for landscape on iOS.

Render-resolution changes apply only to iOS/Android players. Editor and desktop tests apply quality settings without resizing their windows. The 2K option means a render long-edge cap of 2560 pixels where supported, not a requirement to render every device at that resolution.

## Persistence

`GamePreferences` stores version 1 JSON under **Cyclomania.Settings.v1**, independently from Career, Freestyle and Garage. It normalizes missing/corrupt/unsupported data, clamps sliders and presets, and rejects nonfinite volume values. Save failures appear on the settings page; changing a setting retries saving. `GameplayHud.SettingsSaveKey` can be empty to disable storage for tests. Existing progression saves are untouched.

The runtime classes are `GamePreferences`, `GameSettings` and `SettingsAudioChannel`. GameplayHud owns the settings service, music and bicycle-sound instance. Owned audio objects, clips and channel components are cleaned up with their owner.

## Validation

Core checks cover defaults/corruption, value limits, resolution math, music setup, independent audio channels, fade preservation, non-compounding scaling, mute/unmute, multiple sources, quality application, saved reload and background flushing/music pause. Scene checks cover the Settings entry, real bicycle-effects routing, slider changes, selected presets, safe modal bounds, reopen behavior and return to riding. Tests use empty or unique disposable preference keys.

Run `python3 Tests/CoreCycling/run.py`, `python3 Tests/Missions/run.py` and `python3 Tests/TouchControls/check_layout.py`. Headless tests cannot validate perceived mix quality, actual mobile resolution, visual quality or sustained device frame rate. iOS Build Support is still absent from the available Unity editor; export and iPhone/iPad testing remain pending.
