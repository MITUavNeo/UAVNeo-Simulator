# Changelog

All notable changes to UAVSim are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.3] - 2026-05-03

Module 6, autograder integration, and a major refactor that finishes the migration
from the upstream RACECAR Neo Simulator fork to a standalone UAV/drone simulator.
Pairs with the drone-first naming in [`uav-neo-library`](https://github.com/MITUavNeo/uav-neo-library).

The branch `gael/uav-unity-scenes` was the **initial v0.0.3 version** of this
work (Module 6 scene authoring, `BuildAltitudeGates`, line-color randomizer,
`GRGAutograderSetup`). PR #2 opened that branch against `main` on 2026-04-30 and
was closed as superseded on 2026-05-02 once Chris's `8f472b3` refactor adopted
the scene topology and scripts (with edits) directly into `main`. The branch was
retained on the remote as a reference, then deleted on 2026-06-02 — the work it
represents now lives on `main` via this v0.0.3 entry.

### Added
- Module 6 lab content.
- Autograder wiring via `GRGAutograderSetup` (directory creation before `CopyAsset`,
  Applied/Skipped/Failed status popup).
- `DroneWorld.unity` as the canonical baked scene (split out from `FlightDemo`).
- Sandbox scene restored.
- `BuildAltitudeGates` editor tool hardened against missing shaders.

### Changed
- **Wire protocol naming** (`PythonInterface.cs`, byte-compatible — no protocol break):
  - `Header` opcodes `racecar_go`, `racecar_set_start_update`, `racecar_get_delta_time`,
    `racecar_set_update_slow_time` renamed to `drone_*` (positions 8–11 preserved).
  - `Error.racecarsim_outdated` renamed to `Error.sim_outdated` (position 5 preserved).
  - Doc comments and `LogError` prose updated: `racecar_core` → `drone_core`,
    `RacecarSim` → `UAVSim`, `racecar_core_sim.py` → `drone_core_sim.py`.
- **C# class renames**: `RacecarModule` → `DroneModule`, `Racecar` → `Drone`,
  `LevelManager.GetCar()` → `GetDrone()`, `LevelManager.ResetCar()` → `ResetDrone()`.
- **Folder/file moves**: `Scripts/Racecar/` → `Scripts/Drone/`; loose `Drone*.cs`
  scripts (`DroneCourseBuilder`, `DroneCourseRuntime`, `DroneModelSetup`,
  `DronePrefabBuilder`) consolidated under `Scripts/Drone/`.
- **Prefab reorganization**: `Prefabs/Drone/`, `Prefabs/Legacy/` (staging),
  `Prefabs/Shared/`, `Prefabs/KeyPoints/`, `Prefabs/Obstacles/`, `Prefabs/UI/`.
- **Hud refactor (Stages 1–4)**: `TelemetryPanel` and `BottomIndicator` are now
  authored content baked into `Hud.prefab` instead of constructed at runtime
  (~600 lines of construction code deleted). Text/Image references are now named
  `[SerializeField]` fields; the `Texts`/`Images` enums are gone. `ScreenManager`
  has `ShowMessage` and `SetPause` made `virtual`, with four message-fade fields
  promoted to `protected` so `Hud` can override cleanly.
- "Default Drive" renamed to "Default Flight".
- Arming prompt clarified: "Press E" → "Press A (1 key)" to match the input binding.
- Unity 6 obsolete API cleanup: `Object.FindObjectsOfType<T>()` →
  `Object.FindObjectsByType<T>()` across all call sites.
- Nadir camera panel migrated from runtime construction to authored prefab content.

### Fixed
- `CameraModule.UpdateDepthImage` defensive null/size guards (NRE on early frames).
- Hud refactor regressions: camera label overlay, controller buttons/joysticks
  render order, `CS0111` duplicate `UpdateTime`, `CS0128` collision in
  `DronePrefabBuilder.cs:297` (outer GameObject and inner Drone-component variable
  both named `drone` — component variable renamed to `droneScript`).

### Removed
- `Scripts/Drone/Lidar.cs` (165 lines, dead code) and `Scripts/Drone/Drive.cs`
  (139 lines, dead car physics).
- LIDAR opcodes `lidar_get_num_samples` and `lidar_get_samples` reserved as
  `_reserved_25` / `_reserved_26`; sync and async stub handlers removed.
- `Hud.UpdatePhysics()` and `HideOldPhysicsTexts()` IMU bandaids; `Texts.TrueSpeed`,
  `Texts.LinearAcceleration`, `Texts.AngularVelocity` enum entries removed.
- `Images.LidarMap` enum entry and the runtime "find LidarMap and `SetActive(false)`"
  hide code.
- `RACECAR-MN` historical attribution comment in `Drone.cs`.
- Racecar-only scenes: `Scenes/HMC_Labs/`, `Scenes/GrandPrixFiles/`,
  `Scenes/Neo_Labs/`, `Scenes/Community/` (~140 `.unity` files).
- Racecar-only models: `Models/CS - Signs Free/`, `Models/GP2022 Models/`.
- Carryover tarball cruft: `Assets/Fonts/liberation-fonts-ttf-2.1.5/`.
- Stale entries from `EditorBuildSettings.asset` (411-line trim, kept 11 surviving
  scenes).
- ~1707 file deletions total in the racecar-asset purge.

## [0.0.2] - 2026-03-30

Formal integration of AirSim Labs educational content and updates to drone
behavior. Merged via [PR #1](https://github.com/MITUavNeo/UAVNeo-Simulator/pull/1)
("integrate airsim labs and update drone behavior") from the
`integration/grg-labs-behavior` branch.

### Added
- Module 1 — `HelloDrone` scene.
- Module 2 — `DroneControl` scene.
- Module 3 — `ObjectDetection` scene.
- Module 4 — `ArUcoMaze` scene.
- Module 5 — `SearchAndRescue` and `MazeNavigation` scenes.
- 3D model assets for object-detection labs, including a pineapple 3D scan
  (37.6 MB zip) and a simplified-model archive (23.5 MB 7z) with associated
  OBJ/MTL/texture/`.meta` files.

### Changed
- `Flight.cs` and `LevelCollection.cs` updated for the new lab behavior.
- `EditorBuildSettings.asset` updated to register the new module scenes.
- `.gitignore` expanded to exclude Unity build directories: `Builds/`, `Library/`,
  `Logs/`, `Temp/`, `UserSettings/`.

### Removed
- `uav_neo_body.obj.meta` (stale meta file, 109 lines).

## [0.0.1] - 2026-03-13

Initial beta release of UAVSim — a Unity-based UAV/drone simulator forked from
the [RACECAR Neo Simulator](https://github.com/MITRacecarNeo/RacecarNeo-Simulator).
Pairs with the [`uav-neo-library`](https://github.com/MITUavNeo/uav-neo-library)
Python control library over UDP (sync 5065, async 5064, protocol version 1).

### Added
- Initial Unity project import — Assets, ProjectSettings, Packages.
- Drone player prefab and scripts forked from the RACECAR Neo Simulator base.
- AR marker assets (numbered markers, 6x6 AR tag patterns with materials).
- Tape pattern assets for line-following style tasks.
- Texture assets including `Earth.jpg` and miscellaneous PNGs.
- UI artwork: MIT logo, controller instruction art with multi-state button
  variations, Python/R language logos, legacy Racecar logos.
- 3D models and materials for traffic signs and road infrastructure (stop signs,
  directional arrows, chevrons, parking signs) at multiple sizes.
- Demo scene configuration and prefab/material library.
- Windows build artifacts: `UAVSim.exe`, `UnityPlayer.dll`,
  `UnityCrashHandler64.exe` (later removed from `main` in commit `c930eb1`).

[0.0.3]: https://github.com/MITUavNeo/UAVNeo-Simulator/commit/8f472b3
[0.0.2]: https://github.com/MITUavNeo/UAVNeo-Simulator/pull/1
[0.0.1]: https://github.com/MITUavNeo/UAVNeo-Simulator/commit/6f70598
