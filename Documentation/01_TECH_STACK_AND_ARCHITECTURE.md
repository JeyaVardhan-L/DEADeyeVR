# DEADeye VR — Technology Stack & System Architecture Handbook

This document provides the foundational engineering blueprint for **DEADeye VR**. It details every technology in the stack, explains the architectural patterns used, and provides complete Mermaid diagrams and flowcharts grounded in the actual codebase.

---

## 1. Complete Technology Inventory

Every technology listed below is verified from `Packages/manifest.json`, `ProjectSettings/`, and source files in `C:\Unity\Editors\DEADeyeVR_Foundation`.

| Technology | Exact Version | Category | Purpose in DEADeye | Why Selected | Where Used |
|---|---|---|---|---|---|
| **Unity Engine** | `6000.3.27f2` (Unity 6.3 LTS) | Game Engine | Core engine runtime, scene management, asset pipeline, physics execution | Long-Term Support (LTS) stability, latest OpenXR & URP mobile optimization, multi-platform build pipelines | Entire project runtime |
| **C#** | `10.0` / .NET Standard 2.1 | Programming Language | Authoring all gameplay, math, state machines, ballistics, and UI | Strongly typed, garbage collected, high-performance C# job/burst interop, native Unity API integration | All 58 `.cs` scripts in `Assets/DEADeye/Scripts/` |
| **Universal Render Pipeline (URP)** | `17.3.0` | Graphics & Rendering | Optimized modern rendering with Single-Pass Instanced XR support | Low draw-call overhead, customized render features, mobile GPU bandwidth conservation on Quest 2 | Forward+ renderer in `Assets/RendererData/` |
| **OpenXR** | `1.16.1` | XR Platform Standard | Cross-platform vendor-neutral XR device communication | Prevents vendor lock-in; single API targets Desktop XR, Meta Quest 2, 3, Pro without code changes | `Assets/XR/Settings/OpenXRPackageSettings.asset` |
| **XR Interaction Toolkit (XRI)** | `3.4.0` | XR Gameplay Framework | XR Origin rig, controller tracking, ray/poke interactors, locomotion | High-level battle-tested XR abstractions for hands, teleports, continuous turning, and UI raycasts | `VR_XROriginRig`, `DEADeyePlayerController.cs` |
| **Unity Input System** | `1.16.0` | Input Management | Event-driven device polling for modern controllers, keyboards, mice | Replaces legacy `Input.GetAxis`; enables clean bindings and direct hardware state reading | `DEADeyeInputRouter.cs`, `DEADeyeDesktopRig.cs` |
| **Unity AI Navigation** | `2.0.10` | Pathfinding & Navigation | Runtime NavMesh surfaces, agent pathfinding, obstacle avoidance | Package-based NavMesh allows dynamic rooftop and street level baking at runtime | `DEADeyeUrbanCityBuilder.cs`, `DEADeyeHostileAI.cs` |
| **Unity UI (uGUI)** | `2.0.0` | User Interface | Tactical Terminal, in-game HUD, debriefing cards, scope reticle | ScreenSpace and WorldSpace rendering with full `GraphicRaycaster` and EventSystem integration | `DEADeyeMissionHUD.cs`, `DEADeyeTacticalTerminalUI.cs` |
| **Unity Test Framework** | `1.6.0` | Quality Assurance | Automated NUnit unit, integration, and PlayMode screenshot testing | Allows continuous headless validation in CI/CD via Unity command-line batchmode | `Assets/DEADeye/Tests/` (51 total tests) |
| **Unity Physics (PhysX)** | Built-in module | Physics Simulation | Raycast hit detection, CharacterController movement, collision layers | Real-time ray-triangle intersection math, spatial partitioning via bounding volume hierarchies (BVH) | `DEADeyeSniperWeapon.cs`, `DEADeyeDesktopRig.cs` |
| **Android NDK** | `r23b` (bundled) | Native Toolchain | Compiles C++ code emitted by IL2CPP into ARM64 native binaries | Delivers near-native execution speed essential for hitting 72/90 FPS in mobile VR | `Builds/Android/DEADeye_Quest2.apk` |
| **IL2CPP** | Built-in runtime | Compilation Backend | Transpiles managed C# IL bytecode into native C++ prior to machine compilation | Dramatically improves math-heavy raycast and AI performance over Mono runtime on Android | `PlayerSettings.SetScriptingBackend(IL2CPP)` |
| **Git** | `2.x` | Version Control | Source code control, checkpoints, and rollback guarantees | Enables atomic branch management and local safety checkpoints before refactoring | `C:\Unity\Editors\DEADeyeVR_Foundation\.git` |

