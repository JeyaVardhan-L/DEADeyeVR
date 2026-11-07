# Contributing to DEADeye

Thank you for your interest in contributing to **DEADeye**! We welcome bug reports, architectural suggestions, performance optimizations, and code contributions from the game development and XR communities.

---

## 1. Code of Conduct & Principles

- **Focus on Experience**: We prioritize player immersion, tactical weapon discipline, and stable frame rates over feature bloat.
- **Strict Zero-Dollar Budget**: All submitted code and assets must use permissive open-source licenses (MIT, Apache 2.0, BSD, CC0, or CC-BY 4.0). **Do not submit paid Asset Store packages, commercial plugins, or proprietary SDKs.**
- **Mobile VR Performance Discipline**: All code targeted for runtime must run comfortably on mobile hardware (Snapdragon XR2 / Meta Quest 2) maintaining a stable 72 FPS without runtime allocations (`GC.Alloc` spikes).

---

## 2. Development Environment & Setup

- **Unity Version**: **`6000.3.27f2` (Unity 6.3 LTS)**. Pull requests using different major or minor Unity versions will not be merged to maintain project stability.
- **Render Pipeline**: Universal Render Pipeline (`URP 17.3.0`) with Forward+ clustering.
- **Target Platforms**:
  - Windows x64 Desktop Simulator.
  - Android ARM64 OpenXR (`com.unity.xr.openxr`).

---

## 3. Workflow & Submitting Pull Requests

1. **Fork the Repository**:
   Create your feature branch from `master`:
   ```bash
   git checkout -b feature/your-feature-name
   ```
2. **Coding Standards**:
   - Follow standard Microsoft C# naming conventions (PascalCase for methods and properties, camelCase for local variables and parameters, `_camelCase` or `camelCase` with `[SerializeField]` for private fields).
   - Document public APIs and systems using XML documentation comments (`/// <summary>`).
   - Preserve existing architectural patterns: adhere to singletons where established and decouple systems via event delegates.
   - Separate PC Desktop and Quest VR logic via `DEADeyePlayerController` and `DEADeyeInputRouter`.
3. **Automated Testing & Verification**:
   Before submitting your PR, ensure all automated tests pass with zero errors:
   ```powershell
   & "C:\Program Files\Unity\Hub\Editor\6000.3.27f2\Editor\Unity.exe" `
     -batchmode -projectPath "." -runTests -testPlatform EditMode `
     -testResults "TestResults_EditMode.xml" -logFile "test.log"
   ```
4. **Submitting PRs**:
   - Provide a clear, descriptive title and description explaining the changes.
   - Reference any related issues or discussions.
   - Verify that your PR contains no temporary files, debug dumps, or IDE caches.

---

## 4. Reporting Issues

When reporting bugs or crashes:
- Provide your operating system and graphics hardware (or Quest headset model and OS version).
- Include the exact reproduction steps.
- Attach the Unity Editor log (`Editor.log`) or Android logcat dump (`adb logcat -s Unity`).
