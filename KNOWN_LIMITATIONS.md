# DEADeye VR — Known Limitations & Future Roadmap

This document outlines known behavioral differences between desktop simulation and physical VR hardware, current system boundaries, and recommended enhancements for future release milestones.

---

## 1. Desktop Simulator vs. Physical VR Differences

### 1.1 Weapon Handling & Stabilization
- **Physical VR Experience**: Marksmen hold the sniper rifle with two hands. The dominant hand grips the rifle handle and index finger depresses the trigger, while the non-dominant hand stabilizes the front barrel/stock. Two-point tracking significantly dampens natural controller jitter and provides realistic optical alignment.
- **Desktop Simulation**: Aiming is executed via mouse look. Holding Right Mouse Button (`RMB`) toggles the magnified optic camera to center screen. Weapon sway and stabilization are handled procedurally rather than physically.

### 1.2 Optical Scope Parallax
- **Physical VR**: The sniper scope utilizes a 3D ocular lens shader rendered onto a circular physical cylinder attached to the rifle receiver. True binocular stereoscopic depth allows players to look into the scope with one eye while keeping the other eye open for situational peripheral awareness.
- **Desktop Simulation**: The screen renders a monoscopic camera view with the scope RenderTexture placed in the primary viewport.

---

## 2. Editor Simulator vs. Hardware Considerations

### 2.1 Haptic Feedback
- **Hardware-Unverified**: Physical Meta Quest Touch controllers feature dual linear resonant actuators (LRA) providing localized haptic pulses during gunshot discharge, bolt cycling, and target impacts. In Editor PlayMode / Desktop testing, haptic calls are handled gracefully as no-ops without error.

### 2.2 Inside-Out Tracking & Hand Occlusion
- In real-world physical play, bringing controllers too close to the headset (e.g. cheek weld position) can occasionally enter the optical blind spot of Quest tracking cameras. The project's two-handed stabilization offset has been calibrated with an ergonomic forward margin to mitigate occlusion.

---

## 3. Recommended Future Roadmap

1. **Ballistics Physics Engine (External Ballistics)**:
   - Expand the current responsive hitscan system into a dual-mode system where extreme-range shots (>250 meters) simulate bullet drop ($g = 9.81\text{ m/s}^2$), air drag, and dynamic windage drift.
2. **Thermal & Night Vision Optic Modes**:
   - Introduce togglable scope shaders rendering cold urban terrain in monochromatic blue/gray while highlighting hostile humanoids in luminous infrared red/yellow heat signatures.
3. **Advanced Urban Environmental Hazards**:
   - Add dynamic day/night cycles, urban rain precipitation with lens water droplets, and ambient traffic on lower elevated freeways.
4. **Interactive Tactical Spotter**:
   - Implement an AI or cooperative spotter character stationed on the rooftop providing vocal distance telemetry and high-value target callouts.
