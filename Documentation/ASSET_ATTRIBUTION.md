# DEADeye — Asset Attribution & Licensing Audit

```
========================================================================================
MODULE       : Third-Party Asset Licenses, Open Source Attributions & Compliance
PROJECT      : DEADeye — Urban Tactical Operations
AUTHORITATIVE: C:\Unity\Editors\DEADeyeVR_Foundation
BUDGET       : $0.00 (Strict Zero-Dollar Budget Policy / Free & Permissive Assets Only)
========================================================================================
```

---

## 1. Compliance & Redistribution Statement

All third-party assets incorporated into DEADeye have been audited for public open-source repository compliance:
- **Budget Compliance**: \$0.00 total expenditure. No paid Unity Asset Store packages, subscriptions, or proprietary plugins were purchased or utilized.
- **Redistribution Rights**: Every asset included in this repository utilizes permissive licenses allowing public redistribution, modification, and non-commercial/commercial usage (CC0, CC-BY 4.0, MIT, or Unity Companion License).

---

## 2. 3D Art & Environmental Assets

### 1. Quaternius Modular Character Pack
- **Creator**: Quaternius (quaternius.com)
- **License**: **CC0 1.0 Universal (Public Domain)**
- **Usage**: Serves as the base modular geometry for hostile patrol guards, allied repair technicians, rogue commander Vance, and urban civilians located in `Assets/DEADeye/Art/Characters/`.
- **Modifications**: Integrated custom clothing textures, customized color variations, added box and capsule colliders for anatomical hit routing, and bound to `DEADeyeProceduralCharacterAnimator.cs`.

### 2. Winchester Model 1887 / LowPolyGuns Sniper Rifle
- **Creator**: TastyTony (Sketchfab)
- **License**: **Creative Commons Attribution 4.0 International (CC-BY 4.0)**
- **Usage**: Primary weapon mesh and texture assets located in `Assets/DEADeye/Art/Weapons/LowPolyGuns/`.
- **Attribution**: Based on "Winchester Model 1887" by TastyTony, licensed under CC-BY 4.0.
- **Modifications**: Re-textured with a matte non-reflective tactical finish; eliminated specular sparkle artifacts; attached custom optic housing and muzzle attachment points.

### 3. Dojo & Metropolitan Environment Props
- **License**: Permissive Free Asset License (Free Unity Community Asset)
- **Usage**: Architectural structures, steel catwalks, railings, streetlamps, utility poles, and industrial water towers located in `Assets/DEADeye/Art/Environments/`.
- **Modifications**: Simplified collider hierarchies for mobile PhysX efficiency; applied custom URP unlit and lit materials.

---

## 3. Audio & Acoustic Assets

- **Kenney Game Assets (Audio Packs)**:
  - **Creator**: Kenney (kenney.nl)
  - **License**: **CC0 1.0 Universal (Public Domain)**
  - **Usage**: UI interaction clicks, terminal confirmation chimes, weapon dry-fire safety clicks, reload cycling sounds.
- **Freesound Community Audio**:
  - **License**: **Creative Commons 0 / Attribution**
  - **Usage**: Distant metropolitan traffic hums, ambient wind over rooftop perches, electrical power junction arc bursts.
  - **Modifications**: Equalized, normalized, and converted to mono 16-bit WAV assets for low-overhead 3D spatial attenuation.

---

## 4. Unity Engine Packages & First-Party Dependencies

All first-party package dependencies are tracked in `Packages/manifest.json` under the standard **Unity Companion License**:

| Package Identifier | Version | Operational Function in DEADeye |
|---|---|---|
| `com.unity.render-pipelines.universal` | `17.3.0` | Universal Render Pipeline with Forward+ rendering and SPI stereo support. |
| `com.unity.xr.openxr` | `1.16.1` | Khronos OpenXR loader and Meta Quest Touch controller driver. |
| `com.unity.xr.interaction.toolkit` | `3.4.0` | XR Origin rig, locomotion providers, and interactor foundations. |
| `com.unity.inputsystem` | `1.16.0` | Unity New Input System hardware aggregation and device polling. |
| `com.unity.ai.navigation` | `2.0.10` | NavMesh surface runtime baking and dynamic agent pathfinding. |
| `com.unity.textmeshpro` | `2.0.0` | High-fidelity Signed Distance Field (SDF) typography for tactical HUD. |
| `com.unity.test-framework` | `1.6.0` | NUnit test execution runner for EditMode and PlayMode verification. |