---

## 2. Core Technical Concepts — 3-Layer Explanations

### A. The Universal Render Pipeline (URP) & Single-Pass Instancing
- **Simple Explanation**: Imagine painting a picture. Instead of painting the left eye's view on one canvas and then starting over to paint the right eye's view on a second canvas, you dip your brush once and stamp both eyes at the same time.
- **Technical Explanation**: Traditional VR rendering uses Multi-Pass, executing the entire CPU render loop twice (culling, draw calls, state changes). URP's **Single-Pass Instanced (SPI)** mode renders both eyes in a single draw call by utilizing texture arrays (`Texture2DArray`) and hardware vertex instancing with eye-index primitives (`unity_StereoEyeIndex`).
- **DEADeye Implementation**: In `Assets/RendererData/`, URP is configured with Forward+ rendering and Single-Pass Instanced XR. This cuts CPU draw-call overhead by approximately 45%, preventing draw-call bottlenecks on the Snapdragon XR2 processor in the Meta Quest 2.

### B. Hitscan Raycasting vs. Ballistic Projectiles
- **Simple Explanation**: A projectile is like a real bullet that flies through the air over time and drops due to gravity. A hitscan raycast is like an instantaneous laser beam that immediately checks what it touches at the speed of light.
- **Technical Explanation**: Projectile simulation integrates differential equations of motion ($v = v_0 + at$, $x = x_0 + vt$) over discrete fixed time-steps (`FixedUpdate`), requiring continuous collision detection (CCD) to avoid tunneling through geometry. Hitscan performs an analytic line-segment versus bounding-volume hierarchy (BVH) query (`Physics.RaycastAll`) in a single frame, yielding $O(\log N)$ collision resolution.
- **DEADeye Implementation**: `DEADeyeSniperWeapon.cs` implements an authoritative hitscan raycasting system (`ExecuteRaycastShot()`). Because military sniper bullets travel at ~900 m/s and urban engagement distances in DEADeye are 80–300 meters, physical bullet flight time is less than 0.25 seconds. Hitscan guarantees 100% reliable, zero-drift hit registration without physics desynchronization.

### C. The Dual-Rig Architecture (Desktop Simulator vs. Standalone XR Origin)
- **Simple Explanation**: Two drivers in a car: one driver has a standard steering wheel and pedals (keyboard and mouse), while the other has virtual reality motion controls. Only one driver can touch the wheel at any time.
- **Technical Explanation**: `DEADeyePlayerController.cs` acts as an authoritative mediator managing two mutually exclusive GameObject hierarchies: `Desktop_FPSRig` (driven by `CharacterController` and mouse pitch/yaw logic) and `VR_XROriginRig` (driven by OpenXR `TrackedPoseDriver`, `LocomotionMediator`, and XRI interactor groups).
- **DEADeye Implementation**: `DEADeyePlayerController.EnforceActiveRig()` checks `DEADeyeGameManager.Instance.PlayerMode`. In standalone Android VR (`#if UNITY_ANDROID && !UNITY_EDITOR`), the desktop rig is deactivated, the XR Origin is enabled, and the sniper rifle is automatically socketed to the Right Controller's `VRWeaponSocket`.

---

