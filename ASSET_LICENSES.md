# DEADeye VR — Asset Licenses & Attributions

This document records the provenance, licensing terms, and attributions for all third-party and procedural assets utilized in **DEADeye VR**.

---

## 1. Zero-Cost ($0) Compliance Statement

In compliance with project directives:
- **No paid assets or commercial SDKs** were purchased or imported.
- **No proprietary code** was copied from `mikeNspired/XRI-Starter-Kit` or unauthorized repositories.
- All code and procedural systems were authored using official Unity open packages and custom algorithms.

---

## 2. Package & Framework Licenses

### Unity Technologies Packages
- **Unity XR Interaction Toolkit (3.4.0)**
- **Unity OpenXR Plugin (1.16.1)**
- **Unity Input System (1.16.0)**
- **Unity Universal Render Pipeline (URP 17.3.0)**
- **Unity AI Navigation (2.0.7)**
- **Unity TextMesh Pro (3.2.0-pre.6)**

> **License**: [Unity Companion License (UCL)](https://unity.com/legal/licenses/unity-companion-license)  
> *Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files, to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software in Unity projects.*

---

## 3. Environment & 3D Art Assets

### Japanese Dojo Environment
- **Path**: `Assets/DEADeye/Art/Environments/Dojo/`
- **Origin**: Migrated from the original `DEADeyeVR` prototype repository (`Oculus-Shooter-Unity`).
- **License**: Creative Commons Attribution 4.0 International ([CC-BY 4.0](https://creativecommons.org/licenses/by/4.0/)).
- **Usage**: Deployed as the tactical staging lobby (`LobbyScene.unity`).

### Keith Thomson Sniper Rifle
- **Path**: `Assets/DEADeye/Art/Weapons/SniperRifle/`
- **Source Model**: `model.dae`
- **Author**: Keith Thomson
- **Origin**: Credited in original repository metadata.
- **License**: Creative Commons Attribution 3.0 Unported ([CC-BY 3.0](https://creativecommons.org/licenses/by/3.0/)).
- **Usage**: High-precision tactical sniper rifle equipped in the Dojo lobby and mounted on the rooftop perch.

---

## 4. Visual Effects & Shaders

### Particle VFX & Impact Spark Prefabs
- **Path**: `Assets/DEADeye/Art/VFX/`
- **Origin**: Unity Open Source Particle System samples and standard shaders.
- **License**: Unity Companion License & MIT License.

---

## 5. Procedural Synthesizers & Meshes ($0 Dependencies)

### Real-Time Waveform Audio Synthesizer
- **Script**: `Assets/DEADeye/Scripts/Core/DEADeyeAudioManager.cs`
- **Author**: DEADeye VR Development Team
- **License**: MIT License / Project Proprietary
- **Description**: Generates real-time audio clips entirely via mathematical waveform synthesis (transient white noise, sine tone modulation, exponential decaying envelopes). Powers gunshots, bolt cycling, magazine reloads, hit confirmations, target alert stings, and urban ambient background hum with zero external audio files.

### Procedural Urban City & Hostile Silhouettes
- **Scripts**: 
  - `Assets/DEADeye/Editor/DEADeyeMasterSceneBuilder.cs`
  - `Assets/DEADeye/Scripts/AI/DEADeyeHostileCriminal.cs`
  - `Assets/DEADeye/Scripts/AI/DEADeyeHostileMonster.cs`
- **Author**: DEADeye VR Development Team
- **License**: MIT License / Project Proprietary
- **Description**: Procedurally generates urban skyscrapers, city street grids, extraction boundary trigger volumes, and silhouetted hostile humanoids with headshot hitboxes using native Unity geometry primitives and unlit/URP shaders.
