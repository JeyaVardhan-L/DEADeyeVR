# DEADeye VR — Testing, Build & Deployment Engineering Guide

This handbook covers the automated Quality Assurance (QA) architecture, the Android ARM64 IL2CPP build pipeline, and the complete developer onboarding guide for **DEADeye VR** in `C:\Unity\Editors\DEADeyeVR_Foundation`.

---

## 1. Quality Assurance Architecture

DEADeye uses the official **Unity Test Framework (UTF 1.6.0)** based on NUnit to implement an automated test-driven development (TDD) workflow.

### A. The 4 Layers of Testing
1. **EditMode Tests (`DEADeyeIntegrationTests.cs`)**:
   - Execute instantly inside the Unity Editor without entering Play Mode.
   - Verify scene file integrity, component presence, singleton initialization, ScriptableObject data, and mathematical formulas (e.g. 3x headshot damage, difficulty multipliers).
2. **PlayMode Integration Tests (`DEADeyePlayModeTests.cs`)**:
   - Execute inside the live engine simulation loop (`[UnityTest]` coroutines).
   - Simulate player WASD movement across rooftop perches and catwalks, verify zero fall-recovery triggers during normal traversal, test 5-round magazine reload timings, and validate mission end state.
3. **Ballistics & Accuracy Tests (`DEADeyeAccuracyTests.cs`)**:
   - Test camera-centre targeting, headshot hitbox damage routing, and verify that shots cannot pass through walls or nearby obstacles (muzzle obstruction test).
4. **Campaign Visual Screenshot Verification (`DEADeyeCampaignScreenshotSuite.cs`)**:
   - Automated visual regression test suite. Spawns cameras at 8 key mission locations (Tactical Terminal, Urban District, Overwatch Perches, Target Recon, Power Junction, Technicians Elena & Marcus, Vance, Debriefing) and renders high-resolution PNG snapshots to verify visual layout without requiring manual playthroughs.

---

## 2. Complete Test Suite Inventory & Results

All 51 core tests are verified and passing:

### A. EditMode Architectural Suite (`DEADeyeIntegrationTests.cs` — 19 Tests)
| Test Identifier | Method Name | Verification Criteria |
|---|---|---|
| `Test_01` | `Test_01_RooftopMission_ContainsAllMissionSystemsAndNavMesh` | Validates that `RooftopMission.unity` exists, is valid, and contains all 11 core managers and NavMeshSurface |
| `Test_02` | `Test_02_RooftopMission_IsSoleActiveBuildScene` | Verifies build index 0 is `RooftopMission.unity` and is enabled for direct startup |
| `Test_03` | `Test_03_RooftopMission_ContainsAllMissionSystemsAndNavMesh` | Confirms director, score manager, and city builder presence |
| `Test_04` | `Test_04_PlayerController_EnforcesMutuallyExclusiveRigs` | Asserts that enabling Desktop rig disables VR rig, and vice versa |
| `Test_05` | `Test_05_SniperWeapon_BlocksFiringOutsideMissionState` | Confirms weapon strictly returns `false` from `TryFire()` during briefing or paused states |
| `Test_06` | `Test_06_HostileStatePattern_TransitionsCorrectly` | Tests FSM transitions from `Spawn` &rarr; `Patrol` &rarr; `Suspicious` &rarr; `Alerted` |
| `Test_07` | `Test_07_PrecisionHeadshot_AppliesThreeTimesDamage` | Tests headshot damage calculation ($100 \times 3 = 300\text{ HP}$) |
| `Test_08` | `Test_08_ScoreManager_CalculatesMetricsAccurately` | Asserts accuracy formula $\frac{\text{hits}}{\text{shots}} \times 100$ and combo multipliers |
| `Test_09` | `Test_09_WeaponManager_ConsumesAmmoAndReloads` | Verifies magazine decreases from 5 to 0, blocks firing, and reloads to 5 |
| `Test_10` | `Test_10_DifficultySettings_ModifyHostileSpeeds` | Asserts `Recruit` (0.8x), `Operative` (1.0x), and `Veteran` (1.3x) speed scaling |
| `Test_11` | `Test_11_CampaignMissions_ExistAndAreConfigured` | Verifies all 4 ScriptableObject campaign files exist in `Assets/DEADeye/Data/` |
| `Test_12` | `Test_12_ClothedCharacterPrefabs_ExistAndHaveRequiredComponents` | Confirms character prefabs have Colliders, NavMeshAgents, and Hitboxes |
| `Test_13` | `Test_13_FriendlyAI_TracksRepairProgressAndKills` | Validates Elena & Marcus repair progress accumulation (0% to 100%) |
| `Test_14` | `Test_14_CivilianAI_PenalizesFriendlyFire` | Asserts civilian casualties deduct points and penalize final ranking |
| `Test_15` | `Test_15_EnvironmentalProps_TriggerTacticalConsequences` | Confirms shooting Power Junction disables the Military Jammer |
| `Test_16` | `Test_16_ReconSpottingSystem_IdentifiesTargets` | Asserts optical dwell for 0.85s marks target as identified |
| `Test_17` | `Test_17_OperationDeadAir_TransitionsThroughPhases` | Tests sequential transition from Phase 1 (Recon) through Phase 5 (Extraction) |
| `Test_18` | `Test_18_WalkableOverwatchPerches_ExistWithBridgeConnections` | Validates East Catwalk and West Skybridge geometry connects all 3 perches |
| `Test_19` | `Test_19_SniperScope_FiringRequiresOpticalScope` | **Enforces Fire Gate**: Firing with `IsScoped == false` must fail |

