# DEADeye — Game Overview & Design Specification

```
========================================================================================
GAME TITLE     : DEADeye — Urban Tactical Operations
PRIMARY ENGINE : Unity 6.3 LTS (6000.3.27f2)
RENDER PIPELINE: Universal Render Pipeline (URP 17.3.0) — Forward+
TARGETS        : PC Desktop Simulator (Windows x64) & Meta Quest 2 / Pro (Android ARM64 OpenXR)
PRIMARY SCENE  : Assets/DEADeye/Scenes/RooftopMission.unity
VERTICAL SLICE : Operation DEAD AIR (Full 4-Phase Covert Military Operation)
========================================================================================
```

---

## 1. Project Vision & Identity

**DEADeye** is a tactical first-person urban sniper game developed for high-performance deployment across both PC desktop hardware and standalone mobile virtual reality on the **Meta Quest 2**.

Departing from conventional "shooting gallery" prototypes, DEADeye drops the operative directly into a high-altitude urban overwatch position. Stationed atop a 26-meter rooftop complex interconnected by steel catwalks and skybridges, the operative commands a panoramic view over an occupied city district.

The core gameplay centers on patience, observational intelligence, long-range ballistic precision, and rapid threat prioritization under strict operational time limits.

### Design Pillars

```
+---------------------------------------------------------------------------------------+
|                               DEADeye DESIGN PILLARS                                  |
+--------------------------+----------------------------+-------------------------------+
|  1. TACTICAL OVERWATCH   |  2. BALLISTIC DISCIPLINE   |   3. ADAPTIVE CROSS-PLATFORM  |
+--------------------------+----------------------------+-------------------------------+
| Fixed-altitude perimeter | Mandatory optical zoom;    | Unified codebase powering     |
| with strategic vantage   | two-pass raycast targeting;| both desktop mouse/keyboard   |
| points overlooking       | zero hip-fire tolerance;   | simulation and physical 6DoF  |
| dynamic street patrols.  | 3x precision headshots.    | OpenXR mobile VR experiences. |
+--------------------------+----------------------------+-------------------------------+
```

1. **Tactical Overwatch**: Rather than running through ground corridors, the player controls high-altitude elevated perches. The operative must scan roadways, alleys, and plazas, reading enemy patrol rhythms and identifying high-value targets before firing.
2. **Ballistic Discipline**: Unscoped hip-firing is mechanically rejected. Firing requires engaging the dual-lens optical scope, establishing proper alignment, and verifying shot clearance through urban geometry.
3. **Adaptive Cross-Platform Architecture**: A dual-rig subsystem allows full parity between keyboard/mouse desktop simulation and physical 6DoF motion-controlled virtual reality without duplicating game logic or assets.

---

## 2. Setting & Atmosphere

### The Urban District
The operational theater is a dense metropolitan industrial sector:
- **Overwatch Perches**: Three distinct high-ground vantage points situated at an elevation of 26 meters:
  - *Perch 1 (North Overwatch)*: Direct overlook onto the central boulevard and main roadway intersections.
  - *Perch 2 (East Overwatch)*: Accessible via an elevated industrial catwalk; provides flanking line-of-sight across the east transit plaza.
  - *Perch 3 (West Overwatch)*: Accessible via an enclosed structural skybridge; overlooks active construction zones and side alleyways.
- **Street-Level Grid**: An expansive 5-lane roadway network, paved sidewalks, streetlamps, trees, parked vehicles, and architectural structures populated by NavMesh-driven patrols.

### Visual Style
- **Aesthetic**: Gritty, industrial, functional, and militaristic.
- **Color Palette**: Muted urban tones—slate concrete, oxidized steel, asphalt darks, weathered metals—punctuated by tactical HUD amber, laser reticle cyan, and hostile warning red.
- **Lighting & Rendering**: URP Forward+ multi-light pipeline with real-time directional sunlight, baked ambient probes, and post-processing tuned for strict performance budgets on mobile mobile chipsets.

---

## 3. Operational Campaign: Operation DEAD AIR

The vertical slice features the complete covert military engagement **Operation DEAD AIR**, structured as a four-phase objective-driven mission:

```mermaid
flowchart LR
    P1["Phase 1: Recon & Spotting\n(Identify Jammer & Guards)"] --> P2["Phase 2: Disruption\n(Destroy Power Junction)"]
    P2 --> P3["Phase 3: Engineer Defense\n(Protect Elena & Marcus)"]
    P3 --> P4["Phase 4: Commander Vance\n(Neutralize High-Value Target)"]
    P4 --> Debrief["Mission Debriefing\n(Score & Accuracy Tally)"]
```

1. **Phase 1 — Reconnaissance & Target Acquisition**:
   - The operative scans the urban sector using optical magnification.
   - Key objective: Spot and designate the military communications jammer and hostile patrol sentries.
2. **Phase 2 — Electronic Disruption**:
   - Locate and eliminate the electrical power junction feeding the jammer array.
   - Destroying the junction drops hostile communications and triggers perimeter alarms.
3. **Phase 3 — Defense of Allied Technicians**:
   - Allied engineers Elena and Marcus deploy to the communications relay to restore allied feeds.
   - The operative provides sniper overwatch, eliminating converging hostile patrol waves before allied technicians sustain fatal damage.
4. **Phase 4 — Neutralization of Commander Vance**:
   - Rogue hostile commander Vance enters the district with heavy security escorts attempting to flee the sector.
   - The operative must identify and eliminate Vance before he reaches the extraction boundary.

---

## 4. Key Gameplay Metrics & Constraints

| Parameter | Value | Operational Purpose |
|---|---|---|
| **Mission Duration** | 120 Seconds | Enforces tactical urgency; prevents passive camping. |
| **Escaped Hostiles Limit** | 3 Escapes | Mission fails if 3 or more hostiles escape the sector perimeter. |
| **Sniper Magazine Capacity** | 5 Rounds | Demands ammunition discipline and timed reloads. |
| **Reload Duration** | 2.2 Seconds | Creates vulnerability windows during active hostile advances. |
| **Precision Headshot Multiplier** | $3.0\times$ (300 Damage) | Rewards anatomical precision; instantly neutralizes armored targets. |
| **Body Hit Damage** | 100 Damage | Standard single-shot kill on unarmored targets. |
| **Rooftop Elevation** | $26.0\text{ m}$ | Prevents ground clipping; establishes authentic sniper angle-of-depression. |

---

## 5. Technical Specifications

- **Engine Version**: Unity 6.3 LTS (`6000.3.27f2`)
- **Render Pipeline**: Universal Render Pipeline (URP `17.3.0`)
- **Stereoscopic XR**: Khronos OpenXR (`1.16.1`) / XR Interaction Toolkit (`3.4.0`)
- **Stereo Render Mode**: Single-Pass Instanced (SPI)
- **Physics**: NVIDIA PhysX (configured for high-speed hitscan raycasting)
- **Pathfinding**: Unity AI Navigation (`2.0.10`) NavMesh surface
- **Audio**: Custom multi-channel spatial audio manager (`DEADeyeAudioManager.cs`)
