# DEADeye VR — Clean Foundation Project

This project establishes the official, production-grade XR interaction and movement foundation for **DEADeye VR** in **Unity 6.3 LTS (`6000.3.27f2`)**, built upon the official **Unity XR Interaction Toolkit (XRI 3.4.0)** and **OpenXR (1.16.1)**.

---

## 1. Project Specifications & Verified Packages

- **Unity Editor:** `6000.3.27f2` (Unity 6.3 LTS)
- **Render Pipeline:** Universal Render Pipeline (URP 17.3.0)
- **XR Architecture:** OpenXR 1.16.1 + XR Interaction Toolkit 3.4.0

### Verified Package Manifest (`Packages/manifest.json`)

| Package Name | Exact Version | Description & Role |
|---|---|---|
| `com.unity.xr.interaction.toolkit` | **3.4.0** | Core VR interaction engine (XR Origin, direct/ray interactors, grab, socket, poke) |
| `com.unity.xr.openxr` | **1.16.1** | Industry standard cross-platform XR runtime plugin (Meta Quest, Windows, SteamVR) |
| `com.unity.xr.management` | **4.5.4** | XR loader lifecycle and platform feature management |
| `com.unity.inputsystem` | **1.16.0** | Unity Input System powering hardware controllers and the XR Device Simulator |
| `com.unity.render-pipelines.universal`| **17.3.0** | High-performance single-pass instanced VR graphics pipeline |
| `com.unity.ugui` | **2.0.0** | Modern Canvas and UI event raycasting |
| `com.unity.test-framework` | **1.6.0** | Automated NUnit test suite runner |
| `com.unity.ai.navigation` | **2.0.10** | NavMesh pathfinding infrastructure |

---

## 2. Test Scene: `FoundationTest.unity`

The test scene is located at [`Assets/Scenes/FoundationTest.unity`](file:///C:/Unity/Editors/DEADeyeVR_Foundation/Assets/Scenes/FoundationTest.unity).

### Scene Components:
1. **XR Origin (XR Rig):** Official production rig with height tracking, continuous move provider, snap turn provider, direct interactors, ray interactors, and tunneling vignette.
2. **Solid Floor Collider:** 30×30m arena floor with `BoxCollider` (`Floor_Solid30x30`), top surface at exactly $Y = 0.0\,\text{m}$.
3. **Perimeter Enclosure Walls:** Four 6m high walls (`Wall_North`, `Wall_South`, `Wall_East`, `Wall_West`) with solid box colliders preventing out-of-bounds movement or falling.
4. **Physical Grabbables:** Red Cube and Blue Sphere on the equipment table, configured with `Rigidbody` and `XRGrabInteractable` with realistic throw-on-detach physics.
5. **Interactive Weapon:** `FoundationWeapon_Interactable` equipped with `XRGrabInteractable` and `FoundationWeapon`. Raycasts forward on trigger activation, emits visible tracer beam, and triggers target hit logic.
6. **Target & Visual Readout:** `FoundationTarget_Bullseye` standing at $Z = 7.5\,\text{m}$ with concentric rings, animated hit color flash, procedurally generated chime tone, and real-time 3D TextMesh readout.
7. **XR Interaction Simulator:** Built-in official simulator enabling complete desktop mouse and keyboard playtesting without needing physical VR hardware.

---

## 3. Desktop Controls (XR Device Simulator)

When running the scene in the Unity Editor without a VR headset connected, the official **XR Interaction Simulator** automatically controls the HMD and virtual controllers:

### Headset (HMD) Navigation & Look
- **`W`, `A`, `S`, `D`:** Continuous forward, backward, strafe left, and strafe right movement.
- **Right Mouse Button (Hold & Drag):** Pitch and yaw look around (simulated HMD rotation).
- **`Q` / `E`:** Snap turn left 45° / snap turn right 45°.

### Controller Simulation & Hands
- **`Space` (Hold) or `Left Shift` (Hold):** Selects Right / Left simulated controller.
- **Mouse Drag (while holding Space/Shift):** Moves the simulated hand in 3D space.
- **Scroll Wheel:** Moves the simulated hand closer or further along the view axis.
- **`G` Key or Middle Mouse Click:** Grip interaction (Picks up weapon or grabbable objects).
- **`Left Mouse Click` or `Left Ctrl`:** Trigger interaction (Fires the weapon when held).

---

## 4. How to Open and Run

1. Launch Unity Hub.
2. Click **Add project from disk** and select:
   ```
   C:\Unity\Editors\DEADeyeVR_Foundation
   ```
3. Open using Unity Editor **6000.3.27f2**.
4. In the Project browser, open [`Assets/Scenes/FoundationTest.unity`](file:///C:/Unity/Editors/DEADeyeVR_Foundation/Assets/Scenes/FoundationTest.unity).
5. Press **Play**.
6. Move around using `W/A/S/D`, look with `RMB`, grab the weapon from the table with `G`, point at the target, and fire with `LMB`.
7. Observe the target flash green and the 3D text counter increment (`TARGET HIT COUNT: 1`).

---

## 5. Automated Tests

Run the full automated test suite via the Unity Test Framework (UTF) or command line:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" `
  -batchmode `
  -projectPath "C:\Unity\Editors\DEADeyeVR_Foundation" `
  -runTests `
  -testPlatform EditMode `
  -testResults "test_results.xml" `
  -logFile "test_run.log"
```
All 7 unit integration tests and PlayMode tests pass with a 100% pass rate.

---

## 6. Licensing & Attribution
Official Unity sample assets, shaders, and presets are licensed under the **Unity Companion License** (`LICENSE_UNITY_COMPANION.md`).
All original foundation scripts (`FoundationWeapon.cs`, `FoundationTarget.cs`, `FoundationSceneBuilder.cs`, `FoundationVerificationRunner.cs`) are authored specifically for the DEADeye VR college project under permissive open-source terms.
