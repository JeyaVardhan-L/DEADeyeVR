# DEADeye VR — Meta Quest 2 Standalone Setup & Installation Guide

This guide provides the direct, minimal steps to install and play **DEADeye** on your standalone Meta Quest 2 headset without needing a PC keyboard or mouse.

---

## 1. Prerequisites

1. **Meta Quest 2 Headset** in **Developer Mode**:
   - In the Meta Quest mobile app (phone): `Devices` &rarr; `Headset Settings` &rarr; `Developer Mode` &rarr; toggle **ON**.
2. **USB-C Cable** connecting Quest 2 to your PC.
3. Put on your headset and click **"Allow USB Debugging"** (check *Always allow from this computer*).

---

## 2. Installation Methods

The standalone APK is generated at:
```
C:\Unity\Editors\DEADeyeVR_Foundation\Builds\Android\DEADeye_Quest2.apk
```

### Method A: Single Command via ADB (Fastest)

Unity includes the official Android ADB tool. Run this single command in PowerShell:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" install -r -d "C:\Unity\Editors\DEADeyeVR_Foundation\Builds\Android\DEADeye_Quest2.apk"
```

*To verify your headset is connected before running:*
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe" devices -l
```

### Method B: Meta Quest Developer Hub (MQDH)

1. Launch **Meta Quest Developer Hub** on your PC.
2. Ensure your Quest 2 shows as **Connected** (green dot).
3. Open the **Device Manager** tab.
4. Drag and drop `DEADeye_Quest2.apk` directly into the installed apps list.

### Method C: SideQuest

1. Open **SideQuest** on PC.
2. Click the top-right toolbar icon: **"Install APK file from folder on computer"** (box with downward arrow).
3. Select `C:\Unity\Editors\DEADeyeVR_Foundation\Builds\Android\DEADeye_Quest2.apk`.

---

## 3. How to Launch on Quest 2

1. Put on your Meta Quest 2 headset.
2. In the Quest Universal Menu, open your **App Library** (grid icon).
3. In the top-right filter dropdown (which usually says *All* or *Installed*), select **Unknown Sources**.
4. Click **DEADeye** to launch.

---

## 4. Standalone Quest 2 Controls Reference

No PC keyboard or mouse is needed. All gameplay and navigation actions are mapped directly to your Touch Controllers:

| Controller | Input | In-Game Action |
|---|---|---|
| **Right Controller** | **Trigger** | **Fire Sniper Rifle** *(requires optical scope zoom)* / Select UI Button |
| **Right Controller** | **Grip (Hold)** | **Optical Scope Zoom** *(activates lens magnification and crosshairs)* |
| **Right Controller** | **Thumbstick** | **Snap Turn** (smooth/snap camera reorientation) |
| **Right Controller** | **Button A** | **Reload** (chamber fresh 5-round magazine) |
| **Right Controller** | **Button B** | **Toggle Mission Briefing / Intel Panel** |
| **Left Controller** | **Thumbstick** | **Smooth Locomotion Movement** (WASD rooftop traversal) |
| **Left Controller** | **Grip (Hold)** | **Secondary Scope Zoom Hold** |
| **Left Controller** | **Menu Button** | **Pause Game / Mission Menu** |
| **Left Controller** | **Button X** | **Reload** |
| **Left Controller** | **Button Y** | **Toggle Tactical Terminal** |

> [!NOTE]
> **Core Sniper Ballistics Rule**: Just like on desktop, the sniper rifle will strictly not fire when unscoped. Squeeze and hold **Grip** to look through the optical magnification scope, then pull **Trigger** to fire with pin-point accuracy. Pulling trigger while unscoped plays an audible dry-fire click.

---

## 5. Desktop Simulator Preservation

The desktop simulator remains 100% functional inside the Unity Editor and in desktop builds:
- **WASD**: Rooftop movement
- **Mouse Look**: Natural camera rotation (cursor locked, zero drift)
- **RMB (Hold)**: Optical Scope Zoom
- **LMB**: Fire Weapon (while scoped)
- **R**: Reload
- **Escape**: Pause
- **I**: Mission Info
- **Y**: Tactical Terminal

---

## 6. Physical Headset Verification Checklist

While the OpenXR runtime, Touch Controller bindings, weapon socketing, and ARM64 APK build are automated and verified, the following items require physical verification with the headset on your head:

- [ ] **Stereoscopic 3D Depth & IPD Feel**: Verify visual clarity and scale of city skyscrapers from the rooftop perch.
- [ ] **Controller Grip Alignment**: Confirm that the rifle stock rests naturally in your dominant hand.
- [ ] **Optical Scope Alignment**: Check comfort of holding Grip while lining up long-distance targets across the city boulevard.
- [ ] **Frame Rate & Comfort**: Confirm steady 72Hz / 90Hz frame timing on Quest 2 during intense firefights.

