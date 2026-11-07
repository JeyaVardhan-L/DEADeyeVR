# Changelog

All notable changes to the **DEADeye** project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.2.0] - 2026-11-06

### Added
- **Physical Headset Head Tracking Fix**: Integrated direct hardware polling of `UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.CenterEye)` in `DEADeyePlayerController.cs`, enabling 360-degree head rotation on physical Meta Quest 2 hardware.
- **Standing Eye Height Safeguard**: Automated dynamic elevation check preventing floor-level viewpoints when Guardian floor calibration is uncalibrated or during seated play.
- **Stand-Alone Quest 2 APK Build Pipeline**: Added automated headless build target in `DEADeyeAndroidBuilder.cs` outputting signed ARM64 release package `Builds/Android/DEADeye_Quest2.apk` ($55.5\text{ MB}$).
- **Professional Documentation Suite**: Re-architected documentation suite into 12 specialized manuals covering Gameplay, Controls, Architecture, OpenXR, Ballistics, AI, and Testing.

### Changed
- **`TrackedPoseDriver` Configuration**: Set `ignoreTrackingState = true` and programmatically enabled all referenced `InputAction` properties (`Position`, `Rotation`, `Tracking State`) at runtime.
- **`XROrigin` Defaults**: Updated prefab to `RequestedTrackingOriginMode = Floor` (Mode 2) with default camera height offset of $1.7\text{ m}$.
- **Boundary Tracker**: Corrected `DEADeyeRooftopBoundary.cs` to verify active rig in hierarchy, ensuring VR character controllers are accurately tracked.
- **Repository Packaging**: Removed academic viva question banks and college evaluation materials, transitioning repository into a clean open-source tactical game project.

---

## [1.1.0] - 2026-11-04

### Added
- **Operation DEAD AIR**: Implemented the full 4-phase urban covert operation (Reconnaissance, Power Disruption, Technician Defense, and Commander Vance Neutralization).
- **Rooftop Traversal Network**: Constructed walkable industrial catwalks and skybridges connecting Perch 1 (North), Perch 2 (East), and Perch 3 (West) at $26.0\text{ m}$ altitude.
- **Tactical Terminal UI**: Interactive operations terminal allowing mission objective review, operative debriefing telemetry, and difficulty selection.
- **Two-Pass Ballistics Pipeline**: Implemented two-pass hitscan raycast resolving camera-muzzle parallax and eliminating wall-clipping exploits.
- **Mandatory Scope Fire Gate**: Blocked unscoped hip-firing, requiring optical reticle engagement before discharging weapon.
- **Automated QA Test Suite**: Added 27 EditMode integration tests and 5 PlayMode simulation tests in NUnit.

### Changed
- **Weapon Finish**: Removed noisy specular sparkle shaders from sniper rifle; applied tactical matte non-reflective finish.
- **Procedural Character Animator**: Enhanced trigonometric joint oscillation for walking, sprinting, and knockdown drops.

---

## [1.0.0] - 2026-11-02

### Added
- **Core Engine Foundation**: Initialized project in Unity 6.3 LTS (`6000.3.27f2`) with URP 17.3.0 and OpenXR 1.16.1.
- **Dual-Rig Subsystem**: Established `DEADeyePlayerController.cs` providing automatic switching between PC desktop simulator and Meta Quest VR rigs.
- **Basic Ballistics**: Single-shot precision sniper rifle with zoom optic and ammunition management.
- **Hostile AI State Machine**: Basic patrol, alert, and knockdown state machine on NavMesh geometry.
- **Initial Build Pipeline**: Baseline Android ARM64 compilation scripts.