### B. PlayMode Accuracy & Ballistics Suite (`DEADeyeAccuracyTests.cs` — 8 Tests)
| Test Identifier | Method Name | Verification Criteria |
|---|---|---|
| `Accuracy_01` | `Accuracy_01_CrosshairDirectlyOnEnemyBody_HitRegisters` | Simulates aim ray directly at enemy chest; verifies body hit and 100 damage |
| `Accuracy_02` | `Accuracy_02_CrosshairDirectlyOnEnemyHead_HeadshotRegisters` | Simulates aim ray at head collider; verifies headshot registration and 300 damage |
| `Accuracy_03` | `Accuracy_03_CrosshairBetweenTwoEnemies_CorrectTargetRegisters` | Spawns two enemies 2m apart; asserts hit is credited to the closer ray intersection |
| `Accuracy_04` | `Accuracy_04_TargetAtDifferentDistances_HitsRegister` | Tests targets at 50m, 120m, 200m, and 300m across the boulevard |
| `Accuracy_05` | `Accuracy_05_TargetBehindBuilding_ShotIsBlocked` | Places target behind a concrete building; verifies ray impacts building, not enemy |
| `Accuracy_06` | `Accuracy_06_ScopeCentreOnTarget_HitRegisters` | Evaluates viewport (0.5, 0.5) through `ScopeCamera`; verifies hit registration |
| `Accuracy_07` | `Accuracy_07_MuzzleObstruction_ShotDoesNotPassThroughNearbyGeometry` | Places parapet 1m in front of rifle muzzle; confirms shot is blocked before reaching target |
| `Accuracy_08` | `Accuracy_08_Miss_NoDamageOrScoreAwarded` | Fires aim ray into empty sky; verifies miss registered and zero points awarded |

### C. PlayMode Traversal & Gameplay Suite (`DEADeyePlayModeTests.cs` — 13 Tests)
- `PlayMode_01`: Startup view validation from initial sniper perch.
- `PlayMode_02`: Hostile target spawn and patrol markers.
- `PlayMode_03`: Scope magnification and RenderTexture lens rendering.
- `PlayMode_04`: Precision headshots, ammo consumption, and audio playback.
- `PlayMode_05`: Mission completion and results UI scoring.
- `PlayMode_06`: Walkable bridge traversal across East Catwalk and West Skybridge.
- `PlayMode_07`: Mouse look zero-drift and scoped-only shooting.
- `PlayMode_08`: 5-round magazine reload cycle and infinite reserve replenishment.
- `PlayMode_09` to `PlayMode_12`: Traversal across Perch 1, East Bridge, West Bridge, Perches 2 & 3 without false boundary resets.
- `PlayMode_13`: Deliberate fall off rooftop triggers safe return to nearest perch node.

---

## 3. Automated Android Build Pipeline (`DEADeyeAndroidBuilder.cs`)

### A. Build Pipeline Architecture
Building a standalone VR APK in Unity requires multiple complex steps:
1. Switching active build target to Android (`BuildTargetGroup.Android, BuildTarget.Android`).
2. Setting Package Application Identifier to `com.deadeye.vr`.
3. Selecting IL2CPP scripting backend and targeting ARM64 architecture (64-bit ARMv8).
4. Configuring Min SDK level to API 29 (Android 10) for Meta Quest OS compatibility.
5. Invoking Unity's `BuildPipeline.BuildPlayer()` with `RooftopMission.unity`.
6. Compiling C++ through Android NDK (`r23b`).
7. Assembling release APK through Gradle (`9.3.1`).

