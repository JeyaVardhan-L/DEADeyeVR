# DEADeye VR — Project Source & Asset Inventory

**Project Path**: `C:\Unity\Editors\DEADeyeVR_Foundation`  
**Unity Engine**: Unity 6.3 LTS (`6000.3.27f2`)  
**Pipeline**: Universal Render Pipeline (URP 17.3.0)  
**XR Framework**: OpenXR 1.16.1 + XR Interaction Toolkit 3.4.0  
**Target Architectures**: Desktop Windows (x86_64) & Meta Quest 2 / 3 / Pro (Android ARM64)

---

## 1. Directory Structure Overview

```text
C:\Unity\Editors\DEADeyeVR_Foundation\
├── Assets\
│   ├── DEADeye\
│   │   ├── Animations\           # Character animation controllers
│   │   ├── Art\                  # Meshes, characters, weapons, VFX, textures
│   │   │   ├── Characters\       # Quaternius modular models & clothed variants
│   │   │   ├── Environments\     # Dojo lobby assets
│   │   │   ├── Materials\        # Shared URP materials
│   │   │   ├── Textures\         # Environmental and UI textures
│   │   │   ├── VFX\              # Hit feedback and projectile impact VFX
│   │   │   └── Weapons\          # Modular sniper rifle & low-poly firearms
│   │   ├── Audio\                # Recorded 16-bit 44.1kHz WAV sound effects
│   │   ├── Data\                 # Mission definitions and ScriptableObjects
│   │   ├── Editor\               # Automated build pipelines & scene builders
│   │   ├── Prefabs\              # GameObjects, characters, HUD, props
│   │   ├── Scenes\               # RooftopMission (primary), BootScene, LobbyScene
│   │   ├── Scripts\              # Authoritative C# source code
│   │   │   ├── AI\               # Hostile, friendly, civilian, procedural anim
│   │   │   ├── Core\             # Game manager, audio, score, weapon mgr, router
│   │   │   ├── Editor\           # Scene inspection, setup, screenshot capture
│   │   │   ├── Environment\      # Procedural city builder, props, boundaries
│   │   │   ├── Mission\          # Mission director, definitions, objectives
│   │   │   ├── Player\           # Dual-rig controller, desktop rig, boundary
│   │   │   ├── Recon\            # Optical target spotting & reconnaissance
│   │   │   ├── UI\               # Tactical terminal, HUD, debriefing, lobby
│   │   │   └── Weapons\          # Sniper rifle ballistics, optical scope
│   │   ├── Settings\             # URP and graphics configuration assets
│   │   └── Tests\                # Automated EditMode and PlayMode NUnit suites
│   │       ├── EditMode\         # Architectural & structural integration tests
│   │       └── PlayMode\         # Ballistics, accuracy, traversal, screenshots
│   ├── Editor\                   # Legacy foundation scene builders
│   ├── Materials\                # Default foundation materials
│   ├── Prefabs\                  # Legacy foundation prefabs
│   ├── RendererData\             # URP Universal Renderer assets
│   ├── Samples\                  # XR Interaction Toolkit 3.4.0 starter assets
│   ├── Scenes\                   # FoundationTest.unity
│   ├── Scripts\                  # Foundation test runner and mock targets
│   ├── Tests\                    # Foundation tests
│   ├── TextMesh Pro\             # TMP fonts and default resources
│   ├── XR\                       # XR General Settings & OpenXR Package Settings
│   └── XRI\                      # XR Interaction Toolkit runtime & editor settings
├── Builds\
│   └── Android\                  # Standalone APK artifacts (DEADeye_Quest2.apk)
├── Documentation\                # Technical documentation suite & API reference
├── Packages\                     # Unity Package Manager manifest.json
└── ProjectSettings\              # Editor, physics, tags, XR, and player settings
```

---

## 2. Complete C# Script Inventory

Total custom C# scripts in project (excluding third-party samples): **58 files**.

