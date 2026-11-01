# DEADeye VR — Urban Rooftop Sniper VR

**DEADeye VR** is a vertical-slice tactical VR sniper game built on **Unity 6.3 LTS (`6000.3.27f2`)** targeting **Meta Quest 2 / 3 / Pro** with standalone Android APK support and seamless desktop simulator fallback.

---

## 🎯 Game Overview

Players take the role of an elite marksman stationed on a high-altitude urban rooftop (elevation: 26 meters). Looking out over a bustling city district, the operative must identify, track, and neutralize hostile criminal syndicates and rogue entities before they reach extraction zones.

### Core Experience
- **Dojo Tactical Lobby**: Inspect your tactical sniper rifle, review mission briefings on interactive HUD boards, select difficulty ratings ([RECRUIT], [OPERATIVE], [ELITE]), and observe your standing operative avatar.
- **Rooftop Sniper Perch**: Positioned 26 meters above urban city streets with safety boundary collision preventing falls.
- **Dual Optical Scope System**: Render-texture circular ocular lens offering 4x and 8x zoom magnification with crisp crosshair reticle and peripheral awareness.
- **Dynamic Hostile AI**: Criminal syndicates and monstrous entities navigating baked NavMesh urban street networks using a multi-state finite state machine (*Patrol*, *Suspicious*, *Alerted*, *Escape*, *Eliminated*).
- **Audio Waveform Synthesis**: Procedural, zero-royalty gunshot, bolt-cycling, reload, hit chime, alert sound, and ambient city audio synthesizers.
- **Mission Director & Telemetry**: 120-second mission clock, score multiplier tracking, headshot detection (3x score bonus), accuracy rating, and debriefing results screen.

---

## 🎮 Controls & Interaction

DEADeye VR features **strict mutual exclusion** between Desktop Rig and VR XR Origin. The system automatically detects whether a VR HMD is connected and initializes the appropriate rig.

### Desktop Simulator Mode
| Action | Input Binding |
| :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` keys |
| **Look** | Mouse Delta |
| **Aim / Optic Scope** | Right Mouse Button (`RMB`) |
| **Fire Rifle** | Left Mouse Button (`LMB`) (Suppressed over UI buttons) |
| **Reload Rifle** | `R` key |
| **Sprint** | `Left Shift` |

### Meta Quest / PCVR Mode
| Action | VR Controller Binding |
| :--- | :--- |
| **Locomotion** | Left Controller Thumbstick |
| **Snap / Smooth Turn** | Right Controller Thumbstick |
| **Grab Weapon (Primary)** | Right Hand Grip Button |
| **Stabilize (Secondary Foregrip)** | Left Hand Grip Button (near rifle barrel) |
| **Fire Weapon** | Right Hand Primary Index Trigger |
| **Interact with Menus** | Controller Ray Pointer + Index Trigger |

---

## 🏗️ Project Architecture & Scenes

All production code is organized cleanly within `Assets/DEADeye/` adhering to single-responsibility architectural boundaries:

```
Assets/DEADeye/
├── Art/
│   ├── Environments/   # Dojo lobby & urban materials
│   ├── Materials/      # Tactical PBR materials
│   ├── Shaders/        # Scope reticle & holographic shaders
│   ├── Textures/       # High-resolution PBR textures
│   ├── VFX/            # Muzzle flash, bullet sparks, impact VFX
│   └── Weapons/        # Keith Thomson Sniper Rifle model & meshes
├── Editor/             # Automated scene builders & Android APK pipeline
├── Scenes/             # Authoritative project scenes
│   ├── BootScene.unity         # Scene 0: Core singletons & bootloader
│   ├── LobbyScene.unity        # Scene 1: Dojo lobby, operative, briefing
│   └── RooftopMission.unity    # Scene 2: 26m urban perch, AI, optics, HUD
├── Scripts/
│   ├── AI/             # Hostile FSM, navigation, escape zones, markers
│   ├── Core/           # GameManager, Audio, Score, Weapon, Input Router
│   ├── Mission/        # Mission Director, objectives, difficulty settings
│   ├── Player/         # Desktop FPS Rig, VR XR Origin, safety boundary
│   ├── UI/             # Lobby Briefing UI, Mission HUD, Results Debriefing
│   └── Weapons/        # Ballistic sniper rifle, 4x/8x optic camera
└── Tests/
    ├── EditMode/       # 10 integration tests validating core systems
    └── PlayMode/       # 5 live gameplay tests & screenshot generators
```

### Authoritative Build Settings (`EditorBuildSettings.asset`)
1. `Assets/DEADeye/Scenes/BootScene.unity` (Build Index 0)
2. `Assets/DEADeye/Scenes/LobbyScene.unity` (Build Index 1)
3. `Assets/DEADeye/Scenes/RooftopMission.unity` (Build Index 2)

---

## 🚀 Getting Started

### Prerequisites
- **Unity Version**: `6000.3.27f2` (Unity 6.3 LTS)
- **Render Pipeline**: Universal Render Pipeline (URP 17.3.0)
- **XR Stack**: Unity XR Interaction Toolkit 3.4.0, OpenXR Plugin 1.16.1, Unity Input System 1.16.0

### Running in the Unity Editor
1. Launch Unity Hub and add `C:\Unity\Editors\DEADeyeVR_Foundation`.
2. Open the project in Unity `6000.3.27f2`.
3. In the Project window, navigate to `Assets/DEADeye/Scenes/BootScene.unity` and double-click to open.
4. Press **Play**. The bootloader will initialize all authoritative managers and transition smoothly into `LobbyScene`.
5. Click **[DEPLOY TO ROOFTOP]** or select your desired difficulty to engage the urban rooftop mission.

---

## 🧪 Automated Testing

DEADeye VR includes an automated test suite achieving **100% test pass rate (15/15 tests)**:
- **EditMode Tests (10/10 Passed)**: Validates scene presence, singleton integrity, mutual rig exclusion, weapon firing guards, hostile FSM state transitions, headshot multipliers (3x), score calculation, and difficulty speed scaling.
- **PlayMode Tests (5/5 Passed)**: Validates full live gameplay lifecycle: Boot routing -> Lobby difficulty selection -> Rooftop perch initialization -> Hostile target elimination -> Mission results debriefing.

To execute tests via Unity Test Runner:
1. Open `Window > General > Test Runner`.
2. Select **EditMode** or **PlayMode** tab.
3. Click **Run All**.

---

## 📦 Meta Quest Android APK Build

The project features a one-click automated build pipeline generating a standalone Meta Quest APK:
- **Menu Option**: `DEADeye > Build > Build Android Quest APK`
- **Output Artifact**: `Builds/Android/DEADeyeVR_Release.apk`
- **Scripting Backend**: IL2CPP
- **Architecture**: ARM64 (64-bit)
- **Minimum SDK**: Android 10 (API Level 29)
- **SHA-256 Checksum**: `65038FFE79812934C7C75E4F115FCBDCAE240CD96F834FC460BD4B0428B0D310`

---

## 📄 License & Attribution
All engine components, assets, and procedural scripts comply with the [Unity Companion License](ASSET_LICENSES.md) and open-source CC-BY licenses. See [ASSET_LICENSES.md](ASSET_LICENSES.md) for full licensing details.
