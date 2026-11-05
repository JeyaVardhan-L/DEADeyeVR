# DEADeye: Technical Glossary & Architectural Lexicon
## Comprehensive A–Z Reference of Systems, Engines, Graphics, and XR Mechanics

```
========================================================================================
PROJECT       : DEADeye — Tactical Urban Sniper Sandbox & Quest 2 VR Foundation
ENGINE        : Unity 6.3 LTS (Version 6000.3.27f2)
PIPELINE      : Universal Render Pipeline (URP 17.3.0)
TARGETS       : Windows x64 Desktop Simulator & Meta Quest 2 / Pro (Android ARM64 OpenXR)
AUTHORITATIVE : C:\Unity\Editors\DEADeyeVR_Foundation
SCENE         : Assets/DEADeye/Scenes/RooftopMission.unity
========================================================================================
```

---

## Pedagogical Structure
Every technical term in this engineering glossary is defined across three distinct layers:
1. **Simple Definition**: Intuitive, non-technical everyday analogy suitable for beginners and viva introductions.
2. **Technical Definition**: Rigorous explanation of runtime, engine architecture, compiler operations, memory layout, or GPU hardware mechanics.
3. **DEADeye Context**: Concrete implementation details within this repository, citing specific classes, methods, structs, assets, or configuration settings.

---

## Alphabetical Index