### A. Core Systems (`Assets/DEADeye/Scripts/Core/`)
| File | Namespace | Lines | Primary Responsibility |
|---|---|---|---|
| `DEADeyeAudioManager.cs` | `DEADeye` | 590 | Manages 2D/3D audio playback with procedural sound synthesis fallback |
| `DEADeyeBootLoader.cs` | `DEADeye` | 38 | Startup bootstrap enforcing initialization order |
| `DEADeyeEnemyRegistry.cs` | `DEADeye` | 52 | Spatial lookup registry for active hostiles and NPCs |
| `DEADeyeGameManager.cs` | `DEADeye` | 115 | Central singleton managing game state, player mode, and difficulty |
| `DEADeyeGameState.cs` | `DEADeye` | 18 | Enum defining game states (`Briefing`, `MissionActive`, `Paused`, `Completed`, `Failed`) |
| `DEADeyeInputRouter.cs` | `DEADeye` | 344 | Unified input provider for Desktop (mouse/keys) and Quest VR (Touch controllers) |
| `DEADeyeLobbyAvatarController.cs` | `DEADeye` | 154 | Interactive lobby avatar control and visualization |
| `DEADeyePlayerMode.cs` | `DEADeye` | 10 | Enum defining player rig mode (`Desktop`, `VR`) |
| `DEADeyeSceneTransition.cs` | `DEADeye` | 89 | Asynchronous scene loading with visual fade transitions |
| `DEADeyeScoreManager.cs` | `DEADeye` | 139 | Tracks shots, hits, headshots, combos, score calculation, and accuracy % |
| `DEADeyeWeaponManager.cs` | `DEADeye` | 113 | Ammo magazine state, reload timings, and fire gate enforcement |

### B. Player & Rig Systems (`Assets/DEADeye/Scripts/Player/`)
| File | Namespace | Lines | Primary Responsibility |
|---|---|---|---|
| `DEADeyeDesktopRig.cs` | `DEADeye` | 392 | Dedicated PC FPS controller: WASD movement, mouse look, scoped firing, cursor lock |
| `DEADeyePlayerController.cs` | `DEADeye` | 196 | Authoritative controller managing mutual exclusivity between Desktop and XR Origin rigs |
| `DEADeyeRooftopBoundary.cs` | `DEADeye` | 96 | Spatial boundary monitoring and fall recovery across rooftop perches and catwalks |

### C. Weapons & Ballistics (`Assets/DEADeye/Scripts/Weapons/`)
| File | Namespace | Lines | Primary Responsibility |
|---|---|---|---|
| `DEADeyeScopeOptic.cs` | `DEADeye` | 121 | Optical scope lens rendering to 1024x1024 RenderTexture with MSAA and FOV zoom |
| `DEADeyeSniperWeapon.cs` | `DEADeye` | 682 | High-caliber sniper rifle: camera-centre raycast, muzzle obstruction check, recoil, fire gate |

### D. Artificial Intelligence & NPCs (`Assets/DEADeye/Scripts/AI/`)
| File | Namespace | Lines | Primary Responsibility |
|---|---|---|---|
| `DEADeyeCivilianAI.cs` | `DEADeye` | 162 | Neutral civilian logic, fleeing behavior, and friendly-fire penalty rules |
| `DEADeyeEscapeZone.cs` | `DEADeye` | 34 | Trigger volume marking enemy exfiltration / escape points |
| `DEADeyeFriendlyAI.cs` | `DEADeye` | 242 | VIP technicians (Elena & Marcus): cover taking, comms repair progress (0-100%) |
| `DEADeyeHostileAI.cs` | `DEADeye` | 326 | Base enemy class: NavMesh pathfinding, state machine integration, damage handling |
| `DEADeyeHostileCriminal.cs` | `DEADeye` | 238 | Specialized urban operative hostile with cover usage and sprint evasions |
| `DEADeyeHostileHumanoid.cs` | `DEADeye` | 195 | Humanoid patrol guard with visual awareness and alert broadcasting |
| `DEADeyeHostileMarker.cs` | `DEADeye` | 98 | Tactical world-space UI indicator positioned above enemy heads |
| `DEADeyeHostileMonster.cs` | `DEADeye` | 260 | Aggressive biological hostile variant with erratic leap locomotion |
| `DEADeyeProceduralCharacterAnimator.cs` | `DEADeye` | 117 | Procedural bone swing, breathing, crouching, and death collapse without clip dependencies |
| `EnemyStates.cs` | `DEADeye` | 187 | Concrete FSM states: `Spawn`, `Patrol`, `Suspicious`, `Alerted`, `Escape`, `Eliminated` |
| `IEnemyState.cs` | `DEADeye` | 14 | Interface contract for hostile state machine transitions |