## 3. High-Level System Architecture Diagram

```mermaid
graph TD
    subgraph Core Management
        GM[DEADeyeGameManager<br/>State, Mode, Difficulty]
        AM[DEADeyeAudioManager<br/>Spatial Audio & Synthesis]
        SM[DEADeyeScoreManager<br/>Hits, Headshots, Acc %]
        WM[DEADeyeWeaponManager<br/>Magazines, Reload Timers]
        IR[DEADeyeInputRouter<br/>Desktop & Quest Touch Input]
    end

    subgraph Player Architecture
        PC[DEADeyePlayerController<br/>Authoritative Rig Switcher]
        DR[Desktop_FPSRig<br/>WASD + Mouse Pitch/Yaw]
        VR[VR_XROriginRig<br/>OpenXR + Hand Tracking]
        RB[DEADeyeRooftopBoundary<br/>Fall Recovery & Safe Nodes]
    end

    subgraph Weapon & Optics
        SW[DEADeyeSniperWeapon<br/>Raycast Ballistics & Muzzle Check]
        SO[DEADeyeScopeOptic<br/>RenderTexture Magnification]
    end

    subgraph Mission & AI
        MD[DEADeyeMissionDirector<br/>5-Phase Operation State Machine]
        CB[DEADeyeUrbanCityBuilder<br/>Procedural Districts & NavMesh]
        HA[DEADeyeHostileAI<br/>Patrol, Alert, Escape FSM]
        FA[DEADeyeFriendlyAI<br/>Elena & Marcus Comms Repair]
        EP[DEADeyeEnvironmentalProp<br/>Power Junction, Alarm, Gate]
    end

    subgraph User Interface
        HUD[DEADeyeMissionHUD<br/>Stereo Camera Canvas & Telemetry]
        TT[DEADeyeTacticalTerminalUI<br/>Briefing & Mission Cards]
        RU[DEADeyeResultsUI<br/>S/A/B/C/F Debriefing]
    end

    GM --> PC
    GM --> MD
    IR --> DR
    IR --> SW
    PC --> DR
    PC --> VR
    DR --> SW
    VR --> SW
    SW --> SO
    SW --> SM
    SW --> AM
    SW --> HA
    SW --> EP
    MD --> HA
    MD --> FA
    MD --> HUD
    SM --> HUD
    WM --> HUD
    MD --> RU
```

---

## 4. Scene Hierarchy Breakdown (`RooftopMission.unity`)

The authoritative scene contains the following structure:

