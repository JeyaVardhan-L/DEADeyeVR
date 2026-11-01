# DEADeye VR — Foundation Architecture Specification

## 1. Architectural Overview

The **DEADeye VR Foundation** provides a robust, decoupled, and standard-compliant XR gameplay base implemented on **Unity 6.3 LTS (`6000.3.27f2`)** using the **XR Interaction Toolkit (XRI 3.4.0)** and **OpenXR (1.16.1)**.

### Architectural Diagram

```
+----------------------------------------------------------------------------------------------------+
|                                    INPUT & HARDWARE LAYER                                          |
|  - Meta Quest 2 / Touch Controllers (OpenXR Runtime Profile)                                       |
|  - Unity XR Device Simulator (Desktop Keyboard & Mouse Emulator: WASD, RMB, Space/Shift, LMB/Grip)  |
+----------------------------------------------------------------------------------------------------+
                                                  |
                                                  v
+----------------------------------------------------------------------------------------------------+
|                                    XR ORIGIN RIG INFRASTRUCTURE                                    |
|  - XROrigin (Tracking Origin Mode: Floor, Y=0.0m datum)                                            |
|  - Dynamic Move Provider (Continuous Movement + Character Controller Physics Obstacle Collision)    |
|  - Snap Turn Provider (Comfort-rated 45° step turning)                                             |
|  - Tunneling Vignette (Dynamic visual field restriction during rapid angular/linear acceleration)   |
|  - Left/Right Controller Interactors: Direct (Physics Grab), Ray (Far Interaction), Poke (UI)      |
+----------------------------------------------------------------------------------------------------+
                                                  |
                    +-----------------------------+-----------------------------+
                    |                                                           |
                    v                                                           v
+-----------------------------------------------+       +-----------------------------------------------+
|         PHYSICS & BOUNDARY SYSTEM             |       |            XR INTERACTION PIPELINE            |
| - Floor Collider (Top surface at Y=0.0m)      |       | - XRGrabInteractable (Physics Grab & Throw)   |
| - Perimeter Wall Colliders (Solid 6m bounds)  |       | - Activate Events (Trigger down / LMB)        |
| - Continuous Collision Detection (Rigidbodies)|       | - FoundationWeapon (Raycast Ballistics)       |
| - Downward Ground-Snap / Gravity Clamp        |       | - FoundationTarget (Hit Registration & Color) |
+-----------------------------------------------+       +-----------------------------------------------+
```

---

## 2. Core Subsystems

### 2.1 XR Origin & Locomotion Setup
- **Tracking Datum:** Configured to `Floor` mode. In desktop simulation or when no physical HMD tracking offset is active, the camera automatically aligns to nominal eye level ($Y = 1.36\,\text{m}$ to $1.65\,\text{m}$) above the solid collider surface ($Y = 0.0\,\text{m}$), completely eliminating the "player sinking below the floor" defect.
- **Locomotion Mediators:**
  - `ContinuousMoveProvider`: Bound to left thumbstick (`Primary2DAxis`) / `W,A,S,D` keys. Moves the player horizontally while respecting `CharacterController` slope limits and physics obstacles.
  - `SnapTurnProvider`: Bound to right thumbstick (`Primary2DAxis`) / `Q,E` keys. Rotates the rig in discrete 45° increments without motion sickness.
  - `TunnelingVignette`: Automatically darkens peripheral vision during movement, adhering to Meta Quest comfort guidelines.

### 2.2 Boundary Enforcement & Grounding
- **Solid Floor:** 30×30m box collider (`Floor_Solid30x30`) with an explicit top surface at $Y = 0.0\,\text{m}$.
- **Solid Perimeter Walls:** Four 6m high walls (`Wall_North`, `Wall_South`, `Wall_East`, `Wall_West`) with positive dimensions ($30\text{m} \times 6\text{m} \times 1\text{m}$) surrounding the perimeter. Horizontal raycasts in all four directions verify that colliders reliably obstruct movement.

### 2.3 XR Interaction Pipeline (Grabbables & Weapon)
- **Object Pickup & Release:** Both the test cubes, spheres, and weapon use standard `XRGrabInteractable`.
  - `throwOnDetach = true`: Preserves controller release velocity for physical throwing.
  - `smoothPosition` / `smoothRotation = true`: Guarantees jitter-free positioning.
- **Weapon Firing Cycle:**
  - Listens to `activated` event (`ActivateEventArgs`) dispatched by XRI when the user pulls the controller trigger or clicks `LMB` in the simulator.
  - Raycasts forward along `barrelPoint.forward` with layer masking.
  - Emits an instant laser tracer line via `LineRenderer` with auto-decay coroutine.
  - Dispatches procedural audio tone (gunshot pop/thump) without requiring external audio asset dependencies.
- **Target Hit Registration:**
  - `FoundationTarget` detects incoming raycast impact, increments hit counter, flashes its material from red to bright green, generates an ascending chime tone, and updates a 3D TextMesh in the scene.

### 2.4 XR Device Simulator (Desktop Mode)
- Configured via official `XR Interaction Simulator.prefab` from Unity's `com.unity.xr.interaction.toolkit: 3.4.0`.
- Integrated directly into the scene and linked to `XRDeviceSimulatorSettings.asset`.
- Enables complete development, interaction, aiming, and firing testing using only a mouse and keyboard.

---

## 3. Comparison with Previous Baseline

| Issue in Previous Prototype | Root Cause in Legacy Code | Foundation Architectural Resolution |
|---|---|---|
| Player spawned halfway in floor | Manual offset scripts and missing floor collider datum | Standardized `XROrigin` with Floor tracking mode and solid $Y=0$ BoxCollider. |
| Player walked into air off edges | Rooftop perimeter lacked physical barriers and gravity checks | Enclosed 4-wall perimeter boundary with heavyweight colliders. |
| Clunky desktop camera control | Custom raw `DEADeyeDesktopController` wrestling with cursor | Official Unity **XR Interaction Simulator** with standard desktop bindings. |
| Weapon fell through floor on recoil | Dynamic position accumulation without baseline reset | Physics-grounded `XRGrabInteractable` with clean event-driven firing. |
| UI text unreadable / pink shaders | Legacy font references and invalid UI scale | Standard URP 17 shaders and 3D TextMesh components with proper depth testing. |