### E. Mission & Campaign Framework (`Assets/DEADeye/Scripts/Mission/`)
| File | Namespace | Lines | Primary Responsibility |
|---|---|---|---|
| `DEADeyeMissionDefinition.cs` | `DEADeye` | 64 | ScriptableObject defining mission parameters, targets, story text, and constraints |
| `DEADeyeMissionDirector.cs` | `DEADeye` | 917 | State machine driving the 5 phases of Operation DEAD AIR and tracking win/loss conditions |
| `DEADeyeMissionObjective.cs` | `DEADeye` | 37 | Individual objective data container with completion state and telemetry |

### F. Reconnaissance & Spotting (`Assets/DEADeye/Scripts/Recon/`)
| File | Namespace | Lines | Primary Responsibility |
|---|---|---|---|
| `DEADeyeSpotable.cs` | `DEADeye` | 62 | Target component receiving optical dwell focus for recon intelligence |
| `DEADeyeSpottingSystem.cs` | `DEADeye` | 118 | Raycasts through optic reticle to positively identify targets after ~0.85s dwell |

### G. User Interface & HUD (`Assets/DEADeye/Scripts/UI/`)
| File | Namespace | Lines | Primary Responsibility |
|---|---|---|---|
| `DEADeyeLobbyUI.cs` | `DEADeye` | 382 | Tactical briefing lobby interface with loadout selection |
| `DEADeyeMissionHUD.cs` | `DEADeye` | 1208 | Dynamic in-game telemetry: timer, score, ammo, objective bar, scope overlay, pause |
| `DEADeyeResultsUI.cs` | `DEADeye` | 224 | Post-mission debriefing panel calculating letter grades (S, A, B, C, F) and accuracy stats |
| `DEADeyeTacticalTerminalUI.cs` | `DEADeye` | 822 | Full-screen terminal featuring 4 operations, personnel dossiers, and deployment launch |

### H. Environment & Level Generation (`Assets/DEADeye/Scripts/Environment/`)
| File | Namespace | Lines | Primary Responsibility |
|---|---|---|---|
| `DEADeyeDojoLobbyManager.cs` | `DEADeye` | 51 | Interior staging environment initialization |
| `DEADeyeEnvironmentalProp.cs` | `DEADeye` | 201 | Shootable props: Power Junction, Alarm Bell, Transformer, Security Gate |
| `DEADeyeUrbanCityBuilder.cs` | `DEADeye` | 2138 | Procedural generation of 4 urban districts, 3 overwatch perches, bridges, roads, props, NavMesh |

