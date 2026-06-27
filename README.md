# UAVNeo-Simulator

_The MIT Beaver Works UAV simulation environment, based on the [RACECAR Neo Simulator](https://github.com/MITRacecarNeo/RacecarNeo-Simulator)._

UAVSim is a Unity-based simulator for an educational quadrotor drone. It pairs with the [`uav-neo-library`](https://github.com/MITUavNeo/uav-neo-library) Python control library, which students import to control the simulated (or physical) drone via UDP.

> **Note**: As of v0.0.3, UAVSim is built using Unity `6000.4.5f1` (Unity 6). Older or newer versions of Unity are not supported. Migration to a newer version of Unity or opening the project in an older version may be risky and cause unexpected results.

## Table of Contents
- [Changelog](#changelog)
- [Getting Started](#getting-started)
- [Repository Contents](#repository-contents)
    - [Drone Modules](#drone-modules)
- [Python Interface](#python-interface)
    - [Synchronous Communication Protocol](#synchronous-communication-protocol-used-for-python-scripts)
    - [Asynchronous Communication Protocol](#asynchronous-communication-protocol-used-for-jupyter-notebook)
    - [Send Fragmented](#send-fragmented)
- [Modeling Error](#modeling-error)
- [Heritage](#heritage)

## Changelog

See [CHANGELOG.md](./CHANGELOG.md) for a full version history. Highlights:

- **v1.0.0** - Redesign efforts to labs, autograder, UI, and curriculum. Arena prefab added
  to reinforce theming, as well as standardizing lighting to prevent unwanted scene bakes.
  Scenes are now procedurally generated on start, including the ones within the autograder.
  Cleaned up remaining racecar prefabs and assets that are no longer used.
  
- **v0.0.3** — Module 6, autograder integration, and a major refactor that finishes
  the migration from RACECAR Neo (class renames, folder reorg, LIDAR/Drive/IMU
  bandaid removal, Hud refactor, Unity 6 API cleanup, ~1700-file racecar asset purge).
  
- **v0.0.2** — AirSim Labs integration: Modules 1–5 scenes (HelloDrone, DroneControl,
  ObjectDetection, ArUcoMaze, SearchAndRescue, MazeNavigation) and updated drone
  flight behavior. Merged via [PR #1](https://github.com/MITUavNeo/UAVNeo-Simulator/pull/1).
  
- **v0.0.1** — Initial beta release; Unity project import forked from RACECAR Neo
  Simulator with AR markers, tape patterns, and base drone player prefab.

## Getting Started

UAVSim is built with Unity and C#. Before you begin, you will need to download Unity [here](https://unity3d.com/get-unity/download). Select the option that says "Download Unity Hub".

Open Unity Hub and install Unity `6000.4.5f1` from the "Installs" page. In the "Projects" page, click the "Add" button and select the root of this repository. This will import the UAVSim project into Unity.

If you installed [Microsoft Visual Studio](https://visualstudio.microsoft.com/) with Unity, you can open the generated solution file to edit the C# scripts directly.

Inside the Unity Editor, you can build UAVSim from `File -> Build Settings`.

To control the simulated drone, install [`uav-neo-library`](https://github.com/MITUavNeo/uav-neo-library) on the Python side. A minimal student program looks like:

```python
from uav_neo_library import Drone

drone = Drone()

def start():
    drone.drive.set_speed_angle(0, 0)

def update():
    # ... read sensors, command the drone
    pass

drone.go(start, update)
```

## Repository Contents

You will find the following directories inside of `Assets/`:

- **2D**: Images, including AR-tag patterns, tape patterns, HUD content, and UI textures.
- **Editor**: Editor-only scripts and tooling (`BuildAltitudeGates`, `BuildF1Track`, `GRGAutograderSetup`).
- **Fonts**: Non-standard fonts used in the HUD and menus.
- **Materials**: Render and physics materials.
- **Models**: 3D models, including the drone body, gates, and lab props.
- **Prefabs**: Pre-made GameObjects organized into:
  - `Drone/` — the `DronePlayer` prefab.
  - `Shared/` — `LevelManager`, `LevelManagerAutograder`, `ResetBox`, `Sun`, `Terrain`.
  - `Legacy/` — staging folder for legacy racecar prefabs (held back pending deletion).
  - `KeyPoints/`, `Obstacles/`, `UI/` — supporting prefabs.
- **Resources**: Runtime-loadable assets.
- **Scenes**: The "levels" of the simulation. `Main.unity` provides the main menu;
  `FlightDemo.unity` and `Demo.unity` are sandbox-style scenes; the
  `UAV_Neo_Labs/` subdirectory contains the lab modules and their autograder
  variants.
- **Scripts**: The C# scripts that control the simulator.
- **Terrain**: Terrain assets.
- **Textures**: Render textures (color camera, depth camera, downward-facing camera).

Inside of the `Assets/Scripts` directory, you will find the following organization:

- **(root level)**: Cross-cutting scripts (`CenterOfMass`, `PerformanceOptimizer`, `ReloadBuffer`).
- **LevelManagement**: Scripts that control objects in a level (finish lines, key points, autograder triggers).
- **Drone**: Modules that control the drone and all of its hardware.
- **NonMonoBehavior**: Plain C# helpers including `PythonInterface`, the UDP bridge to Python.
- **UI**: Scripts controlling the 2D user interface, including the HUD and menus.

### Drone Modules

The [`Drone`](./Assets/Scripts/Drone/Drone.cs) class controls the drone and roughly mirrors the structure of the [`uav-neo-library`](https://github.com/MITUavNeo/uav-neo-library) library used by the real drone. The following sub-modules each handle a particular aspect of the drone's hardware:

- **`CameraModule`**: Models the color and depth imaging capabilities of the drone's forward-facing camera, plus the downward-facing nadir camera used for line-following and AR-tag tasks. The `DronePlayer` prefab includes a color camera that renders to a dedicated render texture; `CameraModule` pulls this texture from the GPU when requested. The depth image is created by performing a ray cast at "each pixel", at a lower resolution to preserve performance. Both operations are expensive, so color and depth images are cached per frame.
- **`Controller`**: Handles input from the keyboard and Xbox controller. Xbox controllers are mapped differently per operating system, so this module includes a compilation constant which must be set based on the operating system. Arming is bound to `Controller.Button.A` (the `1` key on keyboard).
- **`Flight`**: Handles the drone's motion model — thrust, attitude, and translational/yaw rate commands. Replaces the racecar's `Drive` module.
- **`PhysicsModule`**: Models the drone's IMU. Angular velocity is taken directly from the rigidbody. Linear acceleration is calculated as change in linear velocity, taken from the rigidbody.
- **`CrashSystem`**: Detects collisions and arms/disarms the drone in response, with optional reset behavior tied to `LevelManager`.
- **`PropellerSpin`**: Visual-only propeller animation tied to throttle state.

_The `CameraModule` and `PhysicsModule` are named as such to avoid conflicting with the `Camera` and `Physics` classes provided by Unity._

> **Note**: The 2D LIDAR module from RACECAR Neo has been removed. Its opcodes
> (`lidar_get_num_samples`, `lidar_get_samples`) are reserved on the wire so
> existing protocol versions remain byte-compatible, but they fall through to
> the default "not supported" branch. Drone-side perception uses the forward
> color/depth cameras and the downward nadir camera instead.

## Python Interface

UAVSim communicates with a user's Python program using a custom protocol sent over a UDP connection. This is all handled in the [`PythonInterface`](./Assets/Scripts/NonMonoBehavior/PythonInterface.cs) class. On the Python end, this communication is handled by `drone_core_sim.py` in the `uav-neo-library` library.

`PythonInterface` has two `UdpClient`s operating on fixed ports:

- **Sync port**: `5065`
- **Async port**: `5064`
- **Protocol version**: `1`

The async client operates on a separate thread and handles asynchronous data requests from Jupyter and connection requests from user programs. Once a connection has been established, UAVSim communicates with the user's program through the synchronous client, which operates on the main thread.

In `PythonInterface`, the `Header` enum declares the reserved messages used in the communication protocol. Both Unity and Python declare `Header` and `Error` enums in matching positional order, so byte values align across both sides regardless of the C#/Python identifier names.

### Synchronous Communication Protocol (used for Python scripts)

1. The Python program periodically sends `connect` messages to the UAVSim async client.
2. The async client receives a `connect` message, records the port of the Python program, and responds with a `connect` message. From now on, communication will occur through the synchronous client.
3. When the user enters "User Program" mode in UAVSim, the sync client sends a `unity_start` message to Python and blocks to await a response from Python.
4. Python runs the user's `start` function. If the `start` function calls any of the `drone_core` APIs, these API calls are passed back to the sync client with the corresponding header. For example, if the user calls `drone.camera.get_color_image()`, Python will send the `camera_get_color_image` message to the sync client. UAVSim then responds with the requested data.
5. Once the Python program finishes executing the user's `start` function, it sends back a `python_finished` message. UAVSim stops blocking.
6. Once per frame, the sync client sends a `unity_update` message to Python and blocks to await a response from Python.
7. Python runs the user's `update` function. Once again, it passes API calls back to the sync client.
8. When Python finishes executing the user's `update` function, it sends back a `python_finished` message. UAVSim stops blocking.
9. This process repeats until the user exits or restarts the level in UAVSim. In either of these cases, the sync client sends a `unity_exit` message to Python.
10. Upon receiving the `unity_exit` message, the Python program closes.

Note that after a connection is established, this protocol is **completely synchronous**: both systems are synchronized to the Unity update clock. UAVSim will always block the main thread until receiving a message from Python (or until a timeout occurs — 5 seconds by default).

### Asynchronous Communication Protocol (used for Jupyter Notebook)

1. Jupyter Notebook sends the async client a request for data from a particular sensor (such as a `camera_get_color_image` or `camera_get_depth_image` message).
2. The async client receives the request and tells the corresponding sensor on the main thread that data has been requested.
3. In the subsequent Update frame, the main thread updates the data from that sensor.
4. After a short waiting period, the async client reads the updated data and returns it to Jupyter Notebook.

### Send Fragmented

Color images are too large to send in a single UDP packet, so they are split across several UDP packets (max packet size: 65507 bytes). After each packet, UAVSim blocks until Python responds with the `python_send_next` message, which indicates that it is ready to receive the next fragment.

## Modeling Error

When the "Realism" option is enabled via the settings, a realistic amount of random gaussian error is added to the following sensors:

- depth camera
- IMU

The error rate is based on the data sheets for these sensors.

## Heritage

UAVSim is forked from the [RACECAR Neo Simulator](https://github.com/MITRacecarNeo/RacecarNeo-Simulator), which itself descends from the original [MIT-LL RACECAR](https://github.com/MITLLRacecar) Simulation. The wire protocol is byte-compatible with RACECAR Neo's by design — opcode positions are preserved across the rename so messages send and parse identically on either side. Internal naming, scene content, and prefabs have all been migrated to drone-first conventions; see [CHANGELOG.md](./CHANGELOG.md) for the full migration history.
