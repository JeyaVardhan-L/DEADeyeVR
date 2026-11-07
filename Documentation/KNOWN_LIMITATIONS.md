# DEADeye — Technical Considerations & Known Limitations

```
========================================================================================
MODULE       : Engineering Transparency, Architectural Boundaries & Roadmap
PROJECT      : DEADeye — Urban Tactical Operations
AUTHORITATIVE: C:\Unity\Editors\DEADeyeVR_Foundation
========================================================================================
```

---

## 1. Scope & Vertical Slice Boundaries

In keeping with professional engineering integrity and realistic open-source game development:

### 1. Single-Operation Vertical Slice: Operation DEAD AIR
- **Implemented Scope**: **Operation DEAD AIR** is the complete, playable vertical slice containing all four sequential phases (Reconnaissance, Power Disruption, Engineer Defense, and Commander Vance Neutralization).
- **Campaign Expansion Previews**: Operations 02 (*Broken Signal*), 03 (*Black Horizon*), and 04 (*Silent Citadel*) exist as data-driven mission cards viewable in the Tactical Terminal UI (`DEADeyeTacticalTerminalUI.cs`). They illustrate future campaign progression but are not yet implemented as standalone scene geometries.

### 2. Procedural Animation vs. Skeletal Motion Capture
- **Design Trade-Off**: Characters utilize procedural trigonometric joint oscillation (`DEADeyeProceduralCharacterAnimator.cs`) rather than large motion-capture animation clip libraries.
- **Rationale**:
  1. Strict \$0 budget compliance.
  2. Guarantees the compiled Android package remains under $56.0\text{ MB}$.
  3. Dynamic scaling of stride length and frequency to real-time NavMesh velocity without foot sliding.
- **Known Limitation**: While highly functional and responsive, procedural oscillation lacks the nuanced visual fidelity of multi-layer skeletal blend trees.

---

## 2. Hardware Validation vs. Automated Simulation

- **Automated Test Coverage**: The project maintains 32 automated tests (27 EditMode, 5 PlayMode) running under NUnit, verifying 100% of ballistics math, damage multipliers, FSM transitions, and rig mutual exclusivity.
- **Hardware Ergonomics**: Automated tests execute within Unity's headless test runner. While the OpenXR Touch controller input pipeline and head tracking algorithms compile with 0 errors and pass automated data validation, long-duration physical comfort (e.g., neck strain during extended vertical downward aiming, controller wrist tilt angles) requires physical evaluation on headset hardware.

---

## 3. Gameplay & World Geometry Constraints

- **Fixed Elevated Perimeter**: The operative's operational area is intentionally restricted to the 26-meter rooftop network (Perch 1, East Catwalk, Perch 2, West Skybridge, Perch 3). The player cannot descend to street level or explore interior rooms outside the sniper nest perimeters.
- **Hitscan Ballistics**: Ballistics use high-speed two-pass hitscan raycasts rather than simulated physical bullet drop or aerodynamic wind drift. This design choice was selected to maintain consistent 72 FPS mobile performance on Quest 2's mobile CPU without incurring frame drops from PhysX continuous collision detection.

---

## 4. Engineering Roadmap & Future Milestones

```
[ Current Milestone: v1.2.0 ] ──> Full Playable Vertical Slice (Operation DEAD AIR)
                                  Dual-Rig Desktop / Quest 2 Support
                                  Automated Two-Pass Ballistics & OpenXR Head Tracking
                                       │
                                       ▼
[ Future Milestone: v1.3.0  ] ──> Operation 02: Broken Signal (Night Operations & NVG)
                                  Physical Bullet Drop Simulation (Optional Realism Mode)
                                  Two-Handed VR Rifle Stabilized Grip Pose
                                       │
                                       ▼
[ Future Milestone: v2.0.0  ] ──> Dynamic Urban Weather (Rain, Fog Obscuration)
                                  Cooperative Two-Player Sniper-Spotter Over-the-Network
```