### I. Editor Utilities & Scene Builders (`Assets/DEADeye/Editor/` & `Assets/DEADeye/Scripts/Editor/`)
| File | Namespace | Lines | Primary Responsibility |
|---|---|---|---|
| `DEADeyeAndroidBuilder.cs` | `DEADeye.Editor` | 98 | Command-line batchmode build pipeline for standalone Quest 2 APK |
| `DEADeyeCharacterSetup.cs` | `DEADeye.Editor` | 387 | Character import processor and hitbox/ragdoll configurator |
| `DEADeyeCharacterSetupUtility.cs`| `DEADeye.Editor` | 284 | Automated rigging and prefab creation for Quaternius character assets |
| `DEADeyeMasterCampaignIntegrator.cs`| `DEADeye.Editor` | 215 | Integrates all managers, audio, UI, and lighting into the primary scene |
| `DEADeyeMasterSceneBuilder.cs` | `DEADeye.Editor` | 462 | Reconstructs the complete authoritative mission scene from scratch |
| `DEADeyeMissionSetupUtility.cs` | `DEADeye.Editor` | 228 | Configures ScriptableObject definitions for the 4 operations |
| `DEADeyeSceneInspector.cs` | `DEADeye.Editor` | 365 | Diagnostics utility dumping hierarchy, colliders, and XR components |
| `DEADeyeScreenshotCapture.cs` | `DEADeye.Editor` | 214 | Automated high-resolution in-engine screenshot recorder |
| `DEADeyeTMPSetup.cs` | `DEADeye.Editor` | 36 | Configures default TextMeshPro fonts and shaders |
| `FoundationSceneBuilder.cs` | `DEADeye.Editor` | 382 | Legacy baseline foundation scene builder |

### J. Automated Test Suites (`Assets/DEADeye/Tests/` & `Assets/Tests/`)
| File | Suite Type | Lines | Test Count | Description |
|---|---|---|---|---|
| `DEADeyeIntegrationTests.cs` | EditMode | 450 | 19 tests | Architecture, scene integrity, singleton validation, weapon rules |
| `DEADeyeAccuracyTests.cs` | PlayMode | 384 | 8 tests | Camera-centre targeting, headshots, wall obstruction, muzzle ray |
| `DEADeyeCampaignScreenshotSuite.cs`| PlayMode | 298 | 8 tests | Visual capture across 8 major mission milestones |
| `DEADeyePlayModeTests.cs` | PlayMode | 912 | 13 tests | Full gameplay loop, bridge traversal, reload cycle, fall recovery |
| `FoundationIntegrationTests.cs` | EditMode | 184 | 7 tests | Baseline foundation weapon and target tests |
| `FoundationPlayModeTests.cs` | PlayMode | 142 | 1 test | Baseline simulation run test |
| `FoundationTarget.cs` | Runtime | 94 | N/A | Mock damageable target for unit tests |
| `FoundationVerificationRunner.cs`| Runtime | 212 | N/A | Automated verification runner |
| `FoundationWeapon.cs` | Runtime | 158 | N/A | Mock weapon implementation for tests |

---

## 3. Scene Inventory

| Scene File Path | Purpose | Key Objects Present |
|---|---|---|
| `Assets/DEADeye/Scenes/RooftopMission.unity` | **Primary Authoritative Scene** | `DEADeye_PlayerController` (Desktop + VR), `CityEnvironment_Controller`, `Mission_AuthoritativeManagers`, `XM500_PrecisionSniper`, `DEADeye_MissionHUD`, `RooftopSafetyBoundary`, NavMeshSurface |
| `Assets/DEADeye/Scenes/BootScene.unity` | Startup Bootstrapper | `DEADeyeBootLoader`, initial resolution & audio settings |
| `Assets/DEADeye/Scenes/LobbyScene.unity` | Staging Dojo | `DEADeyeDojoLobbyManager`, `DEADeyeLobbyUI`, loadout preview |
| `Assets/Scenes/FoundationTest.unity` | Unit Test Sandbox | Barebones floor, test targets, mock weapon |
| `Assets/Samples/.../DemoScene.unity` | XRI Sample Scene | XR Interaction Toolkit reference assets |

---

## 4. 3D Model & Art Asset Inventory

