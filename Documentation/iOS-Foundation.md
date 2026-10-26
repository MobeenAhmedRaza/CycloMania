# Part 1 — iOS foundation

Implemented for Unity 6000.0.79f1. Gameplay starts from `Assets/Scenes/gameplay.unity`.

## Configuration

- Product: Cyclomania; initial bundle identifier: `com.emblem.cyclomania` (derived from the existing Android namespace). Replace with your registered identifier before signing.
- iPhone and iPad, iOS 13.0 minimum (existing deployment baseline retained).
- Device SDK, IL2CPP, ARM64, Metal; build number 1.
- Landscape left/right autorotation only, full-screen presentation, hidden status bar.
- iOS runtime requests 60 FPS and enables multitouch. Actual frame rate depends on device performance; performance tuning is Part 15.
- Gameplay Canvas scales against 1920 × 1080 using Expand to preserve the reference layout on narrower screens.
- Buttons are anchored to safe-area edges. `SafeAreaPanel` updates when screen dimensions or safe area change, including rotation and Unity Device Simulator previews. It uses UnityEngine.Device.Screen.
- The world continues to render edge to edge. Future HUD controls should go inside the safe-area panel; full-screen backgrounds stay outside it.

## Export

1. In Unity Hub, add **iOS Build Support** to Unity 6000.0.79f1. It is missing from the current installation.
2. Open this project and let Unity import the new scripts.
3. Choose **Cyclomania > iOS > Export Xcode Project** and select an output folder outside Assets. The export explicitly includes the gameplay scene.
4. Open the exported project in Xcode. Choose your Apple development team and registered bundle identifier, then build for a connected device.

No signing team or certificates were supplied, so signing has not been configured. No Xcode export or on-device run has been performed.

## Validation

New runtime and editor scripts compile against the installed Unity assemblies, including the iOS-only runtime code. Static checks cover scene references, build settings and control bounds for representative landscape phone/tablet dimensions. These checks do not replace Unity rendering or device tests.

In Unity Device Simulator, check both landscape orientations on notched and non-notched iPhones and an iPad. Confirm controls clear the notch and home indicator, remain visible after rotation, and retain their existing actions. On hardware, confirm landscape launch and background/resume behavior.

Part 3 will redesign the controls to match the supplied screenshots. This part preserves the existing actions and establishes their scaling/anchoring foundation.

Unity reference: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Device.Screen-safeArea.html
