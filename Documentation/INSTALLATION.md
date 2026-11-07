# DEADeye — Installation, Setup & Deployment Guide

```
========================================================================================
MODULE       : Installation, Environment Configuration & Build Pipelines
ENGINE       : Unity 6.3 LTS (6000.3.27f2)
TARGETS      : Windows x64 Desktop & Meta Quest 2 Standalone VR (Android ARM64)
AUTHORITATIVE: C:\Unity\Editors\DEADeyeVR_Foundation
REPOSITORY   : https://github.com/JeyaVardhan-L/DEADeyeVR.git
========================================================================================
```

---

## 1. Prerequisites & System Requirements

### Development Workstation Requirements
- **Operating System**: Windows 10 / 11 (64-bit).
- **Unity Engine**: **Unity 6.3 LTS (`6000.3.27f2`)** installed via Unity Hub.
- **Unity Hub Modules Required**:
  - `Windows Build Support (IL2CPP)`
  - `Android Build Support`
  - `Android SDK & NDK Tools`
  - `OpenJDK`
- **Git**: Git 2.30+ installed with Git LFS (if large assets are enabled).

### Target Hardware Compatibility
- **Desktop Simulator**: DirectX 11 / DirectX 12 compatible GPU (NVIDIA GTX 1060 / AMD RX 580 or higher).
- **VR Headset**: Meta Quest 2, Meta Quest 3, or Meta Quest Pro running Meta Quest OS v55+.

---

## 2. Cloning & Project Initialization

### 1. Clone the Authoritative Repository
```bash
git clone https://github.com/JeyaVardhan-L/DEADeyeVR.git
cd DEADeyeVR
```

### 2. Open via Unity Hub
1. Launch **Unity Hub**.
2. Click **Add** $\rightarrow$ **Add project from disk**.
3. Select the root project folder: `C:\Unity\Editors\DEADeyeVR_Foundation` (or your cloned directory).
4. Verify the assigned Editor Version is **`6000.3.27f2`**.
5. Click the project name to launch. Unity will automatically resolve all package dependencies from `Packages/manifest.json`.

---

## 3. Running the Desktop Simulator

The Desktop Simulator allows full gameplay evaluation using standard PC keyboard and mouse without requiring a physical VR headset:

1. In the Unity Project window, navigate to:
   ```
   Assets/DEADeye/Scenes/RooftopMission.unity
   ```
2. Double-click to open `RooftopMission.unity`.
3. Press the **Play** button in the Unity Editor toolbar.
4. Left-click anywhere in the **Game** viewport to lock the cursor.
5. You are immediately placed into the rooftop overwatch nest.
   - Use **WASD** to traverse catwalks.
   - Hold **Right Mouse Button** to engage the optical scope.
   - Click **Left Mouse Button** while scoped to fire.
   - Press **R** to reload; press **Escape** to pause.

---

## 4. Building the Standalone Meta Quest 2 APK

DEADeye includes an automated headless build pipeline (`DEADeyeAndroidBuilder.cs`) that configures IL2CPP, ARM64 architecture, and OpenXR settings:

### Option A: From Unity Editor UI
1. In the top Unity menu bar, select:
   ```
   DEADeye -> Build -> Build Quest 2 Standalone APK
   ```
2. The automated build pipeline will validate the scene, configure Android player settings, invoke IL2CPP, and output the compiled package to:
   ```
   Builds/Android/DEADeye_Quest2.apk
   ```

### Option B: Headless Command Line (Batchmode)
Run this command from PowerShell to compile without opening the Unity Editor GUI:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "C:\Unity\Editors\DEADeyeVR_Foundation" `
  -executeMethod "DEADeye.Editor.DEADeyeAndroidBuilder.BuildQuest2Apk" `
  -logFile "build_quest2.log"
```

*Build duration is approximately 3–4 minutes during clean compilation.*

---

## 5. Sideloading onto Meta Quest 2 Hardware

### Method 1: Direct Android Debug Bridge (ADB)
Ensure Developer Mode is enabled on your Meta Quest headset:

1. Connect the headset to your PC using a high-quality USB-C cable.
2. Put on the headset and select **"Always allow from this computer"** when prompted for USB debugging.
3. Open a terminal and run:
   ```powershell
   & "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" install -r "Builds/Android/DEADeye_Quest2.apk"
   ```
4. Once `Success` is reported, put on the headset.
5. In Quest Home, navigate to **App Library** $\rightarrow$ select the dropdown filter at top-right $\rightarrow$ choose **Unknown Sources**.
6. Select **DEADeye** to launch.

### Method 2: Meta Quest Developer Hub (MQDH)
1. Launch **Meta Quest Developer Hub**.
2. Connect your headset and ensure it appears as **Connected**.
3. Select **Device Manager** $\rightarrow$ **Apps**.
4. Drag and drop `Builds/Android/DEADeye_Quest2.apk` into the installation pane.
5. MQDH will automatically install and verify the package.