| Asset Name | Source / Author | License / Type | Project Location | Role in DEADeye |
|---|---|---|---|---|
| `Superhero_Male_FullBody.fbx` | Quaternius Modular | Free / CC0 | `Assets/DEADeye/Art/Characters/Quaternius/Models/` | Commander Vance & Male Operatives |
| `Superhero_Female_FullBody.fbx`| Quaternius Modular | Free / CC0 | `Assets/DEADeye/Art/Characters/Quaternius/Models/` | Elena (Technician) & Female Hostiles |
| Hair Variants (`Beard`, `Buzzed`, `Parted`) | Quaternius | Free / CC0 | `Assets/DEADeye/Art/Characters/Quaternius/Models/` | Modular visual differentiation of hostiles |
| `model.dae` (Sniper Rifle) | Poly / External | Free Standard | `Assets/DEADeye/Art/Weapons/SniperRifle/source/` | High-fidelity XM500 sniper rifle model |
| `winchester-model-1887.fbx` | LowPolyGuns | Free Standard | `Assets/DEADeye/Art/Weapons/LowPolyGuns/` | Weapon showcase / test firearm |
| Dojo Environment Props | Dojo Pack | Free Standard | `Assets/DEADeye/Art/Environments/Dojo/` | Staging lobby visuals |
| Hit & Impact VFX | HitFolder / HitFolder2 | Unity Particle VFX | `Assets/DEADeye/Art/VFX/` | Concrete sparks, blood splatters, muzzle flash |

---

## 5. Audio Asset Inventory

All audio files located at `Assets/DEADeye/Audio/` (16-bit PCM WAV, 44,100 Hz):

| Filename | Type | In-Game Trigger |
|---|---|---|
| `Sniper_Gunshot_Close.wav` | Gunshot | Played at rifle muzzle position upon firing |
| `Sniper_Gunshot_DistantEcho.wav` | Reverb Tail | Layered urban slap-back echo across city buildings |
| `Sniper_Bolt_Cycle.wav` | Mechanical | Played during bolt-action cycle between shots |
| `Sniper_Reload_MagOut.wav` | Reload Part 1 | Empty magazine ejection |
| `Sniper_Reload_MagIn.wav` | Reload Part 2 | Fresh 5-round magazine insertion |
| `Sniper_Scope_Toggle.wav` | Optic | High-magnification scope lens engagement |
| `Impact_Concrete.wav` | Impact | Bullet strike on building facades, sidewalks, streets |
| `Impact_Metal.wav` | Impact | Bullet strike on Comms Tower, Jammer, or Power Junction |
| `Impact_Body.wav` | Impact | Bullet strike on hostile or NPC body |
| `Bullet_Flyby.wav` | Whiz-by | Supersonic bullet passing near environmental targets |

---

## 6. Installed Packages (`Packages/manifest.json`)

| Package Identifier | Version | Functional Purpose |
|---|---|---|
| `com.unity.render-pipelines.universal` | `17.3.0` | Universal Render Pipeline core (Forward+ / Single-Pass Instanced XR) |
| `com.unity.xr.openxr` | `1.16.1` | Khronos OpenXR runtime standard for Desktop & Quest 2 |
| `com.unity.xr.interaction.toolkit` | `3.4.0` | XR Origin, interactors, controller input, and locomotion |
| `com.unity.xr.management` | `4.5.4` | Lifecycle manager for XR loaders across standalone and Android |
| `com.unity.inputsystem` | `1.16.0` | Modern event-based Input System |
| `com.unity.ai.navigation` | `2.0.10` | NavMeshSurface, runtime NavMesh baking, and AI navigation |
| `com.unity.ugui` | `2.0.0` | Canvas, GraphicRaycaster, UI event routing |
| `com.unity.test-framework` | `1.6.0` | NUnit EditMode and PlayMode automated testing framework |
| `com.unity.cloud.gltfast` | `6.20.0` | Fast glTF/GLB runtime asset importer |
| `com.unity.timeline` | `1.8.10` | Cinematic cutscenes and timing control |