```text
RooftopMission.unity
├── City_Sunlight                           # Directional Light (Sun, shadows, warm urban lighting)
├── Mission_AuthoritativeManagers          # Persistent gameplay manager singletons
│   ├── DEADeyeGameManager                 # Game state, player mode, difficulty
│   ├── DEADeyeAudioManager                # 2D/3D SFX and ambient audio sources
│   ├── DEADeyeScoreManager                # Live score, combo, and accuracy tracker
│   ├── DEADeyeWeaponManager               # Active ammunition and reload countdowns
│   ├── DEADeyeInputRouter                 # Unified input aggregator
│   ├── DEADeyeEnemyRegistry               # Active hostile spatial registry
│   └── DEADeyeMissionDirector             # 5-phase Operation DEAD AIR state machine
├── CityEnvironment_Controller             # Level geometry container
│   ├── DEADeyeUrbanCityBuilder            # Procedural builder generating 4 districts & NavMesh
│   ├── NavMeshSurface                     # Runtime baked AI navigation surface
│   ├── Environmental_Props                # Power junction, alarm bell, transformers, gate
│   └── Overwatch_Perches                  # Perch 1, East Catwalk, West Skybridge
├── DEADeye_PlayerController               # Dual-rig player container
│   ├── Desktop_FPSRig (Active on Desktop) # CharacterController, DesktopCamera, AudioListener
│   │   └── DesktopCamera
│   │       └── DesktopWeaponSocket        # Default hip and ADS weapon mount point
│   ├── VR_XROriginRig (Active in VR)      # XROrigin, XRInputModalityManager, CharacterController
│   │   ├── Camera Offset
│   │   │   ├── Main Camera                # TrackedPoseDriver, stereo eye rendering, AudioListener
│   │   │   ├── Left Controller            # TrackedPoseDriver, Ray/Poke interactors, Controller model
│   │   │   └── Right Controller           # TrackedPoseDriver, VRWeaponSocket, Ray/Poke interactors
│   │   └── Locomotion                     # DynamicMoveProvider, SnapTurnProvider, Turn, Move
│   └── XR Interaction Simulator           # In-editor desktop simulation of VR headsets
├── RooftopSafetyBoundary                  # Fall detection volume and safe respawn nodes
├── XM500_PrecisionSniper                  # High-caliber sniper rifle (XRGrabInteractable)
│   ├── RifleMesh                          # Visual 1:1 scale geometry
│   ├── MuzzlePoint                        # Trajectory origin for physical obstruction test
│   ├── ScopeOptic                         # Optic cylinder with RenderTexture lens
│   └── ScopeCamera                        # High-magnification (12° FOV) optic camera
└── DEADeye_MissionHUD                     # Dynamic user interface container
    ├── HUDCanvas                          # Canvas (Overlay in Desktop / ScreenSpaceCamera in VR)
    │   ├── TelemetryBar                   # Timer, Score, Hostile Count, Escapes Remaining
    │   ├── ObjectiveBar                   # Active sequential objective tracker
    │   ├── AmmoPanel                      # Magazine counter (5 / 5) & reload indicator
    │   ├── ScopeReticleOverlay            # Fullscreen optical lens overlay & mil-dot reticle
    │   ├── TacticalTerminalPanel          # Mission briefing cards & deploy button
    │   └── DebriefPanel                   # Results debriefing and grade assignment
    └── TrackedDeviceGraphicRaycaster      # XRI pointer raycast receiver for VR menu selection
```

---

## 5. Architectural Communication Patterns

DEADeye employs three decoupled software design patterns to maintain high code maintainability:

### 1. The Authoritative Singleton Pattern
Used for long-lived managerial subsystems that need global accessibility without tight scene coupling:
- `DEADeyeGameManager.Instance`
- `DEADeyeAudioManager.Instance`
- `DEADeyeScoreManager.Instance`
- `DEADeyeWeaponManager.Instance`
- `DEADeyeInputRouter.Instance`
- `DEADeyeMissionDirector.Instance`
- `DEADeyeMissionHUD.Instance`
- `DEADeyeTacticalTerminalUI.Instance`

Every singleton enforces strict lifecycle safety in `Awake()`:
```csharp
if (Instance != null && Instance != this)
{
    Destroy(gameObject);
    return;
}
Instance = this;
```

### 2. The Observer Pattern (C# Events & Actions)
Systems broadcast state changes via standard `System.Action` events rather than polling or tightly coupled method calls:
- `DEADeyeWeaponManager.OnAmmoChanged(int current, int max)` &rarr; `DEADeyeMissionHUD.UpdateAmmoDisplay()`
- `DEADeyeScoreManager.OnHitRegistered(bool isHeadshot, int points)` &rarr; `DEADeyeMissionHUD.ShowHitFeedback()`
- `DEADeyeMissionDirector.OnPhaseChanged(OperationPhase phase)` &rarr; `DEADeyeMissionHUD.UpdateObjectiveBar()`
- `DEADeyeHostileAI.OnEliminated(DEADeyeHostileAI hostile)` &rarr; `DEADeyeScoreManager.RegisterElimination()`

