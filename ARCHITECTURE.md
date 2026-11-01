# DEADeye VR — Technical Architecture Specification

## 1. Architectural Philosophy & Design Principles

DEADeye VR is architected according to modern software engineering standards for real-time XR applications:
- **Single Responsibility Principle (SRP)**: Every script has one clear domain (e.g., input routing, audio synthesis, score tracking, optic rendering).
- **Loose Coupling via C# Events & Interfaces**: Subsystems communicate via multicast events (`OnStateChanged`, `OnScoreUpdated`, `OnHostileEliminated`, `OnAmmoChanged`) rather than hard dependencies.
- **Strict Mutual Exclusion for XR / Desktop Rigs**: Virtual Reality (`XROrigin`) and Desktop Simulation (`DEADeyeDesktopRig`) never run concurrently, preventing double-audio listeners, conflicting camera matrices, and duplicate input consumers.
- **Assembly Definition Boundaries**: Clean boundaries separating `DEADeye.Runtime`, `DEADeye.Editor`, `DEADeye.EditMode.Tests`, and `DEADeye.PlayMode.Tests`.

---

## 2. Subsystem Breakdown

```mermaid
graph TD
    subgraph Core Systems
        GM[DEADeyeGameManager]
        IR[DEADeyeInputRouter]
        AM[DEADeyeAudioManager]
        SM[DEADeyeScoreManager]
        WM[DEADeyeWeaponManager]
        ST[DEADeyeSceneTransition]
    end

    subgraph Player Systems
        PC[DEADeyePlayerController]
        DR[DEADeyeDesktopRig]
        XR[XROrigin VR Rig]
        RB[DEADeyeRooftopBoundary]
    end

    subgraph Weapon & Optics
        SW[DEADeyeSniperWeapon]
        SO[DEADeyeScopeOptic]
    end

    subgraph AI & Mission
        MD[DEADeyeMissionDirector]
        AI[DEADeyeHostileAI FSM]
        EZ[DEADeyeEscapeZone]
        HM[DEADeyeHostileMarker]
    end

    subgraph User Interface
        LUI[DEADeyeLobbyUI]
        HUD[DEADeyeMissionHUD]
        RUI[DEADeyeResultsUI]
    end

    GM --> ST
    GM --> PC
    GM --> MD
    PC --> DR
    PC --> XR
    DR --> RB
    XR --> RB

    MD --> AI
    AI --> EZ
    AI --> HM

    DR --> SW
    XR --> SW
    SW --> SO
    SW --> SM
    SW --> AM

    MD --> HUD
    SM --> HUD
    WM --> HUD
    MD --> RUI
```

### 2.1 Core Subsystems
- **`DEADeyeGameManager`**: Persistent singleton managing `DEADeyeGameState` (`Boot`, `Lobby`, `MissionBriefing`, `MissionActive`, `MissionDebriefing`, `MissionFailed`, `Paused`) and `MissionDifficulty` (`Recruit`, `Operative`, `Elite`).
- **`DEADeyeInputRouter`**: Centralizes input event routing for desktop and VR devices. Integrates `IsPointerOverUI()` suppression to ensure mouse clicks or controller triggers over UI elements do not discharge weapons.
- **`DEADeyeAudioManager`**: Procedural real-time waveform synthesizer. Synthesizes gunshots (transient noise + exponential decay), bolt cycling, reloads, hit chimes, and city ambient rumble without relying on external proprietary audio assets.
- **`DEADeyeScoreManager`**: Aggregates telemetry (shots fired, hits, precision headshots, accuracy %, combo streaks, total score) and dispatches real-time updates to listeners.
- **`DEADeyeWeaponManager`**: Enforces magazine capacity (5 rounds), bullet consumption, and cycling timers.
- **`DEADeyeSceneTransition`**: Performs asynchronous scene loading (`BootScene` -> `LobbyScene` -> `RooftopMission`) with smooth state synchronization.

