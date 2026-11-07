# DEADeye — Permanent Project Handover & Re-entry Guide

```text
========================================================================================
DOCUMENT     : Permanent Project Handover & Re-entry Guide
PROJECT      : DEADeye — Urban Tactical Operations
AUTHORITATIVE: https://github.com/JeyaVardhan-L/DEADeyeVR.git
TARGET ENGINE: Unity 6.3 LTS (6000.3.27f2)
TARGET VR    : Meta Quest 2 / Quest 3 / Quest Pro (OpenXR 1.16.1 / Android ARM64)
DESKTOP SIM  : Windows Standalone PC (Keyboard & Mouse)
LATEST COMMIT: e0a4922
LATEST REL   : v1.2.0 (DEADeye_Quest2.apk)
STATUS       : Verified & Documented
========================================================================================
```

---

## Purpose of this Guide

This guide is designed for two audiences:
1. **The original developer returning to DEADeye after months or years away.**
2. **A new engineer, technical artist, or contributor inheriting this repository with zero prior context.**

This repository is completely self-contained. It does not require access to any lost local workstations, unversioned folders, or the original author. If you have Git, Unity 6.3 LTS, and this repository, you have 100% of the game.

---

## Table of Contents

1. [Project Overview & Design Pillars](#1-project-overview--design-pillars)
2. [What Is Implemented vs. What Is Planned](#2-what-is-implemented-vs-what-is-planned)
3. [Environment Recovery & Setup](#3-environment-recovery--setup)
4. [Project Architecture & Communication Patterns](#4-project-architecture--communication-patterns)
5. [Core Scripts, Scenes & Prefabs Reference](#5-core-scripts-scenes--prefabs-reference)
6. [Development & Testing Workflows](#6-development--testing-workflows)
7. [Building for Windows & Meta Quest](#7-building-for-windows--meta-quest)
8. [Current Status & Known Limitations](#8-current-status--known-limitations)
9. [Taking Over Development Responsibly](#9-taking-over-development-responsibly)
10. [Troubleshooting & Environment Disaster Recovery](#10-troubleshooting--environment-disaster-recovery)

---

## 1. Project Overview & Design Pillars

**DEADeye** is a tactical first-person urban sniper simulation developed for **Meta Quest standalone VR** and **PC Desktop** (via an integrated keyboard/mouse simulator).

### The Core Experience
The player operates from a continuous 26-meter rooftop overwatch complex in the fictional metropolis of New Babel. Rather than a static shooting gallery, the operative navigates open catwalks and structural skybridges connecting three distinct tactical perches:
- **Perch 1 (North Main)**: Overlooks street transit arteries and the primary communications jammer.
- **Perch 2 (East Plaza Overlook)**: Provides line of sight over the public square and civilian pathways.
- **Perch 3 (West Construction Zone)**: Flanking perch overseeing high-voltage utility junctions and escape routes.

### Design Pillars
1. **Tactical Precision Over Reflexes**: Positive target designation, situational awareness, and collateral damage avoidance are mandatory.
2. **Two-Pass Ballistic Realism**: Ballistics prevent camera-to-muzzle parallax exploits and physical barrier penetration bugs.
3. **Mandatory Scope Fire Gate**: Hip-firing is physically locked out; discharging the weapon requires acquiring sight alignment through the optical scope.
4. **Hardware Parity**: The game runs identically on standalone VR headsets (6DoF Touch controllers) and desktop PCs (WASD + Mouse) using a dynamic dual-rig architecture.
5. **Zero-Dollar Budget Discipline**: Built strictly using permissive open-source assets (CC0, CC-BY 4.0, Unity Companion License) with a sub-56 MB APK footprint.

---

## 2. What Is Implemented vs. What Is Planned

| Feature / Domain | Implemented in Current Build (`v1.2.0`) | Planned Future Milestones |
|---|---|---|
| **Campaign Mission** | **Operation DEAD AIR** is 100% playable end-to-end with 4 sequential phases, 120s timer, and debriefing score. | Operations 02 (*Broken Signal*), 03 (*Black Horizon*), and 04 (*Silent Citadel*) currently exist as briefing metadata cards. |
| **Ballistics** | Two-pass raycast model with muzzle obstacle detection, distance drop, and anatomical hit zones ($3\times$ headshot, $1\times$ torso). | Aerodynamic bullet drag curves and dynamic crosswind vectors. |
| **Optics** | Dual-lens scope camera rendering to RenderTexture with $4\times, 8\times, 12\times$ magnification and scope fire lockout. | Thermal vision, night vision, and bullet trajectory camera fly-bys. |
| **AI Systems** | 5-state Hostile FSM (`Patrol`, `Alert`, `Combat`, `Knockdown`, `Escape`) with NavMesh pathfinding and procedural limb oscillation. | Motion-captured skeletal animation blending, squad cover leapfrogging, and dynamic vehicle convoys. |
| **Reconnaissance** | Optical spotting reticle designating targets with persistent world-space HUD markers. | Drone reconnaissance feeds and acoustic gunshot triangulation. |
| **Player Rigs** | Automated dual-rig detection: Desktop KBM simulator in Editor/PC, OpenXR 6DoF Touch rig on Meta Quest. | In-game seated/standing height calibration slider. |
| **Automated QA** | 32 automated tests (27 EditMode, 5 PlayMode) passing with 100% success rate in CI/headless runners. | Automated graphics performance regression benchmarker. |

---

## 3. Environment Recovery & Setup

### Required Tools & Versions
- **Unity Editor**: **`6000.3.27f2`** (Unity 6.3 LTS). Verify against `ProjectSettings/ProjectVersion.txt`.
- **Unity Modules**:
  - `Android Build Support`
  - `Android SDK & NDK Tools`
  - `OpenJDK`
  - `Windows Build Support (IL2CPP / Mono)`
- **Host System**: Windows 10/11 64-bit with 16 GB+ RAM and DirectX 12 capable graphics.
- **Git**: Standard Git 2.30+ installed with Git Credential Manager.
- **Headset Deployment**: Android Debug Bridge (`adb`) or Meta Quest Developer Hub (MQDH).

### Cloning & First Launch
```bash
# 1. Clone the authoritative repository
git clone https://github.com/JeyaVardhan-L/DEADeyeVR.git
cd DEADeyeVR

# 2. Inspect Git state to confirm clean HEAD
git status
git log -n 3 --oneline
```

### Opening in Unity
1. Open **Unity Hub** $\rightarrow$ Click **Add** $\rightarrow$ **Add project from disk**.
2. Select the cloned `DEADeyeVR` folder.
3. Confirm the editor version shows **`6000.3.27f2`**.
4. Launch the project. On first open, Unity will:
   - Resolve dependencies from `Packages/manifest.json`.
   - Recreate empty directories matching tracked `.meta` files.
   - Compile all C# assemblies into `Library/ScriptAssemblies/`.
   - Populate the AssetDatabase (takes 2–4 minutes on modern SSDs).

### Running the Existing Game
1. In the Project window, navigate to:
   [`Assets/DEADeye/Scenes/RooftopMission.unity`](../Assets/DEADeye/Scenes/RooftopMission.unity).
2. Double-click to open the scene.
3. Press **Play** in the Unity Editor toolbar.
4. Left-click inside the Game viewport to lock the mouse cursor.
5. You are now playing the desktop simulator:
   - **WASD**: Traverse the rooftop and catwalks.
   - **Mouse**: Aim and look around.
   - **Hold RMB**: Enter optical scope zoom.
   - **LMB (while holding RMB)**: Discharge rifle.
   - **R**: Reload magazine.

---

## 4. Project Architecture & Communication Patterns

DEADeye follows a decoupled, service-oriented architecture designed to prevent tight coupling between VR hardware, UI, audio, and gameplay logic.

```
                    ┌──────────────────────────┐
                    │    Input System Router   │
                    │   (Desktop KBM vs XRI)   │
                    └─────────────┬────────────┘
                                  │
         ┌────────────────────────┴────────────────────────┐
         ▼                                                 ▼
┌──────────────────┐                             ┌──────────────────┐
│ DEADeyeDesktopRig│                             │  VR_XROriginRig  │
│ (WASD, MouseLook)│                             │ (OpenXR, Touch)  │
└────────┬─────────┘                             └────────┬─────────┘
         │                                                │
         └────────────────────────┬───────────────────────┘
                                  ▼
                    ┌──────────────────────────┐
                    │ DEADeyePlayerController  │
                    │   (Unified Player Hub)   │
                    └─────────────┬────────────┘
                                  │
         ┌────────────────────────┼────────────────────────┐
         ▼                        ▼                        ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│DEADeyeSniperWeapon│    │DEADeyeRooftopBndy│    │  DEADeyeSpotable │
│(Two-Pass Optics) │    │(26m Safety Clamp)│    │ (Target Recon)   │
└────────┬─────────┘    └──────────────────┘    └────────┬─────────┘
         │                                               │
         └────────────────────────┬──────────────────────┘
                                  ▼
                    ┌──────────────────────────┐
                    │  DEADeyeMissionDirector  │
                    │  (Phase FSM & Scoring)   │
                    └─────────────┬────────────┘
                                  │
         ┌────────────────────────┼────────────────────────┐
         ▼                        ▼                        ▼
┌──────────────────┐    ┌──────────────────┐    ┌──────────────────┐
│ DEADeyeHostileAI │    │ DEADeyeAudioMgr  │    │ DEADeyeResultsUI │
│ (5-State Patrol) │    │ (Spatial Audio)  │    │ (Score Telemetry)│
└──────────────────┘    └──────────────────┘    └──────────────────┘
```

### Key Architectural Patterns
1. **Dynamic Dual-Rig Abstraction (`DEADeyePlayerController.cs`)**:
   Instead of maintaining separate scenes for desktop testing and VR deployment, a single scene contains both rigs under `DEADeyePlayerController`. At runtime `Awake()`, the system queries `XRSettings.isDeviceActive` and `InputDevices.GetDeviceAtXRNode(XRNode.CenterEye)`:
   - If a VR headset is detected, `VR_XROriginRig` activates with OpenXR Touch tracking and floor offset ($1.7\text{ m}$).
   - If running in the Editor or on PC without a headset, `DEADeyeDesktopRig` activates with standard mouse-look and character controller movement.

2. **Perimeter Boundary Clamping (`DEADeyeRooftopBoundary.cs`)**:
   Operatives cannot fall off rooftops or clip through railing colliders. A bounding box mathematical clamp monitors the player transform each `FixedUpdate()`, smoothly recovering the player to the nearest catwalk waypoint if an unexpected PhysX displacement occurs.

3. **Two-Pass Ballistics Pipeline (`DEADeyeSniperWeapon.cs`)**:
   - **Pass 1 (Camera Sightline)**: Raycasts from the optical camera through the reticle center to establish the intended point of impact.
   - **Pass 2 (Muzzle Vector)**: Raycasts from the rifle's physical muzzle socket to the Pass 1 impact coordinate. If an intermediate wall, catwalk railing, or chimney is struck first, the impact registers on that obstacle, completely eliminating parallax exploits and wall-clipping cheats.

4. **Event-Driven Communication**:
   Systems communicate via C# delegates and event actions rather than polling:
   - `DEADeyeSniperWeapon.ExecuteShot` $\rightarrow$ Consumes ammo, triggers muzzle flash VFX, spatial gunshot audio, and recoil oscillation.
   - `DEADeyeHostileAI.OnEliminated` $\rightarrow$ Notifies `DEADeyeMissionDirector` to advance objective counters.
   - `DEADeyeMissionDirector.OnPhaseChanged` $\rightarrow$ Updates HUD objective text, triggers radio Foley, and activates tactical terminal indicators.

---

## 5. Core Scripts, Scenes & Prefabs Reference

All custom gameplay code resides under [`Assets/DEADeye/Scripts/`](../Assets/DEADeye/Scripts/):

| Namespace / Subdirectory | Key Class | Purpose & Operational Function |
|---|---|---|
| `Player/` | [`DEADeyePlayerController.cs`](../Assets/DEADeye/Scripts/Player/DEADeyePlayerController.cs) | Coordinates desktop and VR rigs, health, movement speed, and active weapon reference. |
| `Player/` | [`DEADeyeDesktopRig.cs`](../Assets/DEADeye/Scripts/Player/DEADeyeDesktopRig.cs) | PC keyboard & mouse FPS controller with pitch/yaw clamping and cursor locking. |
| `Weapons/` | [`DEADeyeSniperWeapon.cs`](../Assets/DEADeye/Scripts/Weapons/DEADeyeSniperWeapon.cs) | Two-pass ballistics, magazine ammo counters, reload delays, and optical fire gate enforcement. |
| `Weapons/` | [`DEADeyeScopeOptic.cs`](../Assets/DEADeye/Scripts/Weapons/DEADeyeScopeOptic.cs) | Manages secondary scope camera, FOV zoom transitions, and reticle shader material properties. |
| `AI/` | [`DEADeyeHostileAI.cs`](../Assets/DEADeye/Scripts/AI/DEADeyeHostileAI.cs) | NavMesh agent FSM managing `Patrol`, `Alert`, `Combat`, `Knockdown`, and `Escape` states. |
| `AI/` | [`DEADeyeProceduralCharacterAnimator.cs`](../Assets/DEADeye/Scripts/AI/DEADeyeProceduralCharacterAnimator.cs) | Trigonometric joint oscillation scaling limb rotation to NavMesh linear velocity. |
| `Mission/` | [`DEADeyeMissionDirector.cs`](../Assets/DEADeye/Scripts/Mission/DEADeyeMissionDirector.cs) | Master state machine for Operation DEAD AIR phases, 120s timer, scoring, and failure conditions. |
| `Recon/` | [`DEADeyeSpotable.cs`](../Assets/DEADeye/Scripts/Recon/DEADeyeSpotable.cs) | Target designating component; tracks optical dwell time before confirming HUD tag. |
| `Player/` | [`DEADeyeRooftopBoundary.cs`](../Assets/DEADeye/Scripts/Player/DEADeyeRooftopBoundary.cs) | Bounding box perimeter enforcement preventing void falling. |
| `Environment/` | [`DEADeyeEnvironmentalProp.cs`](../Assets/DEADeye/Scripts/Environment/DEADeyeEnvironmentalProp.cs) | Destructible power junctions, electrical arcing, and explosive conduit hazards. |
| `UI/` | [`DEADeyeResultsUI.cs`](../Assets/DEADeye/Scripts/UI/DEADeyeResultsUI.cs) | Post-operation performance summary, hit accuracy percentage, and combat rating calculation. |

### Essential Asset Locations
- **Sole Build Scene**: `Assets/DEADeye/Scenes/RooftopMission.unity` (Build index `0`).
- **Player Rig Prefab**: `Assets/DEADeye/Prefabs/Player/DEADeye_Player_DualRig.prefab`.
- **Sniper Weapon Prefab**: `Assets/DEADeye/Prefabs/Weapons/XM500_SniperRifle.prefab`.
- **Character Models**: `Assets/DEADeye/Art/Characters/` (Quaternius modular models & GLB characters).
- **Audio WAVs**: `Assets/DEADeye/Audio/` (Mono 16-bit 44.1 kHz spatial audio clips).
- **Mission ScriptableObjects**: `Assets/DEADeye/Data/Missions/Operation_DeadAir.asset`.
- **Project Settings**: `ProjectSettings/` (All layer matrices, URP Forward+, and OpenXR settings).

---

## 6. Development & Testing Workflows

### How to Create Changes Safely
1. **Always branch from `master`**:
   ```bash
   git checkout master
   git pull origin master
   git checkout -b feat/your-feature-name
   ```
2. **Make focused, testable edits**. Avoid modifying unrelated asset imports or project settings.
3. **Keep metadata in sync**: When renaming or moving assets in Unity, always perform the move inside the Unity Project window so `.meta` GUIDs are updated automatically.

### Running Automated Verification Tests
DEADeye includes 32 automated tests in `Assets/DEADeye/Tests/`:
- **EditMode Suite (27 tests)**: Verifies ammo consumption, optical zoom step cycling, boundary clamping, hostile damage subtraction, and mission phase triggers without entering Play mode.
- **PlayMode Suite (5 tests)**: Simulates 60 frames of runtime physics, NavMesh waypoint traversal, projectile raycast impacts, and mission debriefing flow.

**To run tests inside the Editor:**
1. Open **Window $\rightarrow$ General $\rightarrow$ Test Runner**.
2. Select **EditMode** $\rightarrow$ Click **Run All**.
3. Select **PlayMode** $\rightarrow$ Click **Run All**.

**To run tests headlessly via CLI:**
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" `
  -batchmode -projectPath "." `
  -runTests -testPlatform EditMode `
  -testResults "TestResults_EditMode.xml" `
  -logFile "test_editmode.log"
```
Ensure all tests report 100% pass before opening a Pull Request.

---

## 7. Building for Windows & Meta Quest

### Building Standalone Meta Quest 2 APK

#### Method 1: Headless Automated CLI (Fastest & Recommended)
Execute from PowerShell in the repository root:
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "." `
  -executeMethod "DEADeye.Editor.DEADeyeAndroidBuilder.BuildQuest2Apk" `
  -logFile "build_quest.log"
```
The compiled package is written to: `Builds/Android/DEADeye_Quest2.apk`.

#### Method 2: Via Unity Editor GUI
1. Open **File $\rightarrow$ Build Settings**.
2. Switch platform to **Android**.
3. Set **Texture Compression** to **ASTC**.
4. Confirm `Assets/DEADeye/Scenes/RooftopMission.unity` is checked as Scene 0.
5. Click **Build** $\rightarrow$ Choose destination `Builds/Android/DEADeye_Quest2.apk`.

### Sideloading the APK to Meta Quest
```bash
# 1. Connect headset with USB debugging enabled
adb devices

# 2. Install the standalone package
adb install -r -g Builds/Android/DEADeye_Quest2.apk

# 3. Launch from headset:
# Navigate to App Library -> Filter (top right) -> Unknown Sources -> DEADeye
```

---

## 8. Current Status & Known Limitations

For complete transparency, all known constraints are documented:

1. **Mission Campaign Scope**:
   - Operation 01 (**DEAD AIR**) is the sole fully realized combat level.
   - Operations 02 (*Broken Signal*), 03 (*Black Horizon*), and 04 (*Silent Citadel*) exist as data ScriptableObjects and terminal UI cards. Building their 3D scenes is the natural next step for future development.

2. **Procedural Limb Animation**:
   - Hostile and friendly characters use sinusoidal joint oscillation (`DEADeyeProceduralCharacterAnimator.cs`) rather than skinned skeletal animation. While efficient (sub-56 MB APK), replacing this with a Mecanim animation controller and root motion will dramatically increase visual fidelity.

3. **Physical Ergonomic Validation**:
   - Software tracking logic (Floor mode, $1.7\text{ m}$ standing offset, direct CenterEye polling) has been mathematically validated. Physical headset testing across varying user heights and play space environments remains necessary for fine-tuning neck and wrist comfort.

---

## 9. Taking Over Development Responsibly

If you are inheriting this project, follow this recommended roadmap:

### Milestone 1: Familiarization (Day 1)
- Clone the repository and execute the 32 automated tests.
- Play through Operation DEAD AIR on desktop to understand the pacing and objective flow.
- Review [`Documentation/TECHNICAL_ARCHITECTURE.md`](./TECHNICAL_ARCHITECTURE.md) and [`Documentation/WEAPON_AND_BALLISTICS.md`](./WEAPON_AND_BALLISTICS.md).

### Milestone 2: Campaign Expansion (Weeks 1–2)
- Duplicate `RooftopMission.unity` as `BrokenSignal.unity`.
- Place a new urban layout using existing environment prefabs in `Assets/DEADeye/Art/Environments/`.
- Wire `Operation_BrokenSignal.asset` into `DEADeyeMissionDirector`.

### Milestone 3: Animation Upgrade (Weeks 3–4)
- Import humanoid FBX animation clips (walk, idle, aim, run, hit-react).
- Replace `DEADeyeProceduralCharacterAnimator.cs` with an `Animator` controller component on the Quaternius character prefabs.

### Safety Rules for Maintainers
- **Never commit `.apk` files directly to Git**. Use GitHub Releases for binaries.
- **Never delete or untrack `.meta` files**.
- **Preserve the desktop simulator**. Do not make changes that require a physical headset just to run the game in Editor.

---

## 10. Troubleshooting & Environment Disaster Recovery

### Problem 1: "Package [com.unity.modules.adaptiveperformance] cannot be found"
- **Cause**: Project was opened with an early Unity 6.0 build (e.g. `6000.0.84f1`) rather than the authoritative LTS release **`6000.3.27f2`**.
- **Fix**: Launch with Unity `6000.3.27f2`. If forced to test in 6000.0, temporarily remove `com.unity.modules.adaptiveperformance` and `com.unity.modules.vectorgraphics` from `Packages/manifest.json`.

### Problem 2: Camera Viewpoint Appears at Floor Level in VR
- **Cause**: XR Origin tracking mode reset to `Device` instead of `Floor`, or standing eye-height safeguard was bypassed.
- **Fix**: In `VR_XROriginRig`, ensure `XROrigin.RequestedTrackingOriginMode` is set to `Floor`. Verify `DEADeyePlayerController.cs` has `m_VRStandingHeightOffset = 1.7f`.

### Problem 3: Gunshot Fires but Hits Nothing / Blocked by Catwalk Railing
- **Cause**: Pass 2 muzzle raycast is striking an intermediate barrier.
- **Fix**: Inspect the muzzle socket transform on `XM500_SniperRifle.prefab`. Ensure the muzzle transform forward vector points directly down the barrel and is not colliding with the player's own CharacterController.

### Disaster Recovery: Clean Working State Reset
If the local project becomes corrupted due to failed package updates or cache issues:
```bash
# 1. Close Unity Editor completely.
# 2. Reset source tree to clean remote HEAD:
git reset --hard HEAD
git clean -fd

# 3. Delete regenerable cache directories:
# In PowerShell:
Remove-Item -Recurse -Force Library, Temp, Logs, UserSettings

# 4. Re-open in Unity 6000.3.27f2.
# Unity will regenerate Library/ cleanly in 3-4 minutes.
```