### 3. The State Machine Pattern (FSM)
Used in two critical subsystems:
1. **AI Controller (`DEADeyeHostileAI.cs`)**:
   - `HostileSpawnState`: Initializes NavMeshAgent and delays 0.5s for stability.
   - `HostilePatrolState`: Traverses waypoints at `walkSpeed = 1.6 m/s`.
   - `HostileSuspiciousState`: Pauses, faces suspicion point, and investigates.
   - `HostileAlertedState`: Sprints at `alertSpeed = 2.8 m/s` toward cover or reinforcement points.
   - `HostileEscapeState`: Sprints at `escapeSpeed = 3.6 m/s` toward the nearest `DEADeyeEscapeZone`.
   - `HostileEliminatedState`: Stops NavMeshAgent, triggers procedural collapse, and grants points.
2. **Mission Director (`DEADeyeMissionDirector.cs`)**:
   - `Reconnaissance` &rarr; `Disruption` &rarr; `TechnicianAdvance` &rarr; `NeutralizeCommander` &rarr; `ConvoyExtraction` &rarr; `Debriefing`.

---

## 6. Execution Flowcharts

### A. Game Initialization Sequence
```mermaid
sequenceDiagram
    autonumber
    participant Unity as Unity Engine
    participant Boot as DEADeyeBootLoader
    participant GM as DEADeyeGameManager
    participant Scene as RooftopMission Scene
    participant City as DEADeyeUrbanCityBuilder
    participant PC as DEADeyePlayerController
    participant HUD as DEADeyeMissionHUD
    participant TT as DEADeyeTacticalTerminalUI

    Unity->>Boot: RuntimeInitializeOnLoadMethod
    Boot->>GM: Ensure DEADeyeGameManager exists
    GM->>GM: DetectPlayerMode() (Desktop vs VR)
    Unity->>Scene: Load RooftopMission.unity
    Scene->>City: Awake() -> Build City Districts & Bake NavMesh
    Scene->>PC: Awake() & Start() -> EnforceActiveRig()
    alt Desktop Mode
        PC->>PC: Enable Desktop_FPSRig, Disable VR_XROriginRig
        PC->>PC: Mount XM500 to DesktopWeaponSocket
    else VR Mode
        PC->>PC: Enable VR_XROriginRig, Disable Desktop_FPSRig
        PC->>PC: Mount XM500 to Right Controller VRWeaponSocket
    end
    Scene->>HUD: Initialize HUD Canvas & Elements
    HUD->>TT: EnsureInstance() & Build Tactical Terminal
    TT->>TT: Display Operation DEAD AIR Briefing Card
```

### B. Player Input & Weapon Firing Pipeline
```mermaid
sequenceDiagram
    autonumber
    actor Player as Player (Human)
    participant Router as DEADeyeInputRouter
    participant Rig as Active Rig (Desktop/VR)
    participant Weapon as DEADeyeSniperWeapon
    participant Optic as DEADeyeScopeOptic
    participant Physics as PhysX Engine
    participant Score as DEADeyeScoreManager
    participant Audio as DEADeyeAudioManager
    participant Target as Hostile / Prop Collider

    Player->>Router: Squeeze RMB (Desktop) or Grip (Quest VR)
    Router->>Optic: SetScopeActive(true)
    Optic->>Optic: Enable ScopeCamera & Render to RT_SniperScopeLens
    Player->>Router: Pull LMB (Desktop) or Trigger (Quest VR)
    Router->>Weapon: TryFire()
    Weapon->>Weapon: Check IsScoped == true? (FIRE GATE)
    alt Unscoped
        Weapon->>Audio: PlayDryFire() (Shot Rejected)
    else Scoped
        Weapon->>Weapon: Check Ammo > 0 & Cooldown Ready?
        Weapon->>Audio: PlayGunshot() (Supersonic crack + urban echo)
        Weapon->>Weapon: Trigger Procedural Recoil Kick
        Weapon->>Physics: RaycastAll through Screen/Reticle Centre (0.5, 0.5)
        Physics-->>Weapon: Return Hits sorted by Distance
        Weapon->>Physics: Cast Muzzle-to-AimPoint Obstruction Ray
        alt Obstruction Hit (Wall / Railing)
            Weapon->>Audio: PlayImpactSound(Concrete/Metal)
            Weapon->>Score: RegisterMiss()
        else Clear Shot to Hostile
            Weapon->>Target: Check Collider (Headshot vs Body)
            alt Headshot Hitbox
                Target->>Target: TakeDamage(100 * 3 = 300)
                Weapon->>Score: RegisterHit(isHeadshot: true, points: 300)
                Weapon->>Audio: PlayHeadshotFeedback()
            else Body Hitbox
                Target->>Target: TakeDamage(100)
                Weapon->>Score: RegisterHit(isHeadshot: false, points: 100)
                Weapon->>Audio: PlayHitFeedback()
            end
        end
        Weapon->>Weapon: ConsumeAmmo() & Start Bolt Cycle Cooldown
    end
```