### 2.2 Player & Boundary Subsystems
- **`DEADeyePlayerController`**: Evaluates `XRSettings.isDeviceActive` and presence of VR hardware. Enforces active rig state, disabling inactive components, cameras, and audio listeners.
- **`DEADeyeDesktopRig`**: Smooth FPS controller for desktop testing, supporting WASD movement, mouse pitch/yaw, weapon alignment socket, RMB scope aiming, and LMB weapon firing.
- **`DEADeyeRooftopBoundary`**: Clamps player transform within safe rooftop coordinates ($X \in [-12, 12]$, $Z \in [-12, 2]$ at $Y = 26$m), guaranteeing the player cannot clip or fall off the perimeter.

### 2.3 Weapon & Optical Systems
- **`DEADeyeSniperWeapon`**: Interactive ballistic rifle extending `XRGrabInteractable`. Features two-handed grab stabilization, hitscan ballistics raycasting with distance falloff, muzzle flash particle bursts, dynamic bullet tracer line renderers, and procedural recoil pitch and recovery.
- **`DEADeyeScopeOptic`**: Real-time optical scope rendering a zoomed perspective (4x / 8x FOV) onto a 512x512 circular RenderTexture lens. Features culling when unequipped to conserve mobile GPU fillrate.

### 2.4 AI & Mission Director
- **`DEADeyeHostileAI`**: Finite State Machine controlling urban hostile behaviors.
- **`DEADeyeMissionDirector`**: Governs wave generation, mission countdown (120s timer), extraction zone monitoring, and win/loss resolution.
- **`DEADeyeEscapeZone`**: Designated extraction checkpoints triggering hostile escapes and score penalties.

```mermaid
stateDiagram-v2
    [*] --> Spawn: Wave Trigger
    Spawn --> Patrol: NavMesh Agent Ready
    Patrol --> Suspicious: Near Bullet Impact
    Suspicious --> Alerted: Line of Sight / Damage
    Alerted --> Escape: Sprint to Extraction Zone
    Patrol --> Eliminated: Lethal Hitscan Impact
    Suspicious --> Eliminated: Lethal Hitscan Impact
    Alerted --> Eliminated: Lethal Hitscan Impact
    Escape --> Eliminated: Lethal Hitscan Impact
    Escape --> Escaped: Reached Extraction Trigger
    Eliminated --> [*]
    Escaped --> [*]
```

---

## 3. Scene Architecture & Routing

```mermaid
sequenceDiagram
    participant User
    participant Boot as BootScene
    participant GM as DEADeyeGameManager
    participant Lobby as LobbyScene
    participant Mission as RooftopMission
    participant Results as DEADeyeResultsUI

    User->>Boot: Launch Application
    Boot->>GM: Initialize Singletons (GameManager, Audio, Score, Weapon)
    GM->>Lobby: Async Transition to LobbyScene
    Lobby->>User: Display Tactical Dojo & Operative Avatar
    User->>Lobby: Select Difficulty & Click [DEPLOY TO ROOFTOP]
    Lobby->>Mission: Async Transition to RooftopMission
    Mission->>User: Initialize 26m Perch, City NavMesh, HUD & Sniper Rifle
    User->>Mission: Eliminate Hostiles Before 120s Expiration
    Mission->>Results: Trigger Mission Concluded (Victory / Defeat)
    Results->>User: Display Detailed Debriefing & Metrics
    User->>Lobby: Click [RETURN TO LOBBY]
    Results->>Lobby: Async Transition to LobbyScene
```

---

## 4. Assembly Definition Architecture

| Assembly | Path | Purpose |
| :--- | :--- | :--- |
| **`DEADeye.Runtime`** | `Assets/DEADeye/Scripts/DEADeye.Runtime.asmdef` | Core gameplay, AI, weapons, audio, UI, and player systems |
| **`DEADeye.Editor`** | `Assets/DEADeye/Editor/DEADeye.Editor.asmdef` | Automated scene generation and Android APK compilation pipeline |
| **`DEADeye.EditMode.Tests`** | `Assets/DEADeye/Tests/EditMode/DEADeye.EditMode.Tests.asmdef` | 10 unit and architectural integration test suites |
| **`DEADeye.PlayMode.Tests`** | `Assets/DEADeye/Tests/PlayMode/DEADeye.PlayMode.Tests.asmdef` | 5 end-to-end live gameplay test suites with in-engine screenshot captures |
