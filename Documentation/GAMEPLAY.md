# DEADeye — Gameplay Systems & Mechanics Manual

```
========================================================================================
MODULE       : Gameplay Architecture & Combat Mechanics
ENGINE       : Unity 6.3 LTS (6000.3.27f2)
SOURCE PATHS : Assets/DEADeye/Scripts/Mission/, Assets/DEADeye/Scripts/Weapons/
SCENE        : Assets/DEADeye/Scenes/RooftopMission.unity
========================================================================================
```

---

## 1. Core Gameplay Loop

DEADeye places the operative in a high-intensity urban sniper deployment where every shot, second, and observation matters:

```
[ Mission Briefing & Target Recognition ]
                 │
                 ▼
[ Strategic Overwatch & Street Scanning ]
                 │
                 ▼
[ Engage Optical Scope & Reticle Alignment ]
                 │
                 ▼
[ Precision Ballistic Shot Execution ]
    ├── Wall Obstruction? ──> Deflected (No damage)
    ├── Headshot Hit?     ──> Instant Kill (300 pts + Combo)
    └── Body Hit?         ──> Standard Kill (100 pts)
                 │
                 ▼
[ Tactical Magazine Cycling & Reload Management ]
                 │
                 ▼
[ Objective Progression: Phases 1 to 4 ]
                 │
                 ▼
[ Mission Results & Operational Debriefing ]
```

---

## 2. Overwatch Traversal & Vantage Points

Rather than confining the player to a static pillbox, DEADeye features a traversable elevated rooftop complex connected by walkable industrial walkways:

```
                  [ Perch 3: West Overwatch ]
                  (Altitude: 26m | Pos: -18, 26, 6)
                              │
                              │ [ West Skybridge ]
                              │ (Enclosed Steel Bridge)
                              ▼
[ Perch 1: Primary Nest ] ◄───────────────► [ Perch 2: East Overwatch ]
(Altitude: 26m | Pos: 0, 26, -2.5)  [ East Catwalk ]  (Altitude: 26m | Pos: 18, 26, 4)
                                (Open Grated Walkway)
```

1. **Perch 1 (North Main Nest)** `(0, 26, -2.5)`:
   - Centered overlooking the grand 5-lane boulevard and main plaza.
   - Primary vantage for initial target reconnaissance and vehicle traffic tracking.
2. **East Catwalk & Perch 2** `(18, 26, 4)`:
   - Accessible via an open-grated catwalk with steel safety handrails.
   - Offers acute-angle line of sight into the eastern alleyway and the allied communications relay where Elena and Marcus work.
3. **West Skybridge & Perch 3** `(-18, 26, 6)`:
   - Connected via an enclosed structural skybridge spanning the rooftop gap.
   - Provides flanking overwatch into the western construction zone where Commander Vance attempts extraction.

### Rooftop Perimeter Safety Constraint
Rooftop safety is strictly enforced by `DEADeyeRooftopBoundary.cs`:
- Operative coordinates are bounded by a $64\text{ m} \times 24\text{ m} \times 40\text{ m}$ safety volume centered at $(0, 26, -1)$.
- If the operative falls below $18.0\text{ m}$ (street level is at $0.15\text{ m}$), the boundary controller safely intercepts the character controller and resets the operative to the nearest safe perch coordinate without scene restarts or physics crashes.

---

## 3. Optical Scoping & Aiming Pipeline

Tactical sniping doctrine requires positive target identification through optical magnification before discharging weapons:

```
[ Idle State ] ──Hold RMB / Right Grip──> [ Scope Transition: 0.15s ] ──> [ Active Zoom: FOV 12° / 6° ]
      ▲                                                                               │
      └──────────────── Release RMB / Right Grip ─────────────────────────────────────┘
```

- **Dual-Lens Optical System**: Implemented in `DEADeyeScopeOptic.cs`. An independent high-precision camera renders a zoomed view onto a dedicated render texture reticle (`ScopeTargetTexture`).
- **Variable Magnification Levels**:
  - *Standard Optical Zoom*: $12.0^\circ$ Field of View (approx. $6\times$ optical magnification).
  - *High-Magnification Mode*: $6.0^\circ$ Field of View (approx. $12\times$ optical magnification) for extreme long-range identification.