---

## 7. Answers to Required Architectural Questions

| Question | Architectural Fact & Verification |
|---|---|
| **What is the entry scene?** | `Assets/DEADeye/Scenes/RooftopMission.unity` is configured as build index `0` in `EditorBuildSettings.asset` for direct deployment. |
| **What starts first?** | `DEADeyeBootLoader.cs` executes via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`, ensuring `DEADeyeGameManager` exists before any scene Awake runs. |
| **Which GameObject creates the player?** | The player is pre-configured in the scene root under `DEADeye_PlayerController`, containing both `Desktop_FPSRig` and `VR_XROriginRig`. |
| **Who manages the mission?** | `DEADeyeMissionDirector.cs` on `Mission_AuthoritativeManagers` drives mission phases, countdown timers, and objective triggers. |
| **Who controls the HUD?** | `DEADeyeMissionHUD.cs` dynamically builds its canvas and manages telemetry text, objective bars, and scope reticle overlays. |
| **How does weapon communicate with player?** | In Desktop mode, `DEADeyeDesktopRig` calls `HandleWeaponInput()`; in VR mode, `DEADeyeSniperWeapon` queries `DEADeyeInputRouter` directly and is physically parented to `VRWeaponSocket`. |
| **How are objectives updated?** | `DEADeyeMissionDirector` invokes phase progression events, causing `DEADeyeMissionHUD.UpdateObjectiveTracker()` to display new objectives and play radio chimes. |
| **How are hostiles spawned?** | `DEADeyeMissionDirector` instantiates character prefabs from `hostilePrefabs[]` at dynamically registered `spawnPoints[]` generated by `DEADeyeUrbanCityBuilder`. |
| **How are score & accuracy calculated?** | `DEADeyeScoreManager.cs` tracks `shotsFired`, `shotsHit`, and `headshots`. Accuracy is calculated as `((float)shotsHit / shotsFired) * 100f`. |
| **How is the scope activated?** | Holding RMB (Desktop) or Right/Left Grip (VR) calls `DEADeyeScopeOptic.SetScopeActive(true)`, activating `ScopeCamera` and rendering to `RT_SniperScopeLens`. |
| **How does game know Desktop vs VR?** | `DEADeyeGameManager.DetectPlayerMode()` checks `#if UNITY_ANDROID && !UNITY_EDITOR` (forces VR on Quest) or queries `UnityEngine.XR.XRSettings.isDeviceActive`. |
| **Which objects persist across scenes?** | `DEADeyeGameManager` and `DEADeyeAudioManager` use singletons. However, `RooftopMission.unity` is a self-contained authoritative sandbox containing all systems. |
| **Which systems use singleton references?** | All core managers (`GameManager`, `AudioManager`, `ScoreManager`, `WeaponManager`, `InputRouter`, `MissionDirector`, `MissionHUD`, `TacticalTerminalUI`). |
| **Which systems use events?** | UI updates, hit registration, ammo consumption, mission completion, and hostile elimination use C# `Action` delegates to maintain zero-coupling. |

