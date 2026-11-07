# DEADeye — Weapon Systems & Ballistics Engineering Manual

```
========================================================================================
MODULE       : Weapon Architecture, Two-Pass Ballistics & Optical Scope Pipeline
ENGINE       : Unity 6.3 LTS (6000.3.27f2)
SOURCE FILES : Assets/DEADeye/Scripts/Weapons/DEADeyeSniperWeapon.cs
               Assets/DEADeye/Scripts/Weapons/DEADeyeScopeOptic.cs
========================================================================================
```

---

## 1. Tactical Sniper Rifle Specification: The XM500

The operative's primary armament in DEADeye is the **XM500 Anti-Materiel Precision Sniper Rifle**:

```
+---------------------------------------------------------------------------------------+
|                             XM500 SNIPER SPECIFICATIONS                              |
+--------------------------+----------------------------+-------------------------------+
| Caliber: .50 BMG (12.7mm)| Effective Range: 400m      | Fire Mode: Bolt-Action        |
| Magazine Capacity: 5 Rds | Reload Duration: 2.2s      | Optical Magnification: 6x/12x |
| Base Damage: 100 HP      | Headshot Multiplier: 3.0x  | Max Ballistic Range: 500m     |
+--------------------------+----------------------------+-------------------------------+
```

---

## 2. The Two-Pass Ballistics Pipeline

In standard first-person games, raycasts originating solely from the player's camera cause shooting-around-corners exploits, while raycasts originating solely from the rifle muzzle cause severe parallax misalignment where bullets miss the crosshair at varying target distances.

DEADeye implements a deterministic **Two-Pass Hitscan Ballistics Pipeline** in `DEADeyeSniperWeapon.cs` that completely eliminates both flaws:

```mermaid
sequenceDiagram
    autonumber
    actor Player
    participant Weapon as DEADeyeSniperWeapon
    participant Optic as Scope Camera (Optic Axis)
    participant Muzzle as Rifle Muzzle (Bore Axis)
    participant World as PhysX World Geometry

    Player->>Weapon: Trigger Fire Command (while scoped)
    Weapon->>Optic: Pass 1: Cast Ray from Optic Center (Screen Midpoint)
    Optic->>World: Physics.RaycastAll(cameraAimRay, 500m)
    World-->>Weapon: Intended Aim Point (Hit Target or Max Range Point)

    Weapon->>Muzzle: Pass 2: Cast Ray from Physical Muzzle toward Intended Point
    Muzzle->>World: Physics.Raycast(muzzlePos, directionToPoint, distance)
    alt Physical Obstacle Intercepts Muzzle Ray (Wall / Railing)
        World-->>Weapon: Obstacle Hit Detected
        Weapon->>Weapon: Deflect Shot; Spawn Concrete Dust Decal (Target Unharmed)
    else Muzzle Ray Reaches Intended Target Point
        World-->>Weapon: Confirmed Target Collider Hit
        Weapon->>Weapon: Process Hit Damage (Headshot 3x / Body 1x)
    end
```

### Mathematical Formulation of Two-Pass Parallax Correction

1. **Pass 1 — Intended Aim Vector**:
   Let $\mathbf{C}$ be the world position of the active aiming camera (either the high-magnification optic camera or main camera), and $\hat{\mathbf{v}}_c$ be its normalized forward vector.
   $$\mathbf{R}_{\text{aim}}(t) = \mathbf{C} + t \hat{\mathbf{v}}_c, \quad t \in [0, D_{\max}]$$
   A raycast determines the first valid intersection with scene geometry or entities at point $\mathbf{P}_{\text{target}}$:
   $$\mathbf{P}_{\text{target}} = \begin{cases} \mathbf{R}_{\text{aim}}(t_{\text{hit}}) & \text{if hit detected} \\ \mathbf{C} + D_{\max} \hat{\mathbf{v}}_c & \text{otherwise} \end{cases}$$