[A](#a) | [B](#b) | [C](#c) | [D](#d) | [E](#e) | [F](#f) | [G](#g) | [H](#h) | [I](#i) | [J](#j) | [K](#k) | [L](#l) | [M](#m) | [N](#n) | [O](#o) | [P](#p) | [Q](#q) | [R](#r) | [S](#s) | [T](#t) | [U](#u) | [V](#v) | [W](#w) | [X](#x) | [Z](#z)

---

### A

#### $A^*$ Search Algorithm
- **Simple Definition**: A GPS navigation algorithm that finds the shortest, quickest path from Point A to Point B by estimating which direction brings you closer to your goal while avoiding obstacles.
- **Technical Definition**: A graph traversal and path search algorithm that plots an optimal route between nodes using the evaluation function $f(n) = g(n) + h(n)$, where $g(n)$ is the exact cost from the start node to $n$, and $h(n)$ is an admissible heuristic estimating the cost from $n$ to the goal.
- **DEADeye Context**: Executed internally by Unity's `NavMeshAgent` component on hostile patrol guards (`DEADeyeHostileAI.cs`) and field engineers (`DEADeyeFriendlyTechnician.cs`). Agents traverse the baked polygon triangulation of `Assets/DEADeye/Scenes/RooftopMission.unity` between waypoints, antenna nodes, and cover perches.

#### Action Map
- **Simple Definition**: A control profile or binder that groups inputs together based on what the player is doing (e.g., "Walking", "Driving", "In Menu").
- **Technical Definition**: A logical collection of user input actions within the Unity New Input System (`InputActionAsset`). Action maps allow input states to be enabled or disabled as discrete sets, preventing gameplay inputs from firing while navigating user interface menus.
- **DEADeye Context**: Managed via `DEADeyeInputRouter.cs` and `DEADeyeDesktopInputController.cs`. The router parses movement (`WASD`), mouse rotation (`Look`), scope toggle (`RMB`), and trigger (`LMB`) actions, isolating them when the tactical pause terminal (`Y` or `Escape`) is displayed.

#### Android Debug Bridge (ADB)
- **Simple Definition**: A digital data cable and command-line bridge allowing a desktop computer to communicate directly with an Android smartphone or VR headset.
- **Technical Definition**: A versatile client-server architecture tool (`adb.exe`) from the Android SDK Platform-Tools consisting of a client (running on developer PC), a background daemon (`adbd` executing on the Android/Quest OS kernel), and a server managing communication over USB or TCP/IP.
- **DEADeye Context**: Used to install standalone builds directly to Meta Quest 2 via `adb install -r Builds/Android/DEADeye_Quest2.apk` and stream live runtime crash logs via `adb logcat -s Unity ActivityManager PackageManager`.

#### Android Package (APK)
- **Simple Definition**: A zip archive containing all the code, 3D graphics, sounds, and instructions an Android device needs to install and run an application.
- **Technical Definition**: A variant of the Java Archive (JAR) package format conforming to the `ZIP` file format specification. It packages compiled DEX bytecode (or native ARM64 `.so` libraries compiled via IL2CPP), compiled assets (`assets/bin/Data`), Android manifest XML, and cryptographic signing certificates.
- **DEADeye Context**: `Builds/Android/DEADeye_Quest2.apk` (55,498,259 bytes). Built using Unity 6.3 LTS targeting Android API Level 32 (Android 12L) with ARM64 native binaries and ASTC texture compression.

#### Anti-Aliasing (MSAA)
- **Simple Definition**: A digital smoothing filter that softens jagged, staircase-like pixel edges along diagonal 3D lines, making computer graphics look clean and natural.
- **Technical Definition**: Multi-Sample Anti-Aliasing (MSAA) evaluates polygon coverage at multiple sub-pixel sample points per fragment while running the fragment/pixel shader only once per pixel centroid, reducing geometric aliasing along triangle boundaries with minimal GPU shading overhead.
- **DEADeye Context**: Critical for mobile VR in `Assets/DEADeye/Settings/DEADeye_Mobile_URPAsset.asset`. Configured to 4x MSAA on Meta Quest 2 to eliminate shimmering on urban skyscraper parapets and catwalk railings without incurring the blur of TAA or the performance penalty of SSAA.

#### Application Binary Interface (ABI) / ARM64
- **Simple Definition**: The native spoken language and instruction rulebook understood directly by the mobile processor hardware.
- **Technical Definition**: The low-level interface between an application program and the operating system or hardware architecture. ARM64 (`aarch64`) specifies 64-bit registers, instruction sets, calling conventions, and data alignment for modern ARM processors.
- **DEADeye Context**: Standalone Quest 2 runs a 64-bit Qualcomm Snapdragon XR2 SoC. Unity's build pipeline compiles C# code to C++ via IL2CPP, then compiles to ARM64 native shared libraries (`libmain.so`, `libunity.so`) using the Android NDK r23b/r25b.

#### Assembly Definition (`.asmdef`)
- **Simple Definition**: Project folder dividers that tell Unity: "Compile this folder as an isolated book so that modifying one script does not force the entire library to recompile."
- **Technical Definition**: A JSON-configured metadata asset in Unity that defines a managed C# assembly boundary. Code within an assembly definition compiles into an independent `.dll`, reducing iterative script compilation times and enforcing architectural modularity and dependency flow.
- **DEADeye Context**: The project isolates runtime scripts from automated tests. Test scripts in `Assets/DEADeye/Tests/` reference the primary game assembly, ensuring test harness code is never packaged into release production APK builds.

#### Adaptive Scalable Texture Compression (ASTC)
- **Simple Definition**: An intelligent photo-compression format created specifically for mobile phones and VR headsets that makes textures tiny in file size while keeping them sharp.
- **Technical Definition**: A block-based lossy texture compression technology developed by ARM and Khronos. It supports variable bit rates by compressing rectangular pixel blocks (e.g., $4\times4$, $6\times6$, $8\times8$) into fixed 128-bit blocks, allowing fine-grained trade-offs between memory footprint and visual fidelity.
- **DEADeye Context**: Configured in Unity Editor Build Settings as the default texture compression format for Android. Enables rapid GPU texture decompression directly in Snapdragon XR2 VRAM, preventing texture memory thrashing during long sniper sessions.

#### AudioListener
- **Simple Definition**: The digital "ears" of the game player inside the virtual world.
- **Technical Definition**: A Unity engine component that acts as the virtual microphone receiving signals from all active `AudioSource` components in the 3D scene, computing volume attenuation, panning, and spatial Doppler effects before sending audio buffers to the hardware output device.
- **DEADeye Context**: Attached to the active player camera (`Desktop_FPSRig/Main Camera` or `VR_XROriginRig/Camera Offset/Main Camera`). Controlled dynamically by `DEADeyePlayerController.cs` to ensure only one listener is enabled at runtime, eliminating Unity console warnings.

#### AudioSource
- **Simple Definition**: A digital speaker placed inside the 3D world that plays a sound file when commanded.
- **Technical Definition**: A Unity engine component that plays back an `AudioClip` within the scene. Can be configured for 2D playback (equal volume everywhere) or 3D positional playback (attenuated over distance via logarithmic/linear roll-off curves and spatial panning).
- **DEADeye Context**: Utilized by `DEADeyeAudioController.cs` and `DEADeyeTacticalAudioHub.cs` across 11 discrete WAV sound clips, including sniper rifle crack (`sfx_sniper_fire`), optical scope zoom hum (`sfx_scope_zoom`), bullet impacts (`sfx_impact_concrete`), and jammer interference (`sfx_jammer_pulse`).

---

### B

#### Baking
- **Simple Definition**: Pre-calculating complex math (like shadows or navigation maps) beforehand inside the editor so the game doesn't lag while playing.
- **Technical Definition**: The offline, pre-computation phase where complex geometric, lighting, or spatial relationships are rendered into static data structures (such as lightmap textures, reflection cubemaps, or NavMesh spatial polygons), shifting runtime computational loads entirely to build-time.
- **DEADeye Context**: Applied to both lighting and navigation. `NavMeshSurface` bakes all static urban building rooftops, skybridges, and catwalks into a spatial polygon mesh (`NavMesh.asset`) so AI guards can run pathfinding without dynamic geometry checks.

#### Batching (Static & Dynamic)
- **Simple Definition**: Grouping multiple small objects together into one big shipping crate so the computer processor only has to make one delivery to the graphics card.
- **Technical Definition**: An optimization technique that combines multiple render meshes sharing identical material instances into a single combined vertex/index buffer, reducing CPU-to-GPU draw calls and driver state transitions.
  - *Static Batching*: Combines non-moving geometry at build/load time.
  - *Dynamic Batching*: Transforms small moving vertices on the CPU every frame.
- **DEADeye Context**: Used extensively on rooftop HVAC ductwork, antennas, parapets, and concrete barriers. By sharing standard URP materials across urban props, rooftop draw calls on Quest 2 are kept well below 150 per frame.

#### Bounding Volume Hierarchy (BVH)
- **Simple Definition**: A hierarchy of invisible nested cardboard boxes surrounding 3D models so the computer can instantly check if a bullet hit something without checking every microscopic triangle.
- **Technical Definition**: A tree data structure on a set of geometric objects where all leaf nodes represent primitive triangles or colliders, and internal nodes are bounding boxes enclosing their subtrees. The physics engine traverses this tree in $O(\log N)$ time during raycasts.
- **DEADeye Context**: The PhysX 5.x engine utilizes BVH trees across all skyscraper building colliders. When `DEADeyeSniperRifle.cs` fires a raycast, PhysX quickly eliminates 99% of distant buildings before testing collision with the target's exact collider.

---

### C

#### C# (C-Sharp)
- **Simple Definition**: The modern, strongly typed computer programming language used to write all instructions, mechanics, and rules in the DEADeye project.
- **Technical Definition**: A modern, object-oriented, type-safe programming language developed by Microsoft and conforming to ECMA-334 and ISO/IEC 23270. It runs on the Common Language Runtime (CLR) and compiles down to Intermediate Language (MSIL), which Unity further compiles to native machine code via IL2CPP.
- **DEADeye Context**: All 58 core project scripts in `Assets/DEADeye/Scripts/` are authored in C# 9.0/10.0, utilizing modern language features including pattern matching, null-coalescing expressions, and expression-bodied members.

#### Canvas
- **Simple Definition**: The invisible 2D sheet of glass upon which game menus, crosshairs, ammo counters, and health bars are drawn.
- **Technical Definition**: The root component of Unity UI (uGUI) that holds and positions all UI elements. Can operate in three modes: `ScreenSpaceOverlay` (renders flat over the screen), `ScreenSpaceCamera` (projected at a distance from a designated camera), or `WorldSpace` (positioned as a physical billboard in 3D world coordinates).
- **DEADeye Context**: Managed by `DEADeyeTacticalHUD.cs`. In desktop mode, it renders in `ScreenSpaceOverlay`. When `DEADeyePlayerController.cs` detects VR mode on Quest 2, it dynamically converts the canvas to `ScreenSpaceCamera` at a plane distance of $1.0\text{ m}$ to prevent stereoscopic eye strain.

#### CanvasScaler
- **Simple Definition**: A smart zoom manager that shrinks or enlarges the user interface so text and buttons look the same size on a tiny phone screen, a VR headset, or a massive 4K monitor.
- **Technical Definition**: A component attached to a `Canvas` that determines how UI elements are dynamically scaled based on screen resolution, physical DPI, or constant pixel density using scale modes such as `ScaleWithScreenSize`.
- **DEADeye Context**: Configured on `DEADeyeHUD_Canvas` with reference resolution $1920\times1080$ and screen match mode set to Match Width Or Height ($0.5$ weight), ensuring the tactical terminal is crisp across both PC monitors and Quest 2 eye displays.

#### CharacterController
- **Simple Definition**: A specialized collision bubble designed specifically for walking human characters that can climb stairs and slide smoothly along walls without getting knocked around like a physics ball.
- **Technical Definition**: A physics-integrated motor component provided by Unity that performs collision detection and movement without being affected by external forces, impulses, or joint torques. It exposes `.Move(Vector3 motion)` which resolves non-penetrating sliding against static colliders.
- **DEADeye Context**: The core locomotion driver in `DEADeyePlayerController.cs`. Configured with `height = 1.8f`, `radius = 0.4f`, and `slopeLimit = 45f`, enabling the sniper to navigate rooftop slopes, bridge ramps, and catwalk thresholds without clipping.

#### Clipping Planes (Near / Far)
- **Simple Definition**: The closest and furthest distances a virtual camera can see. Anything closer than the Near plane or farther than the Far plane is cut off and invisible.
- **Technical Definition**: The near and far bounding planes of a camera's view frustum in view space. Geometry closer than $z_{\text{near}}$ or further than $z_{\text{far}}$ is clipped by the GPU rasterizer during homogeneous clip-space projection.
- **DEADeye Context**: Main Player Camera uses $z_{\text{near}} = 0.05\text{ m}$ and $z_{\text{far}} = 2000.0\text{ m}$. The optical scope camera (`ScopeVirtualCamera`) uses an extended far clip plane of $3500.0\text{ m}$ to guarantee that hostile snipers on distant transmission towers remain fully visible when zoomed in.

#### Collider
- **Simple Definition**: An invisible physical shape (box, pill, or sphere) wrapped around an object that stops other objects from walking or passing right through it.
- **Technical Definition**: A geometric shape component attached to a `GameObject` that allows the PhysX engine to calculate contact points, friction, penetration depth, and trigger overlap events.
- **DEADeye Context**: Standard urban buildings utilize `BoxCollider` and `MeshCollider` components. Hostile human targets (`DEADeyeHostileAI.cs`) feature segmented colliders: a `CapsuleCollider` for torso/legs ($1\times$ damage) and a child `SphereCollider` for the head ($3\times$ critical headshot damage).

#### Component
- **Simple Definition**: A modular Lego block of functionality attached to a GameObject that gives it specific abilities (e.g., sound, movement, graphics, or script logic).
- **Technical Definition**: The base class (`UnityEngine.Component`) for anything that can be attached to a `GameObject`. Components define behavior, transformation, rendering, audio, and physics in Unity's hybrid Component-based architecture.
- **DEADeye Context**: `DEADeyeSniperRifle` is a custom `MonoBehaviour` component attached to the `Rifle_Root` GameObject alongside standard engine components like `Transform`, `AudioSource`, and `MeshRenderer`.

#### Coroutine
- **Simple Definition**: A function that can hit the "pause" button, wait for a specified number of seconds or frames, and then pick up right where it left off without freezing the entire game.
- **Technical Definition**: A method returning an `IEnumerator` whose execution can be suspended (`yield return`) across multiple frame updates. The engine advances the enumerator each frame during its execution loop until iteration finishes, providing asynchronous-style execution on the main thread without multithread race conditions.
- **DEADeye Context**: Used extensively in `DEADeyeSniperRifle.cs` for reload timing:
  ```csharp
  private IEnumerator ReloadRoutine() {
      isReloading = true;
      yield return new WaitForSeconds(reloadDuration);
      currentAmmo = magazineCapacity;
      isReloading = false;
  }
  ```

---

### D

#### Delta Time (`Time.deltaTime`)
- **Simple Definition**: The exact fraction of a second that passed between the previous picture on the screen and the current one.
- **Technical Definition**: A static read-only float property representing the completion time in seconds since the last frame. Multiplying movement speeds by `Time.deltaTime` normalizes displacement per second rather than per frame, rendering game speed frame-rate independent.
- **DEADeye Context**: Used in `DEADeyePlayerController.cs` and `DEADeyeMouseLook.cs`:
  ```csharp
  transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
  ```
  Guarantees the sniper walks at exactly $4.5\text{ m/s}$ whether the game runs at $60\text{ FPS}$ on desktop or $72\text{ FPS}$ on Quest 2.

#### Depth Buffer (Z-Buffer)
- **Simple Definition**: A hidden 2D chalkboard where the graphics card writes down how close every pixel is to the camera, so closer walls cover up farther walls instead of showing through them.
- **Technical Definition**: A dedicated 2D memory buffer whose dimensions match the render target. It stores normalized floating-point depth values ($z \in [0, 1]$) for every rasterized fragment. The hardware performs depth testing against this buffer before writing colors, discarding occluded fragments.
- **DEADeye Context**: The URP Forward+ renderer on Quest 2 utilizes a 24-bit depth buffer to prevent visual Z-fighting between overlapping skyscraper facades and bridge decking.

#### Direct Hardware Polling
- **Simple Definition**: Checking the physical buttons and joysticks on a VR controller directly by asking the hardware chip, rather than waiting for an operating system message.
- **Technical Definition**: Reading the state of input peripherals directly via low-level API queries (e.g., `UnityEngine.XR.InputDevices.GetDeviceAtXRNode`) in every frame update, bypassing intermediate action mapping pipelines.
- **DEADeye Context**: Implemented in `DEADeyeInputRouter.cs` via `PollVRHardwareDirectly()`. Queries the right Touch controller primary button (`CommonUsages.primaryButton`) and triggers directly, providing robust fallback input if the OpenXR action map fails to initialize.

#### Draw Call
- **Simple Definition**: A phone call from the computer's CPU telling the GPU: "Here is a batch of triangles and a paint color—draw them right now!"
- **Technical Definition**: A command emitted by the CPU graphics driver to the graphics hardware API (DirectX, Vulkan, or OpenGL ES) instructing it to render a specified set of vertices using a defined shader program and texture state.
- **DEADeye Context**: Desktop scenes tolerate 2,000+ draw calls, but mobile Quest 2 VR requires draw calls to stay below 150–200 per frame. Achieved in DEADeye by combining building materials and using static batching across rooftop structures.

#### Dual-Pass Raycasting
- **Simple Definition**: Firing two laser pointers instead of one: first from the player's eye to see what they are aiming at, and second from the gun's barrel to make sure they aren't shooting straight into a brick wall.
- **Technical Definition**: A two-stage spatial line-segment intersection algorithm designed to eliminate parallax errors in first-person targeting while enforcing realistic spatial occlusion from gun muzzle positions.
- **DEADeye Context**: The signature ballistics system in `DEADeyeSniperRifle.cs`. Pass 1 casts from camera center to find the target point in space. Pass 2 casts from the physical rifle muzzle to that target point. If an obstacle interrupts Pass 2, the bullet strikes the obstacle rather than teleporting through it.

```
[Camera Eye] ====== Pass 1: Viewport Ray (0.5, 0.5) =======> [Enemy Target]
      |                                                           ^
      | (Physical Offset)                                         |
      v                                                           |
[Rifle Muzzle] ===== Pass 2: Obstruction Ray =====================+
               (Blocked if a parapet or barrier is in between)
```

#### Dual-Rig Architecture
- **Simple Definition**: Having two complete player bodies in the scene file—one for playing with keyboard/mouse at a desk, and one for playing inside a VR headset—and turning on the right one automatically.
- **Technical Definition**: A system design pattern where separate GameObject hierarchies represent the player for desktop execution (`Desktop_FPSRig`) and virtual reality execution (`VR_XROriginRig`). A central coordinator evaluates runtime hardware presence, activates the appropriate rig, disables the other, and binds shared weapon/UI subsystems.
- **DEADeye Context**: Enforced by `DEADeyePlayerController.cs`. When OpenXR detects a Quest 2 headset, `Desktop_FPSRig` is completely disabled (shutting down mouse look and desktop cameras) and `VR_XROriginRig` is enabled, binding the sniper rifle to the right controller hand socket.

---

### E

#### Edge Detection (Obstruction Raycast)
- **Simple Definition**: A safety check that detects if your sniper rifle is resting so close to the edge of a rooftop wall that the barrel is clipped inside the concrete.
- **Technical Definition**: A physical geometric query casting short raycasts radially or linearly forward from the weapon's muzzle point to verify that the gun barrel maintains an unpenetrated line-of-sight past local geometry boundaries.
- **DEADeye Context**: Built into `DEADeyeSniperRifle.cs` via `CheckMuzzleClearance()`. If the player presses up against a rooftop parapet, the obstruction ray detects the wall within $0.3\text{ m}$, preventing the weapon from firing and playing a low-pitched obstacle warning sound.

#### EditMode Test
- **Simple Definition**: An automated code check that runs instantly inside the editor without launching the game, verifying that files, math, and project settings are correct.
- **Technical Definition**: A unit or integration test executed within the Unity Editor environment outside of runtime play simulation. Tests run in a single frame, instantiate isolated scene objects or scriptable objects, verify invariants, and clean up immediately.
- **DEADeye Context**: 27 verified tests in `Assets/DEADeye/Tests/EditMode/DEADeyeIntegrationTests.cs`, checking mission definitions, weapon reload math, ammo boundaries, audio clip assignments, and camera near-clip plane values.

#### Entity Component System (ECS)
- **Simple Definition**: A data-first way of organizing video games where objects are just ID numbers, their properties are pure data arrays, and systems do the thinking on thousands of objects at once.
- **Technical Definition**: An architectural pattern that separates domain data from behavior, organizing state into contiguous memory buffers (Components) associated with lightweight IDs (Entities) and processed sequentially by cache-friendly algorithmic loops (Systems).
- **DEADeye Context**: While Unity provides DOTS/ECS for mass simulation, DEADeye deliberately uses classic `MonoBehaviour` object-oriented components for all 58 scripts. This ensures compatibility with standard OpenXR interaction tooling, NavMesh navigation agents, and simple viva explainability.

#### EventSystem
- **Simple Definition**: The invisible traffic officer that listens for mouse clicks, keyboard presses, or VR laser pointers and routes them to the correct user interface button.
- **Technical Definition**: A manager component in Unity responsible for coordinating input events across UI elements. It manages raycasters (`GraphicRaycaster`, `TrackedDeviceGraphicRaycaster`) and dispatches pointer enter, exit, click, and drag events to UI components.
- **DEADeye Context**: Configured in `RooftopMission.unity` on the `EventSystem` GameObject. Utilizes both the `StandaloneInputModule` (desktop) and `XRUIInputModule` (VR), allowing seamless menu navigation in both environments.

---

### F

#### Field of View (FOV)
- **Simple Definition**: How wide of an angle the virtual camera can see. A wide FOV shows lots of scenery on the sides; a narrow FOV acts like binoculars, zooming in on far objects.
- **Technical Definition**: The angular extent of the observable world seen through a virtual camera, typically measured vertically ($\text{vFOV}$) or horizontally ($\text{hFOV}$) in degrees. Narrowing the FOV magnifies the projected image on the view plane.
- **DEADeye Context**: Desktop camera runs at default $75^\circ\text{ FOV}$. When optical scope zoom is engaged via RMB or Touch Grip, `DEADeyeSniperRifle.cs` interpolates the scope camera down to $12^\circ\text{ FOV}$, producing an optical $6.25\times$ telescopic magnification of urban targets.

#### Finite State Machine (FSM)
- **Simple Definition**: A brain system that forces an enemy or game rule to be in only one mood or activity at a time—like "Patrolling", "Investigating", or "Shooting".
- **Technical Definition**: A computational model consisting of a finite number of discrete states, transition conditions, and execution actions. The machine resides in exactly one state at any given moment and transitions between states in response to external inputs or internal timers.
- **DEADeye Context**: Hostile enemy guards in `DEADeyeHostileAI.cs` operate on an explicit FSM:
  ```
  [Patrol] ----(Player Spotted)----> [Alert] ----(In Range/LOS)----> [Engage]
     ^                                 |                                 |
     +---(Target Lost/Timer Expired)---+---(Damage Taken / Flee)--------> [TakeCover]
  ```
  Eliminates erratic behavior, guarantees predictable AI states, and allows faculty to inspect clear state-machine code.

#### Fire Gate
- **Simple Definition**: A safety lock on the sniper rifle that refuses to fire unless the player is actively looking through the telescopic scope.
- **Technical Definition**: A boolean conditional barrier in the primary weapon trigger logic that verifies `isScoped == true` before executing ballistic raycasting, consuming ammunition, or triggering muzzle flash effects.
- **DEADeye Context**: Enforced in `DEADeyeSniperRifle.cs`:
  ```csharp
  public void Fire() {
      if (!isScoped) {
          audioController.PlayDryFire();
          return; // Fire gate: unscoped firing rejected
      }
      ExecuteRaycastShot();
  }
  ```
  Reinforces the tactical sniper identity, stops players from run-and-gun hip-firing, and is verified by automated test `Test_FireGate_BlocksUnscopedShot`.

#### Forward+ Rendering
- **Simple Definition**: A modern graphics drawing method that lets video games shine dozens of real-time lights without slowing down or burning out mobile VR processors.
- **Technical Definition**: A rendering technique that splits screen space into a grid of 2D or 3D frustum tiles. The engine calculates which lights affect each tile during a depth pre-pass, allowing the subsequent forward shading pass to iterate only over lights intersecting that specific tile.
- **DEADeye Context**: Selected in URP settings for DEADeye. Allows urban street lamps, emergency vehicle beacons, and jammer warning strobes across the district without exceeding Quest 2's mobile GPU fill-rate limits.

#### Frustum Culling
- **Simple Definition**: The graphics card refusing to draw anything that is behind the player's back or outside their field of view.
- **Technical Definition**: The hardware and engine optimization process of testing 3D object bounding boxes against the six planes defining the camera's view frustum. Objects situated entirely outside the frustum are culled before rasterization, eliminating wasted vertex and fragment shading.
- **DEADeye Context**: Enabled automatically by Unity's rendering pipeline. By turning away from the vast urban downtown cityscape and facing the brick parapet, draw calls drop from 180 to 45 on Quest 2.

---

### G

#### GameObject
- **Simple Definition**: The fundamental building block of everything in Unity. An empty container that can become a player, a gun, a building, or a light by adding components to it.
- **Technical Definition**: The fundamental class (`UnityEngine.GameObject`) representing entities in the Unity hierarchy. A GameObject possesses an immutable `Transform` component and acts as a container for attaching `Component` instances that provide state and behavior.
- **DEADeye Context**: Every entity in `RooftopMission.unity`—from the `MilitaryJammer_Tower` to `Sniper_Rifle_Mk1`—is a GameObject instantiated in the scene graph.

#### Garbage Collection (GC)
- **Simple Definition**: A background street-sweeper program that periodically cleans up unused computer memory so the game doesn't run out of RAM.
- **Technical Definition**: An automated memory management feature of the Common Language Runtime (CLR) that reclaims heap memory allocated to objects no longer referenced by active execution threads. Stop-the-world GC sweeps introduce millisecond latency spikes (frame drops).
- **DEADeye Context**: Fatal in VR if it drops frames below $72\text{ Hz}$. DEADeye scripts maintain **zero garbage collection per frame** inside `Update()` by pre-allocating reusable physics arrays (`RaycastHit[] hits = new RaycastHit[10]`) and using `Physics.RaycastNonAlloc()` instead of allocating temporary memory.

#### GraphicRaycaster
- **Simple Definition**: A laser beam that shoots out from your mouse cursor or VR hand pointer to detect which user interface button you are pointing at.
- **Technical Definition**: A component attached to a `Canvas` that projects rays into screen space or world space to identify UI elements implementing `ICanvasRaycastFilter` or derived from `Graphic` (such as Buttons, Text, and Images).
- **DEADeye Context**: Used on `DEADeyeHUD_Canvas`. On desktop, standard `GraphicRaycaster` handles mouse clicks; on Quest 2, `TrackedDeviceGraphicRaycaster` from the XR Interaction Toolkit interprets Touch controller pointer positions.

---

### H

#### Hitscan
- **Simple Definition**: An instant laser-beam bullet. The instant you pull the trigger, the game draws an invisible straight line to see what you hit, with zero bullet travel time.
- **Technical Definition**: A ballistics simulation technique where projectile trajectories are modeled as mathematical line segments or rays calculated instantaneously within the current physics frame, rather than simulating physical rigid bodies over time with mass, drag, and gravity.
- **DEADeye Context**: Standard sniper rifle firing in `DEADeyeSniperRifle.cs` uses hitscan via `Physics.Raycast()`. Because tactical sniper distances in Operation DEAD AIR are within $300\text{ m}$, instantaneous raycasting provides crisp, deterministic hit registration without the physics synchronization overhead of projectile colliders.

#### Head-Mounted Display (HMD)
- **Simple Definition**: The virtual reality headset you wear on your face, equipped with internal screens, lenses, and motion sensors.
- **Technical Definition**: A display device worn on the head with separate optical channels for each eye, presenting stereoscopic images with binocular disparity to simulate human 3D depth perception, tracked by motion sensors.
- **DEADeye Context**: Targeted specifically at the Meta Quest 2 standalone HMD, featuring dual fast-switch LCD panels ($1832\times1920$ resolution per eye) driven by the Qualcomm Snapdragon XR2 Gen 1 platform.

#### Head Tracking (6DoF)
- **Simple Definition**: The ability of the VR headset to track both where you look (up, down, left, right) AND where you move your body (leaning, ducking, walking forward).
- **Technical Definition**: Six Degrees of Freedom tracking measuring three rotational axes (Pitch, Yaw, Roll) and three translational axes (Surge along X, Heave along Y, Sway along Z), calculated at high frequency via internal IMUs and external/inside-out camera fusion.
- **DEADeye Context**: Fully supported in DEADeye via OpenXR and `TrackedPoseDriver`. The player can physically duck behind concrete rooftop barriers on Quest 2 to avoid hostile sniper fire.

---

### I

#### IL2CPP (Intermediate Language to C++)
- **Simple Definition**: A super-translator that takes your easy-to-read C# code, turns it into lightning-fast C++ code, and compiles it straight into native machine language for Android and Quest.
- **Technical Definition**: A Unity-developed ahead-of-time (AOT) compilation technology that converts managed C# MSIL (Microsoft Intermediate Language) bytecode into highly optimized native C++ source files, which are then compiled into native machine code (ARM64 `.so`) using platform-native compilers (such as Clang/LLVM).
- **DEADeye Context**: Used for all release Android APK builds. IL2CPP delivers $2\times$ to $3\times$ faster math and physics execution on the Quest 2's Snapdragon CPU compared to older Mono JIT engines.

#### Input Action
- **Simple Definition**: A named game command (like "Fire", "Jump", or "Look") that you can bind to any physical button on a keyboard, mouse, or VR controller.
- **Technical Definition**: An abstraction within the Unity New Input System that represents a discrete user intent. An action can be bound to multiple physical control paths across varying hardware devices and outputs normalized values (Button, Value, Pass-Through).
- **DEADeye Context**: DEADeye defines actions including `FireAction` (bound to `LeftButton` on mouse, `rightHand/trigger` on Quest), `ScopeAction` (`RightButton` / `rightHand/grip`), and `ReloadAction` (`Key/R` / `rightHand/primaryButton`).

#### Inside-Out Tracking
- **Simple Definition**: VR tracking where cameras built directly into the headset watch the room around you, eliminating the need for external tripods or lighthouse sensors.
- **Technical Definition**: A spatial tracking technique where optical cameras mounted on the HMD continuously analyze ambient visual features and surface contrasts in the surrounding physical room, fusing visual-inertial odometry (VIO) data with high-frequency IMU sensors to compute real-time 6DoF camera poses.
- **DEADeye Context**: Used natively by Meta Quest 2's four wide-angle corner tracking cameras to position the player's view within `RooftopMission.unity` without external beacons.

---

### J

#### Jitter
- **Simple Definition**: Tiny, annoying high-frequency shaking or vibrating of an object or camera, usually caused by bad math or conflicting input updates.
- **Technical Definition**: High-frequency spatial or temporal noise in positional or rotational data. Commonly caused by floating-point precision loss, asynchronous frame rate mismatches between input polling and graphics rendering, or competing physics solvers.
- **DEADeye Context**: Completely solved in `DEADeyeMouseLook.cs` through deterministic zero-drift clamping and unscaled delta-time smoothing, ensuring sniper crosshairs remain rock-steady while aiming at long distances.

---

### K

#### Kinematic Rigidbody
- **Simple Definition**: A physics object that can push other things around, but refuses to be pushed, bumped, or dropped by gravity.
- **Technical Definition**: A `Rigidbody` component whose `isKinematic` property is set to `true`. The PhysX engine does not apply forces, gravity, or friction to kinematic bodies; their position and orientation are driven exclusively via script or animation curves while still triggering collision events on dynamic bodies.
- **DEADeye Context**: Assigned to moving catwalk platform doors and security barriers. Prevents physics glitches from pushing barriers out of alignment while allowing them to block bullets and player movement.

---

### L

#### Layer / LayerMask
- **Simple Definition**: Digital colored tags attached to objects so you can tell the physics engine: "Only check if this bullet hits objects tagged as 'Enemy' or 'Wall', and ignore the rest."
- **Technical Definition**: A 32-bit bitfield in Unity used to categorize GameObjects for selective physics raycasting, collision matrices, and camera rendering culling masks.
- **DEADeye Context**: `DEADeyeSniperRifle.cs` constructs a `LayerMask`:
  ```csharp
  int targetLayers = LayerMask.GetMask("Default", "HostileAI", "Environment", "Obstacles");
  ```
  Ensures bullet rays interact with physical world geometry while ignoring trigger volumes, UI planes, and player body colliders.

#### Level of Detail (LOD)
- **Simple Definition**: Swapping a highly detailed 3D model for a simpler, blockier version when it is far away from the player to save computer power.
- **Technical Definition**: A rendering optimization technique where an object is modeled at multiple discrete geometric complexities ($LOD_0, LOD_1, LOD_2$). A `LODGroup` component switches between meshes based on the percentage of screen height the object occupies.
- **DEADeye Context**: Applied to distant urban architecture and skyline assets. Skyscraper models drop from 1,200 polygons at $LOD_0$ to 150 polygons at $LOD_2$ beyond $150\text{ m}$, conserving vertex processing on Quest 2.

#### Linear Color Space
- **Simple Definition**: A math system that blends light colors accurately according to real-world physics, making sunlight and shadows look natural rather than washed out or artificially dark.
- **Technical Definition**: A color calculation workflow where colors and lighting equations are evaluated linearly ($1 + 1 = 2$) before applying gamma correction ($sRGB$ curve $\gamma \approx 2.2$) prior to display output.
- **DEADeye Context**: Configured in Player Settings (`ColorSpace = Linear`). Ensures the harsh midday sunlight across the rooftop scene produces realistic shadow falloffs and highlights on the sniper rifle's matte metal barrel.

---

### M

#### Material
- **Simple Definition**: The digital paint recipe that defines what an object looks like—its color, shininess, roughness, and metal reflection.
- **Technical Definition**: An asset encapsulating a shader program and its associated parameter states, including texture maps (albedo, normal, metallic, smoothness), tint colors, and render queue assignments.
- **DEADeye Context**: Built using URP Universal Render Pipeline shaders (`Universal Render Pipeline/Lit` and `Universal Render Pipeline/Unlit`). Includes custom non-reflective matte materials for tactical weaponry (`Sniper_Matte_Dark`) to prevent distracting glare.

#### Mesh Filter & Mesh Renderer
- **Simple Definition**: The 3D blueprint (Mesh Filter) and the 3D painter (Mesh Renderer) that work together to put a shape on the screen.
- **Technical Definition**: 
  - `MeshFilter`: References a 3D geometric asset (`Mesh`) containing vertex buffers, normal vectors, UV coordinates, and triangle index lists.
  - `MeshRenderer`: Takes that geometry and submits draw calls to the graphics API using the assigned `Material`.
- **DEADeye Context**: Every building, catwalk beam, and weapon component in DEADeye relies on this pairing to display 3D models imported from Quaternius and LowPolyGuns asset packs.

#### MonoBehaviour
- **Simple Definition**: The master base class in Unity that gives any C# script the power to attach to GameObjects and listen to engine events like `Start()` and `Update()`.
- **Technical Definition**: The abstract base class from which every standard Unity script derives. It connects managed C# classes to native engine C++ representations, providing lifecycle hooks (`Awake`, `Start`, `Update`, `FixedUpdate`, `OnTriggerEnter`), coroutine scheduling, and inspector serialization.
- **DEADeye Context**: All 58 gameplay, AI, and mission scripts in DEADeye inherit directly or indirectly from `MonoBehaviour`.

#### Muzzle Flash
- **Simple Definition**: The burst of flame and smoke that erupts from the barrel of a gun the instant a bullet is fired.
- **Technical Definition**: A visual and lighting effect triggered synchronously with weapon discharge, typically composed of a short-duration particle system, an animated billboard quad, and a transient point light source.
- **DEADeye Context**: Driven by `DEADeyeSniperRifle.cs`. Instantiates a transient visual particle burst at `muzzlePoint.position` and enables a point light for $0.05\text{ seconds}$, illuminating the immediate rooftop environment when firing.

---

### N

#### NavMesh (Navigation Mesh)
- **Simple Definition**: An invisible flat 2D/3D floor plan baked over all walkable surfaces that tells computer-controlled characters where they can and cannot walk.
- **Technical Definition**: A spatial data structure representing the walkable surfaces of a 3D environment as a convex polygon mesh. Used by pathfinding systems to compute shortest paths via the $A^*$ search algorithm.
- **DEADeye Context**: Baked via `NavMeshSurface` across the rooftop, catwalks, and bridge networks in `RooftopMission.unity`. Bridges and catwalks are explicitly flagged as walkable surfaces with custom area costs, guiding AI patrol routes.

#### NavMeshAgent
- **Simple Definition**: The invisible autopilot driver attached to an enemy character that steers them along the NavMesh, avoiding walls and reaching destination targets.
- **Technical Definition**: A component that navigates a GameObject across a `NavMesh`, handling path calculation, path following, collision avoidance with other agents (via Reciprocal Velocity Obstacles), and velocity control.
- **DEADeye Context**: Attached to hostile patrol guards (`DEADeyeHostileAI.cs`) with `speed = 3.5f`, `stoppingDistance = 1.0f`, and `acceleration = 8.0f`, allowing enemies to realistically sprint between urban cover perches.

#### NavMeshSurface
- **Simple Definition**: The modern tool in Unity used to scan a 3D level and generate the walkable navigation floor plan with one click.
- **Technical Definition**: A high-level component from the `com.unity.ai.navigation` package that bakes and runtime-manages a NavMesh for specific agent types (e.g., Humanoid) based on scene layer masks and collider geometry.
- **DEADeye Context**: Positioned at the root of `RooftopMission.unity`. Configured with Humanoid parameters (`Agent Radius = 0.4`, `Agent Height = 1.8`, `Max Slope = 45°`), generating a compact $1.2\text{ MB}$ navigation asset.

#### NUnit
- **Simple Definition**: The automated testing framework that runs all your code verification tests and produces a clean report showing which passed and which failed.
- **Technical Definition**: A popular open-source unit testing framework for .NET languages, integrated directly into the Unity Test Framework package (`com.unity.test-framework`). It provides assertions, test fixtures, parameterized test runners, and test attributes (`[Test]`, `[UnityTest]`, `[SetUp]`).
- **DEADeye Context**: Powers all 51 automated tests in DEADeye (`DEADeyeIntegrationTests.cs`, `DEADeyeAccuracyTests.cs`, `DEADeyePlayModeTests.cs`).

---

### O

#### Occlusion Culling
- **Simple Definition**: Refusing to draw buildings or objects that are completely hidden behind a closer, solid wall right in front of you.
- **Technical Definition**: A rendering visibility determination process that disables rendering for objects obscured by other opaque geometry (occluders). Performed using pre-baked binary space partitioning (BSP) cell grids or dynamic hardware occlusion queries.
- **DEADeye Context**: Essential for urban environments. When the player stands on the rooftop facing a high elevator shaft wall, occlusion culling stops the GPU from rendering the miles of skyscrapers sitting directly behind that wall.

#### OpenXR
- **Simple Definition**: An open industry standard that lets you write VR code once and run it across any headset—Meta Quest, HTC Vive, Valve Index, or Apple Vision Pro—without rewriting everything.
- **Technical Definition**: An open, royalty-free API standard developed by the Khronos Group providing high-performance access to XR hardware runtimes. It abstracts hardware differences behind standardized interaction profiles, tracking spaces, and swapchain graphics management.
- **DEADeye Context**: The sole XR runtime used by DEADeye (`com.unity.xr.openxr` v1.16.1). Enabled in Project Settings for Android with the Meta Quest Support feature group enabled, driving standalone Quest 2 tracking.

#### Optical Scope
- **Simple Definition**: A telescopic rifle sight with magnifying glass lenses that lets a sniper see far-away targets up close.
- **Technical Definition**: A specialized dual-camera rendering system. A secondary camera with a narrow field of view renders its output into an offscreen `RenderTexture`, which is then projected onto the circular reticle lens mesh of the 3D sniper rifle model.
- **DEADeye Context**: Managed by `DEADeyeOpticalScope.cs`. Operates synchronously with `DEADeyeSniperRifle.cs`. Blends optical zoom magnification with an outer physical scope housing, preserving peripheral situational awareness.

#### Overdraw
- **Simple Definition**: Wasting computer energy by painting the exact same pixel on the screen multiple times in a single frame because objects are stacked behind each other.
- **Technical Definition**: A GPU performance bottleneck occurring when a single pixel fragment on the screen is shaded multiple times per frame, usually caused by dense overlapping transparent surfaces, particle systems, or unculled forward-rendered geometry.
- **DEADeye Context**: Kept minimal on Quest 2 by utilizing fully opaque shaders on urban buildings and restricting transparent smoke/fire particle counts around destroyed jammer towers.

---

### P

#### Parallax Error
- **Simple Definition**: The visual mismatch that happens because your eyes are in your head, but your gun is down in your hands. If you aim through the gun barrel, you might shoot a railing you didn't realize was in the way.
- **Technical Definition**: The angular displacement in perceived target position resulting from the physical offset between the camera viewpoint (line-of-sight) and the weapon muzzle (trajectory origin).
- **DEADeye Context**: Completely resolved in DEADeye using **Dual-Pass Raycasting**. Pass 1 identifies the target at the center of the scope view; Pass 2 traces the bullet trajectory from the physical muzzle to that point, guaranteeing physical accuracy without visual drift.

#### Physics Raycast (`Physics.Raycast`)
- **Simple Definition**: Casting an invisible laser pointer through the 3D world to check if it hits any solid physical object and report where it struck.
- **Technical Definition**: A static spatial query method in Unity's physics engine (`UnityEngine.Physics`) that projects an infinitely thin geometric line segment from an origin vector along a direction vector up to a maximum distance, returning a boolean indicating collision with any active colliders on the specified `LayerMask`.
- **DEADeye Context**: The backbone of DEADeye ballistics. `DEADeyeSniperRifle.cs` invokes `Physics.Raycast(origin, direction, out hit, maxRange, targetMask)` to execute instantaneous hitscan shots.

#### Pitch, Yaw, Roll
- **Simple Definition**: The three ways an object can turn:
  - *Pitch*: Nodding your head up and down.
  - *Yaw*: Shaking your head left and right.
  - *Roll*: Tilting your head toward your shoulders.
- **Technical Definition**: The three Tait-Bryan angles describing rotational orientation in three-dimensional space relative to a local reference frame:
  - *Pitch*: Rotation around the lateral (X) axis.
  - *Yaw*: Rotation around the vertical (Y) axis.
  - *Roll*: Rotation around the longitudinal (Z) axis.
- **DEADeye Context**: In `DEADeyeMouseLook.cs`, horizontal mouse movement drives player body Yaw (around global Y-axis), while vertical mouse movement drives camera Pitch (around local X-axis) clamped between $-85^\circ$ and $+85^\circ$ to prevent camera flipping.

```
       Pitch (X-Axis: Look Up/Down)
              ^
              |      Yaw (Y-Axis: Look Left/Right)
              |     /
              |   /
              +------> Roll (Z-Axis: Tilt Shoulders)
```

#### PlayMode Test
- **Simple Definition**: An automated game test that actually starts the game engine, advances real game time frame-by-frame, and tests physics, enemy AI, and shooting just like a human player would.
- **Technical Definition**: An integration test running within an active Unity runtime simulation (`[UnityTest]`). It can yield execution across multiple frames (`yield return null`) or wait for physical time intervals (`yield return new WaitForSeconds(1.0f)`), allowing evaluation of physics, movement, and coroutine lifecycles.
- **DEADeye Context**: 24 verified tests in `Assets/DEADeye/Tests/PlayMode/` covering sniper accuracy at $50\text{ m}$, headshot multiplier math, fall recovery triggers, and hostile AI state transitions.

#### Prefab (Pre-fabricated GameObject)
- **Simple Definition**: A saved master template of a game object that you can copy-paste into your scene dozens of times. If you change the master template, all copies update automatically.
- **Technical Definition**: A serialized asset format (`.prefab`) storing a complete GameObject hierarchy with all attached components, property values, and asset links. Supports bidirectional property overriding and inheritance.
- **DEADeye Context**: All enemies (`Hostile_Guard_Sniper.prefab`), weapons (`Sniper_Rifle_Mk1.prefab`), and urban structures (`Skybridge_Segment.prefab`) are stored as prefabs in `Assets/DEADeye/Prefabs/`.

#### Procedural Animation
- **Simple Definition**: Animating 3D character bodies using mathematical code equations (like sine waves and angles) rather than playing pre-recorded motion-capture cartoon clips.
- **Technical Definition**: Generating bone transforms, rotations, and positions algorithmically at runtime using mathematical functions, physics joints, or inverse kinematics (IK) rather than sampling baked keyframe animation clips.
- **DEADeye Context**: Implemented in `DEADeyeProceduralCharacterAnimator.cs`. Because the project avoids bloated third-party animation clips to conserve APK size, hostile guards dynamically sway, walk, and aim using mathematical sine-wave joint oscillations.

---

### Q

#### Quaternion
- **Simple Definition**: A mathematical system using four numbers that computers use to smoothly turn and rotate 3D objects without getting their angles tangled up in a knot.
- **Technical Definition**: A four-dimensional hypercomplex number system ($q = w + xi + yj + zk$) representing spatial orientations in three-dimensional Euclidean space. Quaternions eliminate **Gimbal Lock** (loss of a rotational degree of freedom when two axes align) and provide computationally efficient, non-distorting spherical linear interpolation (SLERP).
- **DEADeye Context**: Every rotation in DEADeye—from camera look orientation to weapon sway and projectile trajectories—is calculated internally using `UnityEngine.Quaternion` rather than raw Euler angles.

---

### R

#### Rasterization
- **Simple Definition**: The process of converting 3D computer models made of math and triangles into a flat 2D picture made of colored screen pixels.
- **Technical Definition**: The hardware graphics pipeline stage where continuous 3D geometric triangles in homogeneous clip space are mapped to discrete pixel fragments in screen space, evaluating vertex attributes across pixel centers via barycentric interpolation.
- **DEADeye Context**: Executed by the Adreno 650 GPU on Meta Quest 2. Highly optimized by using low-poly urban assets to reduce triangle rasterization workloads.

#### RaycastHit
- **Simple Definition**: The information report returned by the physics engine after a laser pointer hits something, telling you what it hit, where it hit, and which way the surface was facing.
- **Technical Definition**: A C# struct (`UnityEngine.RaycastHit`) populated by `Physics.Raycast()` detailing collision intersection data, including `.point` (world-space contact coordinate), `.normal` (surface perpendicular unit vector), `.distance` (ray length), and `.collider` (hit object reference).
- **DEADeye Context**: Evaluated in `DEADeyeSniperRifle.cs`:
  ```csharp
  if (hit.collider.CompareTag("Head")) {
      ApplyDamage(hit.collider.gameObject, baseDamage * headshotMultiplier);
      SpawnBloodEffect(hit.point, hit.normal);
  }
  ```

#### RectTransform
- **Simple Definition**: The 2D cousin of the standard 3D Transform, designed specifically to position, anchor, and stretch user interface boxes on a flat screen.
- **Technical Definition**: A specialized 2D transform component inheriting from `Transform` used for UI elements. It defines position, size, anchors, and pivot points relative to parent rectangular boundaries rather than arbitrary 3D coordinates.
- **DEADeye Context**: Drives all layout panels, objective trackers, and ammo counters in `DEADeyeTacticalHUD.cs`.

#### RenderTexture
- **Simple Definition**: A blank digital canvas that a virtual camera can draw a live video feed onto, which you can then stick onto a TV screen or sniper scope lens inside the game.
- **Technical Definition**: A special GPU texture asset (`UnityEngine.RenderTexture`) configured as a color and depth render target for a camera instead of outputting directly to the backbuffer/framebuffer.
- **DEADeye Context**: Configured in `Assets/DEADeye/Materials/ScopeRenderTexture.renderTexture` ($1024\times1024$ resolution). The secondary `ScopeVirtualCamera` renders its magnified view directly to this texture, which is mapped to the sniper rifle's glass lens material.

#### Rigidbody
- **Simple Definition**: The component that gives an object weight, gravity, velocity, and the ability to tumble, roll, and bounce like a real physical item.
- **Technical Definition**: The primary component integrating an entity into the PhysX dynamic physics simulation. It manages mass, linear/angular velocity, drag coefficients, center of mass, and responds to applied forces and impulses.
- **DEADeye Context**: Used on dynamic urban props like explosive barrels and concrete rubble around the military jammer tower. When shot by the sniper rifle, impulse forces scatter the debris.

#### Rooftop Recovery Volume
- **Simple Definition**: An invisible safety net wrapped beneath the rooftop level that catches the player if they accidentally fall off a ledge, teleporting them safely back to the deck.
- **Technical Definition**: A large, non-rendering trigger collider (`BoxCollider` with `isTrigger = true`) positioned below the lowest walkable rooftop geometry, monitored by `DEADeyeBoundaryTrigger.cs`.
- **DEADeye Context**: Located at $Y = -15.0\text{ m}$ in `RooftopMission.unity`. If the player steps off a catwalk or bridge edge, the trigger catches the CharacterController and resets position to `rooftopSpawnPoint` with zero velocity, preventing game-breaking infinite falls into the void.

---

### S

#### Scene
- **Simple Definition**: A single game level, room, or digital world containing all the lights, buildings, players, enemies, and sound files for that part of the game.
- **Technical Definition**: A serialized asset file (`.unity`) containing the complete environment hierarchy, component configurations, baked lighting references, and static geometry indices representing a game world state.
- **DEADeye Context**: The authoritative level for the entire project is `Assets/DEADeye/Scenes/RooftopMission.unity`.

#### Screen Space - Camera
- **Simple Definition**: A way of drawing menus where the menu is held like a sheet of clear glass at an exact distance in front of the camera lens, rotating wherever the camera turns.
- **Technical Definition**: A `Canvas` render mode where the UI plane is positioned at a fixed distance (`planeDistance`) in front of a specified camera and scaled automatically to fit the camera frustum.
- **DEADeye Context**: The mandatory Canvas mode for VR on Meta Quest 2. Prevents UI from rendering flat across one eye, providing comfortable stereoscopic depth at $1.0\text{ m}$.

#### Screen Space - Overlay
- **Simple Definition**: A way of drawing menus where the menu is slapped flat across your computer screen like a sticker on a window, completely ignoring 3D cameras.
- **Technical Definition**: A `Canvas` render mode where the UI is rendered directly to the final display frame buffer over the 3D scene render without perspective transformation.
- **DEADeye Context**: The default rendering mode for desktop play in DEADeye, providing crisp $1080\text{p}/4\text{K}$ text rendering.

#### Signed Distance Field (SDF) TextMeshPro
- **Simple Definition**: A high-tech font technology that keeps text crystal-clear and razor-sharp even when you zoom in on it with a 10x sniper scope.
- **Technical Definition**: A font rendering technique where font glyphs are pre-computed into high-resolution distance-to-edge scalar fields rather than static raster bitmaps. The fragment shader evaluates distance thresholds, producing infinitely sharp vector-quality text at any scale or distance.
- **DEADeye Context**: All tactical mission briefings, ammo counters, and compass headings utilize TextMeshPro (`com.unity.textmeshpro`) with SDF font assets.

#### Single-Pass Instanced (SPI)
- **Simple Definition**: Drawing a VR scene for both eyes at the same time in one go, rather than drawing the whole world twice.
- **Technical Definition**: A stereo rendering method where the GPU renders both left and right eye viewpoints within a single graphics pass. Shaders utilize texture arrays and GPU instancing to process geometry once, applying per-eye view-projection matrices dynamically via hardware instancing.
- **DEADeye Context**: Selected in OpenXR Project Settings for Android VR. Reduces CPU draw call overhead by almost 50% on Meta Quest 2, making consistent $72\text{ FPS}$ achievable.

#### Singleton Pattern
- **Simple Definition**: A programming rule ensuring there is only ever ONE copy of an important manager (like the Scorekeeper or Audio Manager) in the whole game, and everyone knows how to talk to it.
- **Technical Definition**: A creational design pattern that restricts the instantiation of a class to one single global instance, providing static access via an accessor property (e.g., `Instance`).
- **DEADeye Context**: Used judiciously for top-level managers including `DEADeyeMissionDirector.Instance` and `DEADeyeTacticalAudioHub.Instance`, enabling scripts across the scene to report events without rigid coupling.

#### Spatial Audio (3D Positional Audio)
- **Simple Definition**: Sound that lets you tell exactly where a gunshot or helicopter is just by listening, making audio quieter in one ear and louder in the other based on where you turn your head.
- **Technical Definition**: Audio processing techniques that simulate real-world acoustic propagation, applying interaural time differences (ITD), interaural level differences (ILD), distance attenuation, and head-related transfer functions (HRTF) to render 3D soundscapes.
- **DEADeye Context**: Configured on urban ambient sounds, hostile sniper cracks, and jammer pulses. Hostile gunshots instantly communicate enemy sniper positions to the player through directional hearing.

---

### T

#### TextMeshPro (TMP)
- **Simple Definition**: Unity's advanced text component that replaces old blurry text with beautiful, sharp, customizable letters.
- **Technical Definition**: An advanced text rendering solution for Unity utilizing Signed Distance Field (SDF) rasterization, advanced typography controls (kerning, tracking, line spacing), rich text tagging, and custom shaders.
- **DEADeye Context**: Extensively used across `DEADeyeTacticalHUD.cs` and `DEADeyeTerminalUI.cs` for tactical heads-up display readouts.

#### TrackedDeviceGraphicRaycaster
- **Simple Definition**: The special VR raycaster that lets you point your Touch controller hand at a floating menu button and pull the trigger to click it.
- **Technical Definition**: An input module component from the Unity XR Interaction Toolkit (`com.unity.xr.interaction.toolkit`) that projects spatial rays from 3D tracked controller poses into WorldSpace or ScreenSpaceCamera canvases to interact with uGUI buttons.
- **DEADeye Context**: Attached to `DEADeyeHUD_Canvas` to ensure mission selection and terminal interaction function smoothly on Meta Quest 2.

#### TrackedPoseDriver
- **Simple Definition**: A simple component that glues a virtual camera or virtual hand to the real-life movements of your VR headset or controllers.
- **Technical Definition**: A component from the XR Foundation layer that queries positional and rotational data from OpenXR tracking input features and applies them directly to a GameObject's `Transform` every frame.
- **DEADeye Context**: Drives the head camera and left/right controller hand sockets within `VR_XROriginRig`.

#### Trigger (Physics Trigger Collider)
- **Simple Definition**: An invisible boundary that doesn't stop you from walking through it, but quietly rings an alarm when you do.
- **Technical Definition**: A `Collider` component with its `isTrigger` property set to `true`. The physics engine ignores collision forces for this collider and instead invokes `OnTriggerEnter`, `OnTriggerStay`, and `OnTriggerExit` callback events on attached scripts.
- **DEADeye Context**: Used for tactical reconnaissance observation perches (`DEADeyeReconZone.cs`) and fall-recovery zones (`DEADeyeBoundaryTrigger.cs`).

---

### U

#### Universal Render Pipeline (URP)
- **Simple Definition**: Unity's modern, lightweight, highly optimized graphics engine designed to deliver great-looking games that run smoothly on everything from phones to VR headsets.
- **Technical Definition**: A pre-built scriptable render pipeline (SRP) optimized for cross-platform visual quality and performance. Utilizes a single-pass forward or Forward+ rendering architecture that significantly reduces memory bandwidth and draw call overhead on mobile tile-based GPUs.
- **DEADeye Context**: Version 17.3.0 drives all graphics in DEADeye, configured via `DEADeye_Mobile_URPAsset.asset`.

#### UPM (Unity Package Manager)
- **Simple Definition**: The built-in app store/library manager inside Unity used to download and update official tools like OpenXR, Input System, and TextMeshPro.
- **Technical Definition**: A package management system conforming to the NPM specification that downloads, installs, and manages modular engine dependencies defined in `Packages/manifest.json`.
- **DEADeye Context**: Manages core packages including `com.unity.xr.openxr`, `com.unity.xr.interaction.toolkit`, `com.unity.inputsystem`, and `com.unity.ai.navigation`.

---

### V

#### Vector3
- **Simple Definition**: A math group of three numbers $(X, Y, Z)$ representing a location in 3D space, a direction, or a movement speed.
- **Technical Definition**: A fundamental Unity struct (`UnityEngine.Vector3`) representing points, displacements, or velocities in 3D Euclidean space, with helper methods for dot products, cross products, normalization, and distance calculations.
- **DEADeye Context**: Used universally across all movement, raycasting, and ballistics calculations.

#### ViewportPointToRay
- **Simple Definition**: A math function that takes a spot on your flat screen (like dead center) and shoots a ray straight out into the 3D world from that exact pixel.
- **Technical Definition**: A method on `Camera` that transforms normalized screen viewport coordinates $(x, y \in [0, 1])$ into a 3D world-space `Ray` originating at the camera position and pointing along the projection frustum vector.
- **DEADeye Context**: Used in Pass 1 of `DEADeyeSniperRifle.cs`:
  ```csharp
  Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
  ```
  Guarantees sniper bullets aim directly at the center crosshair pixel.

#### Virtual Reality (VR)
- **Simple Definition**: A computer technology that puts you inside a 3D digital world using a motion-tracked headset, making you feel physically present in that space.
- **Technical Definition**: An interactive computer-generated experience taking place within a simulated environment, incorporating stereoscopic 3D displays, low-latency head and hand tracking, and spatialized audio to achieve psychological **immersion** and **presence**.
- **DEADeye Context**: DEADeye is designed natively for standalone VR on the Meta Quest 2.

---

### W

#### Waypoint Navigation
- **Simple Definition**: Placing invisible numbered breadcrumbs across a rooftop that computer-controlled guards walk between on patrol.
- **Technical Definition**: An AI navigation method where an agent traverses a linked sequence of discrete spatial coordinates (`Transform[] waypoints`) in order or randomly, updating target coordinates upon arriving within a defined threshold distance.
- **DEADeye Context**: Implemented in `DEADeyeHostileAI.cs`. Guards patrol along building perimeters and catwalk paths between pre-placed waypoint transforms.

#### World Space UI
- **Simple Definition**: A user interface that exists as a real physical signpost or TV screen standing inside the 3D game world, rather than floating on your display.
- **Technical Definition**: A `Canvas` render mode where the UI plane resides at arbitrary 3D spatial coordinates within the scene graph, subject to standard 3D camera projection, occlusion, and distance attenuation.
- **DEADeye Context**: Used for the tactical mission terminal (`DEADeyeTerminalUI.cs`) mounted physically on the rooftop communications server rack.

---

### X

#### XR (Extended Reality)
- **Simple Definition**: The umbrella term covering all immersive technologies: Virtual Reality (VR), Augmented Reality (AR), and Mixed Reality (MR).
- **Technical Definition**: The comprehensive superset term referring to all combined real-and-virtual environments and human-machine interactions generated by computer technology and wearables.
- **DEADeye Context**: Supported through Unity's XR Plugin Architecture and OpenXR standards.

#### XR Interaction Toolkit (XRI)
- **Simple Definition**: Unity's official toolbox for VR that makes it easy to grab virtual guns, press buttons, teleport, and interact with the virtual world.
- **Technical Definition**: A high-level, component-based interaction framework (`com.unity.xr.interaction.toolkit`) providing cross-platform interaction primitives (interactors, interactables, locomotion providers, and controllers) for OpenXR hardware.
- **DEADeye Context**: Version 3.4.0 is installed in DEADeye, providing UI raycasting and controller pose drivers.

#### XR Origin
- **Simple Definition**: The center point in the virtual world that represents where the player's physical room floor and boundaries are located.
- **Technical Definition**: The root GameObject hierarchy in Unity XR representing the center of the user's physical tracking space, containing an offset node and child camera/controller nodes.
- **DEADeye Context**: The central VR player component in `VR_XROriginRig`, coordinating head and hand tracking across the rooftop environment.

---

### Z

#### Zero-Drift Mouse Aiming
- **Simple Definition**: Mouse aiming math that never slowly creeps off to the side over time and stops the camera from ever flipping upside down.
- **Technical Definition**: An input smoothing and camera rotation algorithm that maintains explicit scalar Euler angle accumulators for pitch and yaw, clamping pitch strictly within $[-\theta_{\text{max}}, +\theta_{\text{max}}]$ and applying rotations directly via `Quaternion.Euler()`, preventing floating-point precision drift across long play sessions.
- **DEADeye Context**: Implemented in `DEADeyeMouseLook.cs` to give the desktop simulator the precise feel of a competitive first-person shooter.

#### Z-Fighting
- **Simple Definition**: An ugly visual glitch where two overlapping surfaces (like a poster on a wall) flicker back and forth because the computer can't tell which one is in front.
- **Technical Definition**: An artifact in 3D computer graphics occurring when two or more primitive fragments have identical or nearly identical depth buffer values ($z$), causing the rasterizer to alternate between drawing fragments based on floating-point rounding errors.
- **DEADeye Context**: Completely avoided on catwalks and bridge decks by enforcing a minimum physical separation of $10\text{ mm}$ between structural steel meshes and concrete parapets.

