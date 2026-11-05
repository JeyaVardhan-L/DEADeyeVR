# DEADeye VR — Tactical Urban Sniper Sandbox & Quest 2 VR Foundation

[![Unity 6.3 LTS](https://img.shields.io/badge/Unity-6000.3.27f2%20LTS-black.svg?style=flat&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP%2017.3.0%20(Forward%2B)-blue.svg)](https://unity.com/srp/Universal-Render-Pipeline)
[![XR Runtime](https://img.shields.io/badge/XR%20Runtime-Khronos%20OpenXR%201.16.1-orange.svg)](https://www.khronos.org/openxr/)
[![Target Platform](https://img.shields.io/badge/Targets-PC%20Desktop%20%7C%20Meta%20Quest%202-green.svg)](https://www.meta.com/quest/products/quest-2/)
[![Automated Tests](https://img.shields.io/badge/Automated%20Tests-51%20Passing%20(100%25)-brightgreen.svg)]()

**DEADeye** is a mission-driven tactical urban sniper game engineered in **Unity 6.3 LTS** for both **PC Desktop Simulation** (keyboard & mouse) and **Meta Quest 2 Standalone VR** (Android ARM64 / OpenXR).

Departing fundamentally from static "shooting-gallery" prototypes, DEADeye places the operative on an expansive urban rooftop perimeter connected by traversable steel catwalks and skybridges. The player undertakes **Operation DEAD AIR**, a four-phase covert military operation requiring long-range target reconnaissance, communications jammer destruction, the tactical defense of allied repair engineers, and the neutralization of a rogue commander.

---

## 📸 In-Game Screenshots

| Urban Rooftop Overview | Optical Scope Engagement |
|:---:|:---:|
| ![Urban District](Screenshots/01_UrbanDistrict_Skyline.png) | ![Optical Scope](Screenshots/07_OpticalScope_ZoomedView.png) |
| *Expansive urban district and traversable rooftop complex* | *High-precision dual-lens optical zoom with crosshair reticle* |

| Traversable Catwalks & Skybridges | 3D Tactical Terminal |
|:---:|:---:|
| ![Walkable Skybridge](Screenshots/09_WalkableBridge_EastCatwalk.png) | ![Tactical Terminal](Screenshots/MissionSelect_TacticalTerminal.png) |
| *Steel catwalk connecting overwatch perches* | *In-world terminal for mission briefings and debriefings* |

---

## 🎯 Gameplay & Narrative: Operation DEAD AIR

The campaign vertical slice is structured into four sequential tactical phases managed by an authoritative state machine (`DEADeyeMissionDirector.cs`):

1. **Phase 1: Tactical Reconnaissance**:
   The player explores the rooftop compound and crosses elevated steel skybridges to scout three designated observation perches: *North Overwatch*, *East Plaza*, and *West Construction*.
2. **Phase 2: Signal Disruption**:
   A hostile military communications jammer tower ($1,500\text{ m}$ away) is broadcasting electronic countermeasures across allied channels. The operative must locate and destroy its external power junctions to restore radio contact.
3. **Phase 3: Allied Asset Protection**:
   Allied field technicians **Elena** and **Marcus** advance across the district catwalks to repair the primary communications dish. The sniper must provide active overwatch, suppressing advancing squads of hostile patrol guards.
4. **Phase 4: High-Value Target Assassination**:
   Rogue **Commander Vance** attempts an emergency helicopter extraction from an adjacent skyscraper helipad. The sniper must identify and eliminate Vance before his extraction craft departs.

---

## 🏗️ Core Engineering Highlights

### 1. Dual-Rig Architecture (Desktop & Quest 2)
The game uses a unified scene (`Assets/DEADeye/Scenes/RooftopMission.unity`) with two mutually exclusive player rigs managed by `DEADeyePlayerController.cs`:
- **Desktop Rig (`Desktop_FPSRig`)**: CharacterController locomotion ($4.5\text{ m/s}$), zero-drift mouse look, and camera-centre optical scoping.
- **VR Rig (`VR_XROriginRig`)**: TrackedPoseDriver head tracking, direct OpenXR Touch controller input polling, right-hand weapon socketing (`VRWeaponSocket.cs`), and stereoscopic UI projection.

### 2. Dual-Pass Ballistics Pipeline
To eliminate camera-versus-barrel parallax errors and prevent bullets from shooting through nearby obstacles:
- **Pass 1 (Camera Viewport Ray)**: Projects from `Camera.main.ViewportPointToRay(Vector3(0.5, 0.5, 0))` to identify the exact point the player is aiming at.
- **Pass 2 (Muzzle Obstruction Ray)**: Projects from the physical rifle muzzle point to the target point found in Pass 1. If a low rooftop parapet or barrier blocks Pass 2, the bullet strikes that obstacle rather than tunneling through it.

### 3. Mandatory Scope Fire Gate
Unscoped hip-firing is strictly blocked at the weapon trigger level (`DEADeyeSniperRifle.cs`). Attempting to fire while unscoped plays a mechanical dry-fire hammer click (`sfx_dry_fire.wav`) without consuming ammunition or casting rays. The operative must hold RMB (desktop) or the Right Touch Grip (VR) to engage optical magnification before the rifle can discharge.

### 4. Zero-Drift Mouse Look & Boundary Recovery
- `DEADeyeMouseLook.cs` uses explicit scalar pitch and yaw accumulators, clamping vertical look strictly within $[-85^\circ, +85^\circ]$ to eliminate precision drift across long play sessions.
- An expansive physical trigger volume (`DEADeyeBoundaryTrigger.cs`) beneath the rooftops intercepts any accidental falls, safely resetting the player to the nearest spawn point without reloading the scene.

---

## 🎮 Control Schemes

The project automatically switches between Desktop and VR modes upon detecting an active OpenXR headset runtime.

### Desktop Controls (Keyboard & Mouse)
| Input | Action | Function |
|---|---|---|
| **Mouse Movement** | Aim / Camera Look | Free camera rotation with pitch clamping |
| **WASD** | Locomotion | Walk across rooftop, catwalks, and skybridges |
| **Right Mouse Button (Hold)** | Optical Scope Zoom | Engages $6.25\times$ telescopic magnification and reticle |
| **Left Mouse Button** | Fire Rifle | Fires weapon (requires active scope zoom) |
| **R** | Reload | Reloads 5-round internal magazine |
| **Y** | Tactical Terminal | Opens/closes 3D mission briefings and debriefings |
| **Escape** | Pause Menu | Toggles cursor lock and displays pause options |

### Meta Quest 2 / Pro Controls (OpenXR Touch Controllers)
| Input | Action | Function |
|---|---|---|
| **Headset 6DoF Pose** | Natural Head Look | Positional and rotational head tracking |
| **Left Thumbstick** | Smooth Locomotion | Forward, backward, and strafe traversal |
| **Right Thumbstick** | Snap / Smooth Turn | Horizontal yaw rotation |
| **Right Grip (Hold)** | Scope Telemetry | Activates optical magnification |
| **Right Index Trigger** | Fire Rifle | Discharges weapon (requires active scope zoom) |
| **Right Button A / X** | Reload | Reloads rifle magazine |
| **Left Button Y / Menu** | Tactical Terminal | Opens/closes tactical mission overview |

---

## 🛠️ Technology Stack & Requirements

| Layer | Technology | Version / Configuration |
|---|---|---|
| **Game Engine** | Unity Editor | `6000.3.27f2` (Unity 6.3 LTS) |
| **Render Pipeline** | Universal Render Pipeline (URP) | `17.3.0` — Forward+ Rendering |
| **Stereo Rendering** | Single-Pass Instanced (SPI) | Khronos OpenXR Plugin `1.16.1` |
| **Interaction Framework** | XR Interaction Toolkit (XRI) | `3.4.0` |
| **Input System** | Unity Input System | `1.16.0` (Action-based router + direct polling) |
| **Navigation** | Unity AI Navigation | `2.0.10` (Baked NavMesh surface) |
| **Target Build Platform** | Android ARM64 | Target API Level 32 (Android 12L / Quest OS) |
| **Texture Compression** | ASTC | $6\times6$ / $8\times8$ block compression |
| **Scripting Backend** | IL2CPP | C++ Ahead-Of-Time native compilation |

---

## 🚀 Setup & Execution Guide

### 1. Opening the Authoritative Project
1. Install **Unity Hub** and install **Unity 6.3 LTS (`6000.3.27f2`)** with **Android Build Support** (including Android SDK & NDK Tools and OpenJDK).
2. Add the repository directory to Unity Hub:
   ```
   C:\Unity\Editors\DEADeyeVR_Foundation
   ```
3. Open the project and ensure the primary scene is active:
   `Assets/DEADeye/Scenes/RooftopMission.unity`

### 2. Running the Desktop Simulator
1. In the Unity Editor, press the **Play** button.
2. Click inside the Game view to lock the cursor.
3. Use `WASD` to navigate the rooftop catwalks, hold `RMB` to scope in, and click `LMB` to fire. Press `Y` to inspect the tactical terminal.

### 3. Building the Standalone Quest 2 APK
Execute the headless batchmode build command from PowerShell:
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" `
  -projectPath "C:\Unity\Editors\DEADeyeVR_Foundation" `
  -executeMethod DEADeyeAndroidBuilder.BuildQuest2APK `
  -batchmode -nographics -quit `
  -logFile "Builds\Android\quest2_build.log"
```
The compiled, signed release package is output to:
`Builds/Android/DEADeye_Quest2.apk` (~55.5 MB)

### 4. Installing to Meta Quest 2 via ADB
Connect your Meta Quest 2 via USB with **Developer Mode** enabled:
```powershell
# 1. Verify connected device
adb devices

# 2. Sideload and install APK
adb install -r "Builds\Android\DEADeye_Quest2.apk"

# 3. Stream live Unity engine logs
adb logcat -s Unity ActivityManager PackageManager
```
Put on the headset, navigate to **App Library $\rightarrow$ Unknown Sources**, and launch **DEADeye**.

---

## 🧪 Automated Testing & QA

The project maintains **51 automated tests** across EditMode and PlayMode running under the Unity Test Framework and NUnit:

- **EditMode Tests (27/27 Passing)**: Validates weapon reload timing, magazine capacities, ScriptableObject definitions, audio clip assignments, and camera clipping planes.
- **PlayMode Tests (24/24 Passing)**: Validates sniper accuracy at $50\text{ m}$, headshot damage multipliers ($3\times$), wall obstruction collision, scope fire gates, CharacterController bridge traversal, and mission state transitions.

Run the test suite headlessly:
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" `
  -projectPath "C:\Unity\Editors\DEADeyeVR_Foundation" `
  -runTests -testPlatform EditMode `
  -testResults "Builds\Logs\EditMode_Results.xml" `
  -batchmode -nographics -quit
```

---

## ⚖️ Known Limitations & Honest Scope

In keeping with academic and professional engineering integrity:

1. **Campaign Scope**: **Operation DEAD AIR** is the complete, fully playable vertical slice. Operations 02 (*Broken Signal*), 03 (*Black Horizon*), and 04 (*Silent Citadel*) exist as data-driven preview cards in the terminal for future expansion.
2. **Hardware Validation vs. Build Verification**: The standalone Quest 2 APK (`DEADeye_Quest2.apk`) compiles with 0 errors and passes all automated input/render tests. However, final ergonomic comfort (extended neck fatigue, physical controller alignment) requires on-headset user testing.
3. **Character Animation**: Hostile and friendly characters use `DEADeyeProceduralCharacterAnimator.cs` (trigonometric joint oscillation) to maintain a zero-dollar budget and sub-56 MB APK size, rather than heavy motion-capture animation clip libraries.
4. **Ballistics**: Sniping uses instantaneous hitscan raycasting with obstruction detection. Bullet flight time, Coriolis drift, and windage curves are omitted as tactical distances remain under $300\text{ m}$.

For a complete breakdown of boundaries, trade-offs, and the multi-phase engineering roadmap, see [`Documentation/08_PROJECT_LIMITATIONS_AND_ROADMAP.md`](Documentation/08_PROJECT_LIMITATIONS_AND_ROADMAP.md).

---

## 📚 Complete Technical Documentation Index

The repository contains an exhaustive 10-part engineering handbook suite in the `Documentation/` directory:

1. [**`00_MASTER_PROJECT_HANDBOOK.md`**](Documentation/00_MASTER_PROJECT_HANDBOOK.md) — Master operational manual, architecture diagrams, and stakeholder reading tracks.
2. [**`PROJECT_SOURCE_INVENTORY.md`**](Documentation/PROJECT_SOURCE_INVENTORY.md) — Line-by-line manifest of all 58 C# scripts, 3D models, audio assets, and packages.
3. [**`01_TECH_STACK_AND_ARCHITECTURE.md`**](Documentation/01_TECH_STACK_AND_ARCHITECTURE.md) — Deep technical rationale for Unity 6.3, URP, OpenXR, and architecture Q&A.
4. [**`02_COMPLETE_CODEBASE_REFERENCE.md`**](Documentation/02_COMPLETE_CODEBASE_REFERENCE.md) — Exhaustive script-by-script API reference across all subsystems.
5. [**`03_GAMEPLAY_AND_AI_SYSTEMS.md`**](Documentation/03_GAMEPLAY_AND_AI_SYSTEMS.md) — Ballistics pipeline, optical scope rendering, AI FSM, and bridge traversal math.
6. [**`04_VR_AND_QUEST2_ENGINEERING.md`**](Documentation/04_VR_AND_QUEST2_ENGINEERING.md) — Khronos OpenXR architecture, Touch controller bindings, and stereoscopic UI.
7. [**`05_TESTING_AND_BUILD_GUIDE.md`**](Documentation/05_TESTING_AND_BUILD_GUIDE.md) — Complete 51-test inventory, batchmode automation, and ADB diagnostics.
8. [**`06_VIVA_QUESTION_BANK.md`**](Documentation/06_VIVA_QUESTION_BANK.md) — 100+ viva defense questions with 4-part answers for faculty presentation.
9. [**`07_TECHNICAL_GLOSSARY.md`**](Documentation/07_TECHNICAL_GLOSSARY.md) — Alphabetical A–Z lexicon explaining 60+ terms across 3 pedagogical layers.
10. [**`08_PROJECT_LIMITATIONS_AND_ROADMAP.md`**](Documentation/08_PROJECT_LIMITATIONS_AND_ROADMAP.md) — Engineering critique, boundary disclosure, and Phase 2/3 roadmap.
11. [**`GITHUB_PUBLICATION_AUDIT.md`**](Documentation/GITHUB_PUBLICATION_AUDIT.md) — Pre-publication audit verifying zero secrets, 100 MB file limits, and repo health.
12. [**`GITHUB_PUBLICATION_REPORT.md`**](Documentation/GITHUB_PUBLICATION_REPORT.md) — Final synchronization and publication verification report.

---

## 📄 License & Attribution

- **Source Code**: Custom gameplay, AI, ballistics, and mission systems authored under the MIT License.
- **3D Character Assets**: [Quaternius](https://quaternius.com/) (Universal Character Pack, CC0 Public Domain).
- **3D Weapon Assets**: LowPolyGuns (Standard Permissive License).
- **Environment Props**: Synty / Low Poly Dojo (Authorized for educational & non-commercial tactical simulation).