2. **Pass 2 — Physical Muzzle Line-of-Sight**:
   Let $\mathbf{M}$ be the physical muzzle transform position. The ballistic trajectory vector $\vec{\mathbf{B}}$ is computed as:
   $$\vec{\mathbf{B}} = \mathbf{P}_{\text{target}} - \mathbf{M}, \quad d_B = \|\vec{\mathbf{B}}\|, \quad \hat{\mathbf{u}}_B = \frac{\vec{\mathbf{B}}}{d_B}$$
   A second ray is cast from $\mathbf{M}$ along $\hat{\mathbf{u}}_B$ over distance $d_B$:
   $$\text{Hit}_{\text{muzzle}} = \text{Raycast}(\mathbf{M}, \hat{\mathbf{u}}_B, d_B - \epsilon)$$
   If $\text{Hit}_{\text{muzzle}}$ encounters an environmental collider (such as a parapet wall or steel railing) before reaching $\mathbf{P}_{\text{target}}$, the projectile is absorbed by the obstacle. If clear, damage is dispatched to the target.

---

## 3. Optical Scope & Magnification Pipeline

Implemented in `DEADeyeScopeOptic.cs`, the sniper scope utilizes a secondary render texture camera pipeline:

```
[ Primary Game Camera ] ──> Renders full scene at 60/72 FPS
[ Scope Optic Camera  ] ──> Renders narrow-FOV cone (12° / 6°) ──> [ ScopeRenderTexture (512x512) ]
                                                                             │
                                                                             ▼
                                                  [ Reticle Material on Scope Lens Mesh ]
```

- **Scope Camera Specifications**:
  - Attached as a child to the rifle's optical housing.
  - Active only when aiming (`scopeCamera.enabled = isAiming`), saving rendering overhead when unscoped.
  - Post-processing and anti-aliasing are specifically configured to prevent visual noise through the optic.
- **Magnification Settings**:
  - *Standard Optical Zoom*: Field of view set to $12.0^\circ$ ($\approx 6\times$ magnification).
  - *High-Magnification Mode*: Field of view set to $6.0^\circ$ ($\approx 12\times$ magnification).

---

## 4. Mandatory Scope Fire Gate

A defining gameplay rule in DEADeye is the **Scope Fire Gate**:

```csharp
public bool TryFire()
{
    // 1. Mission State Check
    if (DEADeyeGameManager.Instance != null && 
        DEADeyeGameManager.Instance.CurrentState != DEADeyeGameState.MissionActive)
        return false;

    // 2. Optical Scope Gate — hip-firing strictly prohibited
    if (scopeOptic != null && !scopeOptic.IsAiming)
    {
        PlayDryFireClick();
        if (DEADeyeMissionHUD.Instance != null)
        {
            DEADeyeMissionHUD.Instance.ShowScopeWarning("ENGAGE OPTICAL SCOPE TO FIRE");
        }
        return false;
    }

    // 3. Ammunition & Reload Gate
    if (currentAmmo <= 0 || isReloading) return false;

    // Execute Two-Pass Raycast...
}
```

### Tactical Rationale
In real-world anti-materiel rifle operations, hip-firing a 30-pound rifle while maintaining accuracy is physically impossible. Requiring the player to raise the optic and acquire the reticle creates an authentic sniper cadence, discouraging run-and-gun play in favor of calculated tactical engagement.

---

## 5. Damage Modeling & Target Anatomical Hitboxes

Target entities (`DEADeyeHostileAI.cs`, `DEADeyeFriendlyTechnician.cs`) employ multi-collider anatomical damage routing:

```
                  +-------------------+
                  |   Head Collider   | ──> 3.0x Multiplier (300 Damage)
                  +-------------------+     [ Instant Neutralization ]
                  |   Body Collider   | ──> 1.0x Multiplier (100 Damage)
                  |   (Torso / Hips)  |     [ Standard Elimination ]
                  +-------------------+
                  |   Limb Colliders  | ──> 1.0x Multiplier (100 Damage)
                  +-------------------+
```

- **Headshot Verification**:
  ```csharp
  bool isHeadshot = hit.collider.CompareTag("Head") || 
                    hit.collider.name.IndexOf("Head", System.StringComparison.OrdinalIgnoreCase) >= 0;
  int damage = isHeadshot ? (baseDamage * 3) : baseDamage;
  ```
- **Feedback FX**:
  - *Headshot*: Blood particle burst, metallic confirmation chime, $+300$ floating combat text.
  - *Body Shot*: Standard dust/spark impact, $+100$ score indicator.
  - *Knockdown Reaction*: Hostiles execute a procedural physics-driven knockdown drop upon receiving lethal damage.
