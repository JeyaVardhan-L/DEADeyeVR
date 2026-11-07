# DEADeye VR — Complete Codebase & API Engineering Reference

This reference documents the custom C# scripts comprising **DEADeye VR** in `C:\Unity\Editors\DEADeyeVR_Foundation\Assets\DEADeye\Scripts\`. Every class, method, property, and algorithmic design decision is documented with actual line references and edge-case behaviors.

---

## Script Quick Index

1. [Core Systems](#1-core-systems)
   - `DEADeyeGameManager`, `DEADeyeInputRouter`, `DEADeyeAudioManager`, `DEADeyeScoreManager`, `DEADeyeWeaponManager`, `DEADeyeEnemyRegistry`, `DEADeyeBootLoader`, `DEADeyeSceneTransition`, `DEADeyeLobbyAvatarController`, Enums (`DEADeyeGameState`, `DEADeyePlayerMode`)
2. [Player & Locomotion](#2-player--locomotion)
   - `DEADeyePlayerController`, `DEADeyeDesktopRig`, `DEADeyeRooftopBoundary`
3. [Weapons & Ballistics](#3-weapons--ballistics)
   - `DEADeyeSniperWeapon`, `DEADeyeScopeOptic`
4. [Artificial Intelligence & NPCs](#4-artificial-intelligence--npcs)
   - `DEADeyeHostileAI`, `DEADeyeHostileCriminal`, `DEADeyeHostileHumanoid`, `DEADeyeHostileMonster`, `DEADeyeFriendlyAI`, `DEADeyeCivilianAI`, `DEADeyeProceduralCharacterAnimator`, `DEADeyeHostileMarker`, `DEADeyeEscapeZone`, `EnemyStates`, `IEnemyState`
5. [Mission & Objectives](#5-mission--objectives)
   - `DEADeyeMissionDirector`, `DEADeyeMissionDefinition`, `DEADeyeMissionObjective`
6. [Reconnaissance & Spotting](#6-reconnaissance--spotting)
   - `DEADeyeSpottingSystem`, `DEADeyeSpotable`
7. [User Interface & Telemetry](#7-user-interface--telemetry)
   - `DEADeyeMissionHUD`, `DEADeyeTacticalTerminalUI`, `DEADeyeResultsUI`, `DEADeyeLobbyUI`
8. [Environment & Generation](#8-environment--generation)
   - `DEADeyeUrbanCityBuilder`, `DEADeyeEnvironmentalProp`, `DEADeyeDojoLobbyManager`
9. [Editor Automation & Build Pipelines](#9-editor-automation--build-pipelines)
   - `DEADeyeAndroidBuilder`, `DEADeyeMasterCampaignIntegrator`, `DEADeyeSceneInspector`, `DEADeyeScreenshotCapture`, `DEADeyeCharacterSetupUtility`
10. [Automated Test Suites](#10-automated-test-suites)
    - `DEADeyeIntegrationTests`, `DEADeyeAccuracyTests`, `DEADeyePlayModeTests`, `DEADeyeCampaignScreenshotSuite`

---

## 1. Core Systems

### `DEADeyeGameManager.cs`
- **Path**: `Assets/DEADeye/Scripts/Core/DEADeyeGameManager.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Central authoritative singleton managing global game states, player mode (Desktop vs VR), and difficulty level.
- **Key Fields & Properties**:
  - `public static DEADeyeGameManager Instance { get; set; }`
  - `[SerializeField] private DEADeyeGameState currentState`: Current state (`Briefing`, `MissionActive`, `Paused`, `Completed`, `Failed`).
  - `[SerializeField] private DEADeyePlayerMode playerMode`: Active mode (`Desktop` or `VR`).
  - `[SerializeField] private MissionDifficulty difficulty`: `Recruit`, `Operative`, `Veteran`.
- **Key Methods**:
  - `public void DetectPlayerMode()`:
    ```csharp
    bool xrActive = UnityEngine.XR.XRSettings.isDeviceActive;
    #if UNITY_ANDROID && !UNITY_EDITOR
    playerMode = DEADeyePlayerMode.VR;
    #else
    playerMode = xrActive ? DEADeyePlayerMode.VR : DEADeyePlayerMode.Desktop;
    #endif
    ```
    *Explanation*: On standalone Quest 2, `#if UNITY_ANDROID` unconditionally forces `DEADeyePlayerMode.VR`. On PC, it checks if an active XR headset is streaming via OpenXR; otherwise defaults to Desktop FPS mode.
  - `public void SetGameState(DEADeyeGameState newState)`: Changes state and invokes `OnGameStateChanged`.

---

### `DEADeyeInputRouter.cs`
- **Path**: `Assets/DEADeye/Scripts/Core/DEADeyeInputRouter.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Unified input query aggregator. Routes mouse/keyboard input on Desktop and Meta Quest Touch Controller input in VR without requiring desktop window focus.
- **Key Fields**:
  - `private bool prevRightTrigger, prevRightGrip, prevLeftGrip, prevButtonA, prevButtonB, prevButtonX, prevButtonY, prevMenuButton`: State buffers for hardware edge-detection.
  - `private bool vrTriggerDown, vrGripHeld, vrReloadDown, vrInfoDown, vrTerminalDown, vrPauseDown`: Cached frame values.
  - `private Vector2 vrMoveInput`: Thumbstick vector with `0.04f` deadzone filtering.
- **Key Methods**:
  - `private void PollVRControllers()`: Reads `UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand)` and `XRNode.LeftHand`. Checks `CommonUsages.triggerButton`, `CommonUsages.gripButton`, `CommonUsages.primaryButton` (A/X), `CommonUsages.secondaryButton` (B/Y), and `CommonUsages.menuButton`.
  - `public bool GetFireDown()`: Returns `Mouse.current.leftButton.wasPressedThisFrame || vrTriggerDown`. Blocks firing if `IsPointerOverUI()` is true.
  - `public bool GetAimHeld()`: Returns `Mouse.current.rightButton.isPressed || vrGripHeld`.
  - `public Vector2 GetMoveInput()`: Checks WASD keys. If zero, returns `vrMoveInput` from left controller thumbstick.

---

### `DEADeyeAudioManager.cs`
- **Path**: `Assets/DEADeye/Scripts/Core/DEADeyeAudioManager.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Manages all 2D and 3D spatial sound effects. Includes an algorithmic audio synthesis engine that generates procedural PCM sound waves if external `.wav` files are missing.
- **Key Methods**:
  - `public void PlayGunshot(Vector3 position)`: Plays `Sniper_Gunshot_Close.wav` and layers a spatial reverberation echo.
  - `public void PlayDryFire()`: Plays an empty mechanical click when the player attempts to shoot unscoped.
  - `public void PlayImpact(Vector3 position, SurfaceType surface)`: Plays concrete, metal, or flesh impact audio based on raycast hit collider tags.
  - `private AudioClip SynthesizeSniperGunshot()`: Programmatically constructs a 44.1kHz audio buffer:
    $$\text{Sample}(t) = \sin(2\pi \cdot 65 \cdot t) \cdot e^{-12t} + \text{WhiteNoise}(t) \cdot e^{-28t}$$
    Delivers a sub-bass kick transient layered with high-frequency supersonic air displacement.

---

### `DEADeyeScoreManager.cs`
- **Path**: `Assets/DEADeye/Scripts/Core/DEADeyeScoreManager.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Authoritative scoring engine tracking shots, hits, headshots, hostile eliminations, civilian casualties, and calculating accuracy percentage.
- **Key Methods**:
  - `public void RegisterShot()`: Increments `shotsFired`.
  - `public void RegisterHit(bool isHeadshot, int points)`: Increments `shotsHit`, evaluates combo multipliers, and fires `OnHitRegistered`.
  - `public float AccuracyPercentage => shotsFired > 0 ? ((float)shotsHit / shotsFired) * 100f : 0f;`

---

### `DEADeyeWeaponManager.cs`
- **Path**: `Assets/DEADeye/Scripts/Core/DEADeyeWeaponManager.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Tracks active weapon magazine count (5 rounds capacity), reload durations (1.8s), and enforces the firing gate.
- **Key Methods**:
  - `public bool CanFire()`: Returns `currentAmmo > 0 && !isReloading`.
  - `public void ConsumeAmmo()`: Decrements `currentAmmo` and invokes `OnAmmoChanged`.
  - `public void Reload()`: Initiates coroutine `ReloadRoutine()`, plays reload audio, and restores magazine to 5 rounds after 1.8 seconds.

---

## 2. Player & Locomotion

### `DEADeyePlayerController.cs`
- **Path**: `Assets/DEADeye/Scripts/Player/DEADeyePlayerController.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Authoritative parent controller enforcing mutual exclusivity between Desktop and VR player rigs.
- **Key Methods**:
  - `public void EnforceActiveRig(DEADeyePlayerMode? explicitMode = null)`:
    - If `VR`: Disables `desktopRig`, activates `vrRig`, and executes `AttachWeaponToVRHand()`.
    - If `Desktop`: Disables `vrRig`, activates `desktopRig`, and calls `desktopRig.AttachWeapon()`.
  - `private void AttachWeaponToVRHand()`: Locates `Right Controller` under `VR_XROriginRig`, creates or finds `VRWeaponSocket` at `(0f, -0.04f, 0.12f)`, parents `XM500_PrecisionSniper`, and locks its `Rigidbody` (`isKinematic = true, useGravity = false`).

---

### `DEADeyeDesktopRig.cs`
- **Path**: `Assets/DEADeye/Scripts/Player/DEADeyeDesktopRig.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: First-person keyboard and mouse controller. Provides mouse look, cursor lock, WASD rooftop traversal, and handles desktop weapon firing inputs.
- **Key Methods**:
  - `private void HandleMouseLook()`:
    ```csharp
    Vector2 look = DEADeyeInputRouter.Instance.GetLookInput();
    yaw += look.x;
    pitch -= look.y;
    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
    cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    ```
    *Explanation*: Uses raw pixel deltas rather than virtual axes, guaranteeing zero stationary cursor drift.
  - `private void HandleAiming()`: Squeezing RMB smoothly interpolates camera FOV from 60° to 32° and shifts the weapon socket to optical ADS position `(0f, -0.155f, 0.36f)`.

---

### `DEADeyeRooftopBoundary.cs`
- **Path**: `Assets/DEADeye/Scripts/Player/DEADeyeRooftopBoundary.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Spatial safety network. Monitors player coordinates across the 3 overwatch perches and connecting bridges. If the player falls below `minimumSafeY = 18.0f`, teleports them to the nearest safe perch node without game-over penalty.

---

## 3. Weapons & Ballistics

### `DEADeyeSniperWeapon.cs`
- **Path**: `Assets/DEADeye/Scripts/Weapons/DEADeyeSniperWeapon.cs`
- **Namespace**: `DEADeye` | **Base Class**: `XRGrabInteractable`
- **Responsibility**: Authoritative sniper ballistics, camera-centre targeting, muzzle obstruction validation, recoil recovery, and hit registration.
- **Key Properties**:
  - `public bool IsScoped`: Checks if `scopeOptic.IsAiming` or `desktopRig.IsAiming` is true. Mandatory prerequisite for weapon firing.
- **Key Methods**:
  - `public bool TryFire()`:
    1. Validates that `DEADeyeGameManager.Instance.CurrentState == DEADeyeGameState.MissionActive`.
    2. Validates `!DEADeyeInputRouter.Instance.IsPointerOverUI()`.
    3. **Enforces Fire Gate**: `if (!IsScoped) return false;`.
    4. Validates cooldown (`Time.time >= nextFireTime`) and ammo (`CanFire()`).
    5. Calls `ExecuteRaycastShot()`.
  - `private void ExecuteRaycastShot()`:
    - **Step 1 (Camera Aim Ray)**: Casts a ray from `activeAimingCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f))` out to 350 meters.
    - **Step 2 (Aim Point Evaluation)**: Finds the first valid collider not marked as player geometry. If nothing is hit, aim point is set to $Origin + Direction \times 350m$.
    - **Step 3 (Muzzle-to-Target Obstruction Check)**:
      ```csharp
      Vector3 muzzlePos = muzzlePoint.position;
      Vector3 muzzleDir = (intendedAimPoint - muzzlePos).normalized;
      float dist = Vector3.Distance(muzzlePos, intendedAimPoint);
      RaycastHit[] muzzleHits = Physics.RaycastAll(new Ray(muzzlePos, muzzleDir), dist + 0.15f, hitLayers);
      ```
      If an obstacle (concrete wall, parapet, structural pillar) intersects this muzzle ray before the intended aim point, the shot is blocked, concrete impact audio plays, and target damage is aborted.
    - **Step 4 (Damage Routing)**: If hit is clear, checks for `DEADeyeHostileAI` or `DEADeyeEnvironmentalProp`. Differentiates headshots (3x damage: 300 HP) from torso/limbs (100 HP).

---

### `DEADeyeScopeOptic.cs`
- **Path**: `Assets/DEADeye/Scripts/Weapons/DEADeyeScopeOptic.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Optical sniper scope. Renders a magnified 12° FOV image to a 1024x1024 ARGB32 RenderTexture (`RT_SniperScopeLens`) with 4x MSAA, displaying the view on the rifle's 3D lens and the HUD reticle overlay.

---

## 4. Artificial Intelligence & NPCs

### `DEADeyeHostileAI.cs`
- **Path**: `Assets/DEADeye/Scripts/AI/DEADeyeHostileAI.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Base class for urban hostiles. Implements NavMeshAgent navigation and the finite state machine (`IEnemyState`).
- **State Machine Architecture**:
  - `HostileSpawnState`: Stabilizes agent on NavMesh for 0.5s.
  - `HostilePatrolState`: Moves through assigned waypoints at `walkSpeed = 1.6 m/s`.
  - `HostileSuspiciousState`: Halts on hearing unsuppressed gunshot or nearby impact; faces threat origin for 3 seconds.
  - `HostileAlertedState`: Sprints at `alertSpeed = 2.8 m/s` toward tactical cover.
  - `HostileEscapeState`: Triggered when mission fails or timer expires; sprints at `escapeSpeed = 3.6 m/s` toward `DEADeyeEscapeZone`.
  - `HostileEliminatedState`: Disables colliders, stops agent, triggers procedural death collapse, and notifies `DEADeyeMissionDirector`.

---

### `DEADeyeFriendlyAI.cs`
- **Path**: `Assets/DEADeye/Scripts/AI/DEADeyeFriendlyAI.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Controls Comms Technicians Elena and Marcus during Phase 3 of Operation DEAD AIR.
- **Key Mechanics**:
  - `workProgress`: Increments from 0% to 100% at `repairSpeedPerSecond = 3.5%/s` while at the Comms Console.
  - If hostile fire passes within 8 meters, shifts to `FriendlyState.TakingCover`, halting repair until the sniper clears the threat.
  - If technician health reaches 0, fires `OnVipKilled`, triggering immediate mission failure.

---

### `DEADeyeProceduralCharacterAnimator.cs`
- **Path**: `Assets/DEADeye/Scripts/AI/DEADeyeProceduralCharacterAnimator.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Procedural bone animation. Automatically locates `leg-left`, `leg-right`, `arm-left`, `arm-right`, `torso`, and `head` transforms. Calculates natural sinusoidal limb swing based on `agent.velocity.magnitude` without requiring imported animation clips.

---

## 5. Mission & Campaign Framework

### `DEADeyeMissionDirector.cs`
- **Path**: `Assets/DEADeye/Scripts/Mission/DEADeyeMissionDirector.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Authoritative mission controller driving the 5 sequential phases of **Operation DEAD AIR**:
  1. `Phase 1: Reconnaissance`: Player must locate and dwell-spot the Military Jammer, Technicians Elena & Marcus, and Commander Vance.
  2. `Phase 2: Disruption`: Player must disable the Jammer (shooting the high-voltage Power Junction or shooting the Jammer array directly).
  3. `Phase 3: TechnicianAdvance`: Escort Elena & Marcus across Central Plaza to the Comms Console while neutralizing attacking hostile waves.
  4. `Phase 4: NeutralizeCommander`: Commander Vance spawns at the Construction Site with an armed escort, attempting to escape to the Transport Interchange.
  5. `Phase 5: ConvoyExtraction`: Clear final ambush forces to allow the allied extraction convoy to secure the team.

---

### `DEADeyeMissionDefinition.cs`
- **Path**: `Assets/DEADeye/Scripts/Mission/DEADeyeMissionDefinition.cs`
- **Namespace**: `DEADeye` | **Base Class**: `ScriptableObject`
- **Responsibility**: Data container holding operational parameters: Operation Name, District, Threat Level, Duration, Max Escapes Allowed, and Briefing Text.

---

## 6. Reconnaissance & Spotting

### `DEADeyeSpottingSystem.cs`
- **Path**: `Assets/DEADeye/Scripts/Recon/DEADeyeSpottingSystem.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Casts a continuous ray through the optical scope center (0.5, 0.5). When focused on a `DEADeyeSpotable` target for `spotDwellRequired = 0.85s`, marks the target as identified, plays a tactical audio ping, and triggers mission objective updates.

---

## 7. User Interface & Telemetry

### `DEADeyeMissionHUD.cs`
- **Path**: `Assets/DEADeye/Scripts/UI/DEADeyeMissionHUD.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: In-game HUD canvas management.
- **Dynamic Render Mode**:
  - In Desktop mode: `RenderMode.ScreenSpaceOverlay`.
  - In VR mode: `RenderMode.ScreenSpaceCamera`, `worldCamera = ActiveCamera`, `planeDistance = 1.0f`.
- **Contains**: Telemetry bar (timer, score, hostile counter, escape limit), sequential objective bar, ammunition indicator, optical scope overlay, and results panel.

---

### `DEADeyeTacticalTerminalUI.cs`
- **Path**: `Assets/DEADeye/Scripts/UI/DEADeyeTacticalTerminalUI.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: High-contrast mission selection terminal. Displays 4 operations (Op 01: DEAD AIR playable; Ops 02–04 locked), personnel dossier cards, objective overviews, and the "DEPLOY" launch trigger.

---

## 8. Environment & Generation

### `DEADeyeUrbanCityBuilder.cs`
- **Path**: `Assets/DEADeye/Scripts/Environment/DEADeyeUrbanCityBuilder.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: 2,138-line procedural city generator. Procedurally constructs:
  1. Central Commercial Plaza with Comms Tower, Jammer, and fountain cover.
  2. Residential District with multi-story brownstones and alleys.
  3. Transport Interchange with 4-lane boulevard, bus shelter, and evac route.
  4. Construction Site with 4-floor concrete skeleton and tower crane.
  5. 3 Walkable Overwatch Perches interconnected by steel catwalks and skybridges with physical railing colliders.
  6. Bakes the runtime `NavMeshSurface` across all streets and sidewalks.

---

### `DEADeyeEnvironmentalProp.cs`
- **Path**: `Assets/DEADeye/Scripts/Environment/DEADeyeEnvironmentalProp.cs`
- **Namespace**: `DEADeye` | **Base Class**: `MonoBehaviour`
- **Responsibility**: Shootable interactive props:
  - `PowerJunction`: Shooting disables the jammer instantly.
  - `AlarmBell`: Shooting silences district sirens before guards raise an alert.
  - `DistractionTransformer`: Shooting triggers electrical sparks, pulling nearby patrol guards.
  - `SecurityGate`: Shooting opens the locked alley gate for an alternate path.

---

## 9. Editor Automation & Build Pipelines

### `DEADeyeAndroidBuilder.cs`
- **Path**: `Assets/DEADeye/Editor/DEADeyeAndroidBuilder.cs`
- **Namespace**: `DEADeye.Editor` | **Type**: `static class`
- **Responsibility**: Automated Android ARM64 IL2CPP build pipeline for Meta Quest 2. Can be executed from the Unity Editor menu (`DEADeye/Build/Build Quest 2 Standalone APK`) or headless batchmode:
  ```powershell
  Unity.exe -quit -batchmode -projectPath "C:\Unity\Editors\DEADeyeVR_Foundation" -executeMethod DEADeye.Editor.DEADeyeAndroidBuilder.BuildQuest2Apk -logFile "build_quest2.log"
  ```
  Outputs standalone package to: `Builds/Android/DEADeye_Quest2.apk`.

---

## 10. Automated Test Suites

| Test Class | Suite Type | Path | Tests | Key Validations |
|---|---|---|---|---|
| `DEADeyeIntegrationTests.cs` | EditMode | `Assets/DEADeye/Tests/EditMode/` | 19 | Scene singletons, dual-rig exclusivity, fire gate enforcement, difficulty multipliers, 3x headshot damage |
| `DEADeyeAccuracyTests.cs` | PlayMode | `Assets/DEADeye/Tests/PlayMode/` | 8 | Center-screen crosshair hit accuracy, headshot hitbox registration, muzzle wall obstruction blocking, target distance testing |
| `DEADeyePlayModeTests.cs` | PlayMode | `Assets/DEADeye/Tests/PlayMode/` | 13 | Rooftop traversal across perches 1, 2, 3, catwalk navigation, reload cycle, fall recovery, end-game results |
| `DEADeyeCampaignScreenshotSuite.cs`| PlayMode | `Assets/DEADeye/Tests/PlayMode/` | 8 | Visual capture across 8 mission phases: Briefing, Overview, Overwatch Perches, Recon, Power Junction, Technicians, Vance, Debrief |

