# DEADeye — Known Limitations & Technical Considerations

This document details the behavioral distinctions between desktop simulation and physical Meta Quest VR hardware, current system boundaries, and recommended technical enhancements for subsequent releases.

---

## 1. Desktop Simulation vs. Physical VR Hardware

### 1.1 Weapon Aiming & Spatial Stabilization
- **Physical VR Experience (Level 3: Hardware-Unverified)**:
  - In 6DOF VR on Meta Quest 2 / 3 / Pro, the player physically brings the rifle ocular cylinder to their dominant eye.
  - Two-handed grab interaction (`XRGrabInteractable` with secondary attach transform) stabilizes physical controller tremor. Bringing the non-dominant hand near the foregrip smooths angular aiming rotations.
- **Desktop Simulator (Level 2: Editor Manual Verified)**:
  - On PC, aiming is controlled via mouse pitch/yaw. Holding Right Mouse Button (`RMB`) toggles optical magnification and full-screen reticle overlay.
  - Recoil is handled procedurally via damped spring pitch recovery rather than physical hand impulse.

### 1.2 Optical Scope Parallax & Rendering
- **Physical VR**:
  - The optical scope renders a real-time perspective to a circular ocular lens attached physically to the rifle receiver. True binocular vision enables players to view the magnified reticle with one eye while keeping the opposite eye open for peripheral threat scanning.
- **Desktop Simulator**:
  - The screen renders a monoscopic camera view with a COD Mobile-style central scope overlay and vignette masking.

### 1.3 Stationary Perch vs Locomotion
- Per the **Final Scope Reduction Directive**, all walking locomotion has been intentionally disabled across both platforms.
- The player is anchored at `(0, 26.0, -2.5)`. This eliminates any possibility of:
  - Walking off the rooftop edge into the street void.
  - Collision clipping through parapet walls or floors.
  - Motion sickness (vestibular mismatch) from artificial thumbstick locomotion in VR.

---

## 2. Hardware-Unverified Items

The following physical device behaviors are marked as **Hardware-Unverified** until executed on physical Meta Quest hardware:

1. **Controller Haptic Feedback**:
   - Touch controller linear resonant actuator (LRA) pulses on gunshot discharge, bolt-action cycling, and hit confirmation. In Desktop mode, haptic calls are suppressed gracefully without error.
2. **Headset Proximity Tracking**:
   - In tight cheek-weld postures, bringing Touch controllers very close to the headset can enter the tracking boundary of Quest inside-out cameras. The weapon socket has been offset forward by 12cm to prevent tracking loss.
3. **Thermal & Battery Profiling**:
   - Sustained 72Hz / 90Hz frame rates under continuous real-time scope rendering on the Qualcomm Snapdragon XR2 Gen 1 (Quest 2) and XR2 Gen 2 (Quest 3) platforms.

---

## 3. Recommended Future Roadmap

1. **Ballistic Projectile Simulation**:
   - Add optional bullet flight time ($t = d / v$), windage deflection, and gravity bullet drop for shots beyond 200 meters.
2. **Thermal / Night Vision Optic Shaders**:
   - Add togglable scope modes rendering urban geometry in cold monochrome while highlighting hostiles in high-contrast thermal signatures.
3. **Advanced Weather & Environmental Audio**:
   - Dynamic urban rain, fog conditions obscuring distant targets, and thunder/wind audio effects.
4. **Tactical Spotter AI**:
   - Add a stationary NPC spotter on the rooftop calling out high-value target bearing and range coordinates.
