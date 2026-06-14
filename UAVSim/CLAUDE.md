# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

UAVSim is the Unity-side half of the MIT Beaver Works UAV educational sim. The other half is the Python library [`uav-neo-library`](https://github.com/MITUavNeo/uav-neo-library); the two talk over UDP. See [README.md](README.md) for a detailed component / wire-protocol writeup — that document is the source of truth for the architecture and is intentionally not duplicated here.

For broader workspace context (sibling repos, course schedule, sister tracks) see [../../../CLAUDE.md](../../../CLAUDE.md) and [../../CLAUDE.md](../../CLAUDE.md).

## Unity version — read this first

- [README.md:7](README.md#L7) says **6000.4.5f1**.
- [ProjectSettings/ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) says **6000.4.6f1**.

The actual project version wins. Don't try to "fix" the version mismatch by editing `ProjectVersion.txt` — open in the version Unity Hub actually has installed and let it stay as-is. Mixing 6000.4.5 ↔ 6000.4.6 is fine; downgrading to anything pre-Unity-6 or jumping to 6000.5 will corrupt the project.

## How you'll actually do work here

There is **no CLI build/test/lint**. All operations happen through the Unity Editor or through the project's MCP server.

### MCP server (primary tooling surface)

[.mcp.json](.mcp.json) configures the `ai-game-developer` (com.ivanmurzak.unity.mcp v0.72.1) server at `http://localhost:25408`. **The Unity Editor must be open for the server to respond** — it runs inside the editor process.

You'll do most inspection/edits through MCP tools rather than reading `.unity` YAML directly:
- `scene-list-opened`, `scene-open`, `scene-save`, `scene-get-data`
- `gameobject-find`, `gameobject-component-get`, `gameobject-component-modify`
- `assets-find`, `assets-get-data`, `assets-modify`
- `editor-application-get-state` / `set-state` (enter/exit Play mode)
- `tests-run` (Unity Test Framework — Edit mode is faster; requires all open scenes saved)
- `script-update-or-create`, `script-read` (triggers `AssetDatabase.Refresh` + compile)

`.unity` files are still readable YAML and grepping them is fast for cross-scene comparison (e.g. lightmap settings, render settings), but **never hand-edit them** — go through MCP so Unity rewrites the file cleanly and updates the meta state.

### Build

Builds happen through the Editor only: `File → Build Settings`. There is no headless build script. The Windows-build branch (`windows` on the simulator GitHub remote) is rebuilt by hand and contains only the artifact, not the source.

### Tests

Unity Test Framework. Run via `Window → General → Test Runner`, or via MCP `tests-run` (default to EditMode unless something specifically needs PlayMode). EditMode tests fail-fast if any open scene is dirty — save before running.

## Architecture conventions worth knowing

These are easy to break and not obvious from reading individual files.

### Wire-protocol opcodes are positional

[Assets/Scripts/NonMonoBehavior/PythonInterface.cs](Assets/Scripts/NonMonoBehavior/PythonInterface.cs) declares `Header` and `Error` enums whose **byte values come from declaration order**. The Python side (`drone_core_sim.py` in `uav-neo-library`) declares matching enums in identical positional order. So:

- **Never reorder, insert in the middle, or remove an enum entry** without bumping `version` (currently `1`) on both sides.
- LIDAR opcodes at positions 25 / 26 are deliberately preserved as `_reserved_25` / `_reserved_26` sentinels even though there's no LIDAR on the drone — this keeps the protocol byte-compatible with the RACECAR Neo fork.
- Append new opcodes at the **end** of the enum.

### `DroneModule` is the base class for drone subsystems

All hardware modules (`CameraModule`, `Flight`, `PhysicsModule`, `CrashSystem`, `PropellerSpin`) inherit [DroneModule.cs](Assets/Scripts/Drone/DroneModule.cs), which is itself a `MonoBehaviour`. They live as components on the `DronePlayer` prefab; `Awake()` does `GetComponent<Drone>()` to find the parent. If you add a new drone subsystem, follow this pattern — don't write a fresh MonoBehaviour from scratch.

### Class names that dodge Unity built-ins

`CameraModule` and `PhysicsModule` are named that way to avoid colliding with `UnityEngine.Camera` and `UnityEngine.Physics`. The `Drone` property `Camera` returns the module, not a Unity Camera. Don't rename these back to "obvious" names.

### `Controller.cs` has a compile-time OS define

[Assets/Scripts/Drone/Controller.cs:2](Assets/Scripts/Drone/Controller.cs#L2) starts with `#define WINDOWS`. Xbox-controller axis/button mapping differs per OS, so this must be hand-edited when developing on macOS/Linux. There is no runtime detection.

### Three `LevelManagerMode`s drive scene behavior

`Exploration` / `Autograder` / `Race` ([Assets/Scripts/LevelManagement/LevelManager.cs](Assets/Scripts/LevelManagement/LevelManager.cs)). A scene's `LevelManager` GameObject picks the mode. The autograder scenes under `Assets/Scenes/UAV_Neo_Labs/Autograder/` are wired with task components (`CoordinateThresholdTask`, `ProximityTask`, etc.) — see [Assets/Editor/GRGAutograderSetup.cs](Assets/Editor/GRGAutograderSetup.cs) which has a `Tools → GRG → Setup Autograder Scenes` menu that idempotently re-wires Modules 3b/4/5/6.

### Hud is authored-prefab content, not runtime-constructed

As of v0.0.3, `TelemetryPanel` and `BottomIndicator` live inside `Hud.prefab` and `Hud.cs` references them through `[SerializeField]` fields. The old `Texts`/`Images` enums and ~600 lines of runtime construction code are **gone**. Don't add new HUD elements by constructing them in code — author them in the prefab and add a serialized field reference.

### Lab scenes (Modules 1–6) use two distinct lighting profiles

This caught us once and will catch you again:

- **Modules 1–5** (top-level `UAV_Neo_Labs/Module*.unity`): `EnableBakedLightmaps: 0`, Progressive CPU backend, references a shared `LightingSettings` asset (GUID `34ad9922…`).
- **Module 6 + every Autograder scene**: `EnableBakedLightmaps: 1` with a real `LightingDataAsset`, Progressive GPU + Optix denoiser, inline lighting settings (no shared asset ref).

If a scene looks "wrong" (washed-out, flat shadows), check first: is `Sun.transform.eulerAngles` set to ~(50, 330, 0)? In some scenes it's accidentally (0, 0, 0), pointing the directional light horizontally. The `Sun` is a prefab instance — fixes on a single scene are stored as instance overrides; fix the source prefab at `Assets/Prefabs/Shared/Sun.prefab` to propagate.

Baked lightmaps capture the sun direction **at bake time**. After moving the sun, re-bake or the indirect light will still reflect the old angle.

## Packages

[Packages/manifest.json](Packages/manifest.json) pulls from OpenUPM (`package.openupm.com`) for two non-Unity dependencies: `com.ivanmurzak.unity.mcp` and `extensions.unity.playerprefsex`. If `Library/` is wiped and Unity can't resolve them, check that the scoped registry block survived.

## Heritage / why some things look weird

The repo is a fork of [RACECAR Neo Simulator](https://github.com/MITRacecarNeo/RacecarNeo-Simulator). Most car-specific scripts, scenes, and assets have been removed (~1700 files in v0.0.3), but expect to occasionally find references that don't make sense for a drone:

- `Assets/Prefabs/Legacy/` is a staging area for prefabs not yet deleted.
- "Default Drive" was renamed "Default Flight" but artifacts of the rename may surface.
- The protocol-level opcode renames (`racecar_*` → `drone_*`) are byte-compatible by design — see "positional opcodes" above.

When in doubt about a leftover symbol, check [CHANGELOG.md](CHANGELOG.md) v0.0.3 — most of the migration is documented there.