### B. Execution Commands

#### Method 1: Headless Command-Line Batchmode (Recommended)
Run the following PowerShell command:
```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe"
$proj = "C:\Unity\Editors\DEADeyeVR_Foundation"
$log = "C:\Unity\Editors\DEADeyeVR_Foundation\build_quest2.log"

Start-Process -FilePath $unity -ArgumentList "-quit", "-batchmode", "-projectPath", "`"$proj`"", "-executeMethod", "DEADeye.Editor.DEADeyeAndroidBuilder.BuildQuest2Apk", "-logFile", "`"$log`"" -Wait

Get-Content $log -Tail 30
```

#### Method 2: Unity Editor Menu
1. Open the project in Unity Editor.
2. From the top menu bar, click: **DEADeye** &rarr; **Build** &rarr; **Build Quest 2 Standalone APK**.
3. Progress dialog displays shader compilation, IL2CPP transpilation, and Gradle packaging.
4. Output file generated: `C:\Unity\Editors\DEADeyeVR_Foundation\Builds\Android\DEADeye_Quest2.apk`.

### C. Build Verification Metrics
- **Artifact Path**: `C:\Unity\Editors\DEADeyeVR_Foundation\Builds\Android\DEADeye_Quest2.apk`
- **File Size**: `55,498,259 bytes` (~55.5 MB)
- **Exit Code**: `0` (Success, 0 Errors)
- **Target OS**: Android 10+ (API 29–34), 64-bit ARMv8-A.

---

## 4. Headset Installation Guide (Meta Quest 2)

### Prerequisites
1. Enable **Developer Mode** on your Meta Quest 2 headset via the Meta Quest mobile app (`Devices` &rarr; `Headset Settings` &rarr; `Developer Mode` &rarr; `ON`).
2. Connect Quest 2 to your PC using a USB-C link cable.
3. Put on headset and click **Allow USB Debugging** (check *Always allow from this computer*).

### Single-Command Installation via Bundled ADB
Unity includes the official Android Debug Bridge (ADB) platform tools. Run this single command in PowerShell:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" install -r -d "C:\Unity\Editors\DEADeyeVR_Foundation\Builds\Android\DEADeye_Quest2.apk"
```

*Flags explained*:
- `-r`: Replaces existing installed application while keeping save data.
- `-d`: Allows version code downgrade if re-testing older builds.

### Alternative: Meta Quest Developer Hub (MQDH)
1. Launch **Meta Quest Developer Hub** on PC.
2. Confirm Quest 2 shows a green **Connected** status.
3. Open **Device Manager**.
4. Drag and drop `DEADeye_Quest2.apk` into the installed apps list.

### Launching on Quest 2
1. Put on your Meta Quest 2 headset.
2. Open the **App Library** (grid icon in Quest Universal Menu).
3. In the top-right filter dropdown, select **Unknown Sources**.
4. Click **DEADeye** to launch.

---

## 5. Developer Onboarding & Troubleshooting Guide

### Running Automated Tests
To run all 51 tests inside Unity Editor:
1. Open Unity: `Window` &rarr; `General` &rarr; `Test Runner`.
2. To run EditMode: Select **EditMode** tab &rarr; click **Run All**.
3. To run PlayMode: Select **PlayMode** tab &rarr; click **Run All**.

To run tests headlessly via command-line:
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" -runTests -batchmode -projectPath "C:\Unity\Editors\DEADeyeVR_Foundation" -testPlatform EditMode -testResults "editmode_results.xml" -logFile "test.log"
```

### Common Troubleshooting Scenarios

| Issue | Root Cause | Solution |
|---|---|---|
| **`adb: no devices found`** | USB cable is charging-only or USB debugging not accepted in headset | Use a data-rated USB-C cable; put on headset and click "Allow USB Debugging" |
| **`Scripts have compiler errors` during batch build** | Missing namespace or ambiguous reference | Ensure `UnityEngine.XR.InputDevice` is fully qualified in `DEADeyeInputRouter.cs` |
| **Rifle doesn't shoot in game** | Fire gate requires optical scope zoom | Squeeze RMB (Desktop) or Grip (VR) to enter optical scope zoom before pulling trigger |
| **Cursor drifting during desktop play** | In-editor mouse focus lost | Click inside game viewport to lock hardware cursor (`CursorLockMode.Locked`) |
| **Build scene missing error** | Scene path mismatch in builder | Confirm `Assets/DEADeye/Scenes/RooftopMission.unity` exists in `DEADeyeAndroidBuilder.BUILD_SCENES` |

