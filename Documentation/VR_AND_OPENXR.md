# DEADeye — Meta Quest 2 & OpenXR Engineering Manual

```
========================================================================================
MODULE       : Standalone Mobile VR, OpenXR 1.16.1 & Headset Tracking Architecture
TARGET       : Meta Quest 2, Meta Quest 3, Meta Quest Pro (Android ARM64)
ENGINE       : Unity 6.3 LTS (6000.3.27f2)
SOURCE PATHS : Assets/DEADeye/Scripts/Player/, Assets/DEADeye/Scripts/Core/
PACKAGE STACK: com.unity.xr.openxr (1.16.1), com.unity.xr.interaction.toolkit (3.4.0)
========================================================================================
```

---

## 1. XR Technology Stack & Runtime Configuration

DEADeye targets standalone Meta Quest hardware using an open standard architecture based on Khronos OpenXR rather than proprietary vendor SDKs:

```
[ Unity 6.3 LTS Application Layer ]
                │
                ▼
[ XR Interaction Toolkit 3.4.0 (XRI) ]
                │
                ▼
[ Unity OpenXR Plugin 1.16.1 ]
                │
                ▼
[ Khronos OpenXR Loader (libopenxr_loader.so) ]
                │
                ▼
[ Meta Quest OS / Oculus OpenXR Runtime ]
                │
                ▼
[ Snapdragon XR2 Gen 1 Hardware (Meta Quest 2) ]
```

### Core Graphics & XR Settings
- **Stereo Rendering Mode**: **Single-Pass Instanced (SPI)**.
  - Both left and right eye viewports are submitted to the GPU in a single draw call invocation using texture arrays (`SV_RenderTargetArrayIndex`), reducing CPU draw-call overhead by approximately $45\%$.
- **Render Pipeline**: Universal Render Pipeline (URP `17.3.0`) utilizing the **Forward+** cluster renderer.
- **Color Space**: Linear color space with ASTC $6\times 6$ block texture compression.
- **Target Frame Rate**: $72\text{ Hz}$ standard Quest 2 refresh rate.

---

## 2. XR Origin, Head Tracking & Camera Architecture

The VR player rig is instantiated from `Assets/DEADeye/Prefabs/VR_XROriginRig.prefab` based on the Unity XR CoreUtils `XROrigin`:

```
VR_XROriginRig (Root Transform | Coordinates: 0, 26, -2.5)
│
├── Camera Offset (Tracking Origin Compensator | Local Y: 0.0m in Floor Mode)
│   │
│   ├── Main Camera (Tagged MainCamera | Has TrackedPoseDriver & URP XR Data)
│   │
│   ├── Left Controller (TrackedPoseDriver | Hand Model | Locomotion Provider)
│   │
│   └── Right Controller (TrackedPoseDriver | Hand Model)
│       └── VRWeaponSocket (Rifle Grip Socket | Local: 0, -0.04, 0.12)
│           └── XM500_PrecisionSniper (Physical Rifle Instance)
```

### The Floor-Level Viewpoint & Head Rotation Fix
During physical Meta Quest 2 testing, Touch controller movement and triggers functioned properly, but two critical issues were observed:
1. The camera viewpoint appeared near floor level.
2. Turning the physical head did not rotate the game view.

#### Root Cause Analysis
1. **Disabled Action Reference in `TrackedPoseDriver`**:
   The `TrackedPoseDriver` on `Main Camera` referenced external Input Actions (`XRI Head`), with `m_UseReference = 1`. In Unity's Input System, `TrackedPoseDriver` does not enable referenced actions itself. With no active `InputActionManager` binding the `XRI Head` action map, `m_TrackingStateInput` evaluated to disabled.
2. **Pose Invalidation**:
   In `TrackedPoseDriver.ReadTrackingState()`, when the tracking state action is disabled, `m_CurrentTrackingState` defaults to `TrackingStates.None`. Because `m_IgnoreTrackingState` was `false`, both `positionValid` and `rotationValid` evaluated to `false` every frame, discarding all HMD orientation and position updates.