- **Mandatory Scope Fire Gate**:
  - Hip-firing is strictly blocked in `DEADeyeSniperWeapon.TryFire()`.
  - Attempting to discharge the weapon while unscoped emits an audible dry-fire safety click and visual HUD reminder: *"ENGAGE OPTICAL SCOPE TO FIRE"*.

---

## 4. Ballistics & Target Damage Model

Target hit detection is evaluated through a deterministic two-pass raycast pipeline that resolves camera-muzzle parallax:

```
Pass 1: Optic Camera Center Ray ──────────────────> Intended World Point (Hit 1)
                                                              ▲
Pass 2: Muzzle Physical Ray ──────────────────────────────────┘
            │
            ├── Collision with Wall/Obstacle? ──> Shot Obstructed (Impact Decal & Dust)
            └── Clear Line to Target?         ──> Impact Confirmed & Damage Processed
```

### Anatomical Damage Multipliers
- **Headshot**: Hit collider tagged `Head` or named `HeadCollider`.
  - Damage: $300\text{ HP}$ ($3.0\times$ base damage multiplier).
  - Score Award: **300 Points**.
  - Audio: High-impact ballistic crack + distinctive metallic elimination chime.
- **Body / Torso Hit**: Hit collider tagged `Body` or default mesh collider.
  - Damage: $100\text{ HP}$ ($1.0\times$ base damage multiplier).
  - Score Award: **100 Points**.
- **Environmental Penetration**:
  - Structural concrete, brick buildings, steel beams, and vehicle hulls fully absorb ballistic energy, blocking hits to targets behind cover.

---

## 5. Operation DEAD AIR — Mission Phases

The operational director (`DEADeyeMissionDirector.cs`) orchestrates the four phases dynamically over a 120-second combat clock:

### Phase 1: Recon & Spotting
- **Briefing**: Intelligence reports an active electronic warfare unit suppressing allied frequencies.
- **Objective**: Use optical scope magnification to scan the urban roadways. Identify the military communications jammer and hostile patrol sentries.
- **Completion Trigger**: 3 key hostile targets or objects spotted via `DEADeyeSpotable.cs`.

### Phase 2: Electronic Disruption
- **Briefing**: The jammer array is powered by a high-voltage auxiliary junction box.
- **Objective**: Locate the electrical junction box mounted on the street-level utility pole and destroy it with precision ballistic fire.
- **Consequence**: The junction explodes into electrical arc bursts; the jammer array deactivates, and local alarms activate.

### Phase 3: Defense of Allied Technicians
- **Briefing**: Allied field technicians Elena and Marcus deploy to the eastern satellite relay to reconnect the grid.
- **Objective**: Provide continuous sniper overwatch. Hostile patrol squads spawn from alleyways and converge on the technicians.
- **Failure Condition**: If technician health drops to zero, the mission terminates in operational failure.

### Phase 4: Neutralize Commander Vance
- **Briefing**: Hostile commander Vance enters the district with heavy tactical security attempting to escape to an extraction vehicle.
- **Objective**: Identify Vance among the escorts, pierce his body armor with a precision headshot, and eliminate him before he reaches the extraction boundary.

---

## 6. Scoring, Combos & Mission Evaluation

The performance scoring engine (`DEADeyeScoreManager.cs`) evaluates operational efficiency:

$$ \text{Total Score} = (\text{Base Kills} \times 100) + (\text{Headshots} \times 300) + (\text{Combo Bonus}) - (\text{Civilian Casualties} \times 500) $$

### Performance Grading Scale

| Grade | Rating | Minimum Score Required | Tactical Evaluation |
|:---:|:---:|:---:|---|
| **S** | Elite Marksman | 3,500+ Pts | Flawless operation, $\ge 80\%$ headshot accuracy, zero technician damage. |
| **A** | Sharpshooter | 2,800+ Pts | High accuracy, completed all 4 phases, zero escapes. |
| **B** | Marksman | 2,000+ Pts | Objective achieved, acceptable hit consistency. |
| **C** | Operational Pass | 1,200+ Pts | Mission accomplished, low precision or 1–2 hostile escapes. |
| **F** | Operational Failure | < 1,200 Pts / Time Expired | Technicians neutralized, 3+ hostiles escaped, or timer expired. |
