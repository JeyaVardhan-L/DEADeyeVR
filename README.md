# DEADeye — Urban Sniper Experience

**DEADeye** is a streamlined, high-intensity first-person urban sniper game built in **Unity 6.3 LTS (`6000.3.27f2`)** for **Meta Quest 2 / 3 / Pro** (standalone Android ARM64) and PC Desktop (keyboard & mouse simulator).

Engineered following the **Final Scope Reduction Directive**, DEADeye eliminates all unnecessary lobby screens, avatar previews, and walking locomotion in favor of an immediate, pure sniper experience inspired by *Call of Duty Mobile*.

---

## 🎯 Gameplay Experience

When launched, the player is placed **immediately and directly** into the stationary rooftop sniper nest overlooking an expansive urban city district:

- **Stationary Rooftop Perch**: Positioned securely at 26 meters altitude at `(0, 26, -2.5)`. The player cannot walk off the roof, clip through floors, or fall into void geometry.
- **Immediate Weapon Presentation**: The XM500 Precision Sniper Rifle is automatically mounted in the foreground within the player's primary field of view.
- **Optical Scope System**: Secondary aim (RMB or VR hand grip) activates real-time optical magnification with high-precision crosshair reticle, aiming pip, and peripheral vignetting.
- **Call of Duty Mobile Inspired HUD**:
  - **Top Telemetry Bar**: Live mission countdown (`TIME 02:00`), live score (`SCORE 0000`), remaining targets (`HOSTILES 8`), and escaped enemies (`ESCAPES 0/3`).
  - **Bottom Ammunition Box**: Clean tactical magazine readout (`AMMO: 5 / 5`) with `[R] RELOAD` prompt.
  - **Dynamic Hit Feedback**: Real-time hit confirmation banner with tactical hitmarker cross, awarding **`+100 BODY HIT`** or **`+300 PRECISION HEADSHOT`**.
  - **Tactical Pause Menu**: Pressing `Escape` opens a menu with **[RESUME MISSION]**, **[RESTART MISSION]**, and **[QUIT GAME]**.
- **Urban Hostile AI**: Criminal syndicates and monstrous hostiles navigate street corridors and crosswalks along NavMesh paths toward extraction checkpoints. Hostiles feature procedural walking strides, arm swings, torso bobbing, overhead red triangular tracking indicators, and knockdown elimination reactions.
- **Mission Evaluation & Debriefing**: Upon timer expiration, hostile quota elimination, or mission failure (3 hostiles escaped), an interactive Results Screen displays final score, eliminations, escapes, and accuracy percentage with **[RESTART MISSION]** and **[QUIT GAME]** actions.

---

## 🎮 Controls

The game features **strict mutual exclusion** between Desktop FPS mode and Meta Quest VR mode. The engine automatically detects whether an active VR HMD is connected.

### Desktop Controls (Keyboard & Mouse)
| Action | Binding | Notes |
| :--- | :--- | :--- |
| **Look / Aim** | `Mouse Delta` | Clamped pitch (-80° to +80°) and free yaw |
| **Optic Scope Zoom** | `Right Mouse Button (RMB)` | Toggles magnified scope view & reticle overlay |
| **Fire Rifle** | `Left Mouse Button (LMB)` | Discharges weapon (0.85s bolt-action cooldown); suppressed over UI |
| **Reload** | `R` key | Instantly reloads 5-round magazine |
| **Pause / Menu** | `Escape` key | Toggles tactical pause interface |

### Meta Quest 2 / 3 / Pro (VR Mode)
| Action | VR Controller Binding | Notes |
| :--- | :--- | :--- |
| **Aim Weapon** | Right Hand Controller (6DOF) | Direct physical controller aiming |
| **Stabilize (Foregrip)** | Left Hand Grip Button | Dual-handed aim stabilization |
| **Optic Scope** | Raise rifle ocular to eye | Automatic proximity optic engagement |
| **Fire Weapon** | Right Index Trigger | Discharges rifle with haptic pulse |
| **UI Interaction** | Controller Ray Pointer + Trigger | Selects pause / results buttons |

---

## 🏗️ Architecture & Sole Build Scene

The project has been consolidated into **one authoritative gameplay scene**:

- **`Assets/DEADeye/Scenes/RooftopMission.unity`** (Build Index 0)

All game singletons (`DEADeyeGameManager`, `DEADeyeMissionDirector`, `DEADeyeScoreManager`, `DEADeyeEnemyRegistry`, `DEADeyeWeaponManager`, `DEADeyeAudioManager`, `DEADeyeInputRouter`) are instantiated directly within `RooftopMission.unity` on the `Mission_AuthoritativeManagers` root, guaranteeing instantaneous zero-delay startup without intermediate boot or lobby transitions.

```
Assets/DEADeye/
├── Art/
│   ├── Environments/           # PBR city and building textures/materials
│   ├── Weapons/                # XM500 sniper rifle 3D model & materials
│   └── VFX/                    # Muzzle flashes, sparks, impact effects
├── Editor/
│   ├── DEADeyeAndroidBuilder.cs       # Automated Meta Quest APK builder
│   └── DEADeyeMasterSceneBuilder.cs   # Programmatic single-scene builder
├── Scenes/
│   ├── RooftopMission.unity           # Sole authoritative build scene (Index 0)
│   └── RooftopMission/                # Baked persistent NavMeshData asset
├── Scripts/
│   ├── AI/                     # Hostile FSM, procedural animation, indicators
│   ├── Core/                   # Game, Score, Weapon, Audio, and Input managers
│   ├── Environment/            # Procedural city streets, buildings, nests
│   ├── Mission/                # 120s timer director, wave spawns, objectives
│   ├── Player/                 # Stationary desktop rig, XR Origin, boundaries
│   ├── UI/                     # COD Mobile style HUD, pause menu, results UI
│   └── Weapons/                # Ballistic sniper rifle, scope optic camera
└── Tests/
    ├── EditMode/               # 18 unit & integration tests (100% pass)
    └── PlayMode/               # 5 live gameplay tests & screenshot captures
```

---

## 🧪 Verification & Test Results

The codebase is validated by automated batchmode test suites in Unity 6.3 LTS:

- **EditMode Tests**: **18 / 18 PASSED (100%)** (`TestResults_EditMode.xml`)
- **PlayMode Tests**: **5 / 5 PASSED (100%)** (`TestResults_PlayMode.xml`)
- **Combined Automated Pass Rate**: **23 / 23 (100%)**

### PlayMode Gameplay Checks Verified:
1. Game opens directly into rooftop sniper position.
2. Correct first-person view with rifle visible in foreground.
3. Player remains securely positioned at 26m elevation without falling or clipping.
4. City is visible below with roadways, sidewalks, and buildings.
5. Hostile targets spawn directly onto street NavMesh coordinates.
6. Enemies visibly move and patrol defined urban routes.
7. Overhead red triangular indicators follow each hostile.
8. Scope activates upon right-click with magnified FOV.
9. Scope reticle and optical alignment match target point.
10. Weapon fires on left-click with muzzle flash and tracer.
11. Ammunition decreases from 5 to 4 and updates HUD.
12. Reload restores full 5-round magazine.
13. Precision headshots register accurately, awarding 300 points.
14. Buildings and architectural obstacles block ballistic rays.
15. Eliminated enemies play knockdown reactions and are removed.
16. Hostile escapes are tracked accurately against the 3-escape limit.
17. Mission concludes on victory (quota eliminated) or defeat.
18. Results screen displays comprehensive debriefing statistics.
19. [RESTART MISSION] button reloads the scene with fresh state.

---

## 📦 Meta Quest Release Build

The production standalone Android APK is built via IL2CPP ARM64:

- **APK File**: [`Builds/Android/DEADeye_Release.apk`](file:///C:/Unity/Editors/DEADeyeVR_Foundation/Builds/Android/DEADeye_Release.apk)
- **Size**: **56,613,215 bytes** (~54.0 MB)
- **SHA-256 Checksum**: `A4723212A849D2981095D8A654DBE82F3429C34A7B255F89A7EFDD33090C2BCC`
- **Target OS**: Android API Level 29+ (Meta Quest 2, Meta Quest 3, Meta Quest Pro)
- **Scripting Backend**: IL2CPP / ARM64 / OpenXR URP