3. **Floor Mode Offset Zeroing**:
   On Quest OpenXR, `inputSubsystem.GetTrackingOriginMode()` returns `TrackingOriginModeFlags.Floor`. In Floor mode, `XROrigin.MoveOffsetHeight(0f)` lowers `Camera Offset` to local $Y = 0$, expecting the HMD's tracked pose to provide the standing height ($~1.65\text{ m}$). Because `TrackedPoseDriver` discarded position updates, the camera remained at $(0, 0, 0)$—placing the operative at ground level.

#### The Multi-Layered Targeted Resolution
To permanently resolve this with zero architectural churn, a multi-tiered fail-safe was implemented:

1. **`TrackedPoseDriver` Configuration**:
   - `vrTrackedPoseDriver.ignoreTrackingState = true` (ensures updates are never discarded if tracking states report `None`).
   - Explicitly calls `Enable()` on `positionInput.action`, `rotationInput.action`, and `trackingStateInput.action`.
2. **Direct Hardware Subsystem Polling**:
   - In `DEADeyePlayerController.cs`, `UpdateVRHeadTracking()` directly queries the low-level OpenXR device node:
     ```csharp
     InputDevice centerEye = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
     if (!centerEye.isValid) centerEye = InputDevices.GetDeviceAtXRNode(XRNode.Head);

     if (centerEye.isValid)
     {
         if (centerEye.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion headRot) ||
             centerEye.TryGetFeatureValue(CommonUsages.deviceRotation, out headRot))
         {
             if (headRot != Quaternion.identity) vrCamera.transform.localRotation = headRot;
         }

         if (centerEye.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 headPos) ||
             centerEye.TryGetFeatureValue(CommonUsages.devicePosition, out headPos))
         {
             if (headPos.sqrMagnitude > 0.0001f) vrCamera.transform.localPosition = headPos;
         }
     }
     ```
   - This bypasses Input Action asset binding latencies entirely, utilizing the exact same proven hardware querying method that powers Touch controllers in `DEADeyeInputRouter.cs`.
3. **Automated Standing Eye Height Safeguard**:
   - To ensure the operative is never trapped at floor level (e.g., if playing seated or if Guardian floor calibration is offset):
     ```csharp
     if (vrCameraOffset != null)
     {
         float currentTotalHeight = vrCameraOffset.localPosition.y + vrCamera.transform.localPosition.y;
         if (currentTotalHeight < 1.3f)
         {
             Vector3 offsetPos = vrCameraOffset.localPosition;
             offsetPos.y = Mathf.Max(0f, 1.7f - vrCamera.transform.localPosition.y);
             vrCameraOffset.localPosition = offsetPos;
         }
     }
     ```
   - If total eye elevation is below $1.3\text{ m}$, `Camera Offset` automatically elevates to ensure the operative stands naturally at $1.7\text{ m}$ eye level overlooking the city.

---

## 3. Touch Controller Input Pipeline

`DEADeyeInputRouter.cs` queries Meta Quest Touch controllers via direct OpenXR common usages:

```csharp
InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
InputDevice leftHand  = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

// 1. Right Trigger -> Fire Weapon
bool curTrigger = false;
if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out curTrigger))
{
    vrTriggerDown = curTrigger && !prevRightTrigger;
    vrTriggerHeld = curTrigger;
    prevRightTrigger = curTrigger;
}

// 2. Right Grip -> Hold Optical Scope
bool curGrip = false;
if (rightHand.isValid && rightHand.TryGetFeatureValue(CommonUsages.gripButton, out curGrip))
{
    vrGripHeld = curGrip;
}

// 3. Thumbstick -> Locomotion Movement
if (leftHand.isValid && leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 lAxis))
{
    if (lAxis.sqrMagnitude > 0.04f) vrMoveInput = lAxis;
}
```

---

## 4. Standalone Quest 2 Build & Optimization Profile

- **Scripting Backend**: IL2CPP / ARM64
- **Minimum Android API**: API Level 29 (Android 10.0)
- **Target Android API**: Automatic (Highest installed SDK)
- **Android Manifest Requirements**:
  ```xml
  <uses-feature android:name="android.hardware.vr.headtracking" android:required="true" android:version="1" />
  <category android:name="com.oculus.intent.category.VR" />
  ```
- **Compiled Package**: `Builds/Android/DEADeye_Quest2.apk` ($55.5\text{ MB}$).
