# SortLab 3D & VR Sorting Challenge

An educational Unity project that combines **3D sorting-algorithm visualization**, **interactive collision physics**, and a **controller-driven VR sorting challenge**. The project is organized around hands-on demonstrations: users can watch algorithms operate on a 3D array, explore a sphere–cube collision experiment, and arrange numbered cubes into ascending order on a virtual tabletop.

> **Unity project location:** `SortingAlgorithmVisualization_VR/`  
> **Unity version:** 2021.3.45f2 LTS  
> **Primary development target:** Windows Standalone (64-bit)

## Project modules

### 1. SortLab 3D — Sorting algorithm visualization

The desktop visualization represents array values as 3D objects and animates algorithm operations. The implementation includes these 12 sorting algorithms:

- Bubble Sort
- Cocktail Shaker Sort
- Comb Sort
- Gnome Sort
- Heap Sort
- Insertion Sort
- Bottom-Up Merge Sort
- Odd-Even Sort
- Quick Sort
- Radix Sort
- Selection Sort
- Shell Sort

The surrounding application provides:

- Array generation with configurable element count and value range.
- Playback controls for play, pause, stepping forward, and stepping backward.
- Adjustable visualization speed.
- A draggable C# code inspector with synchronized line highlighting.
- A 3D complete-binary-tree view for Heap Sort and digit buckets for Radix Sort.
- A free-flight camera with keyboard movement, mouse-look, and camera framing.

### 2. CollisionLab — Interactive physics experiment

CollisionLab demonstrates a sphere colliding with a cube using Unity's built-in Rigidbody and collider physics.

The experiment exposes controls for:

- Initial launch speed.
- Sphere and cube mass.
- Coefficient of restitution.
- Presets such as equal mass, heavy impactor, heavy target, elastic, and inelastic collisions.

The visualization and telemetry code includes:

- Linear-velocity vectors.
- Collision contact-point and contact-normal visualization.
- Before-and-after velocity information.
- Momentum and kinetic-energy calculations.
- Collision state and impact telemetry.

The lab is intended to help users relate the visible collision to concepts such as impulse, momentum conservation, restitution, and kinetic-energy loss.

### 3. VR Sorting Challenge — Tabletop cube-sorting game

The VR Sorting Challenge is a separate tabletop scene. The player uses simulated XR controllers to pick up five numbered cubes and place them into five target slots.

- Five cubes carry the values **1–5**.
- The initial staging order is **3, 1, 5, 2, 4**.
- Five slots are arranged from left to right.
- Cube placement is handled by the sorting-challenge scripts and slot snap anchors.
- The interface reports slot progress and whether the arrangement is correct.
- Completion requires the ascending order **1 → 2 → 3 → 4 → 5**.
- Reset, Play Again, and Back to Main Menu controls are provided.

The scene includes a custom grab-transformer component for tabletop interaction and a table-surface constraint. The challenge can be opened directly in the Unity Editor or built as a dedicated Windows player.

### 4. VR Collision Lab

The project also contains a VR version of the collision laboratory. It reuses the collision experiment and adds XR scene setup and interaction components. XR lifecycle initialization is guarded so the scene can fall back to simulated operation when an XR loader is not configured.

## Scenes

| Scene | Purpose |
|---|---|
| `MainScene` | Main SortLab 3D application and navigation menu |
| `CollisionLab` | Desktop sphere–cube collision experiment |
| `CollisionLab_VR` | VR/simulator version of CollisionLab |
| `VRSortingChallenge` | Five-cube tabletop sorting game |

## Technology stack

Versions below are taken from the Unity project's `Packages/manifest.json` and `ProjectSettings/ProjectVersion.txt`.

| Technology | Version / Use |
|---|---|
| Unity Editor | 2021.3.45f2 |
| C# | Unity scripting |
| XR Interaction Toolkit | 2.6.3 |
| Input System | 1.7.0 |
| XR Core Utilities | 2.3.0 |
| XR Plug-in Management | 4.5.0 |
| OpenXR Plugin | 1.13.0 |
| TextMeshPro | 3.0.6 |
| Unity UI (uGUI) | 1.0.0 |
| Physics | Unity built-in physics (PhysX) |

## Getting started

### Prerequisites

- Unity Hub.
- **Unity 2021.3.45f2** installed through Unity Hub.
- Windows is the configured standalone build target.
- A physical VR headset is **not required** for the laptop-based XR Device Simulator workflow.

### Open the project

1. Clone this repository.
2. In Unity Hub, choose **Add** or **Open**.
3. Select the `SortingAlgorithmVisualization_VR` folder (the folder containing `Assets`, `Packages`, and `ProjectSettings`).
4. Open it with Unity **2021.3.45f2**.
5. Allow Unity Package Manager to resolve the packages listed in `Packages/manifest.json`.
6. Open `Assets/Scenes/MainScene.unity` and press **Play** to explore the desktop application.

To open the tabletop game directly, open:

`Assets/Scenes/VRSortingChallenge.unity`

The VR-related scenes use the XR Interaction Toolkit sample assets included under:

`Assets/Samples/XR Interaction Toolkit/2.6.3/`

### XR Device Simulator controls

The project includes the XR Interaction Toolkit 2.6.3 simulator input-action assets. The bindings below are from the checked-in simulator action files.

| Action | Input |
|---|---|
| Manipulate the right controller | Hold **Space** (or press **Y** to toggle) |
| Manipulate the left controller | Hold **Left Shift** (or press **T** to toggle) |
| Manipulate the simulated HMD | Hold **Right Mouse Button** |
| Move the currently manipulated device | **W, A, S, D, Q, E** |
| Rotate-mode override | Hold **Left Ctrl** |
| Grab / grip | **G** |
| Trigger / UI press | **Left Mouse Button** |
| Cycle simulated devices | **Tab** |
| Stop manipulation | **Esc** |
| Reset simulator device pose | **V** |

For the sorting challenge, select a controller, aim it at a numbered cube, and use the grip input to select/grab it. Use the trigger input for UI interactions. The simulator overlay and the checked-in input-action assets are the source of truth if controls are changed later.

### Desktop application shortcuts

| Context | Action | Input |
|---|---|---|
| Global | Toggle sidebar/menu | `M` |
| SortLab 3D | Toggle code inspector | `C` |
| SortLab 3D | Reframe the camera | `F` |
| SortLab 3D | Move camera | `W A S D Q E` |
| SortLab 3D | Camera look | Right mouse button |
| SortLab 3D | Camera sprint | Hold Left Shift |
| SortLab 3D | Open CollisionLab | `F2` |
| CollisionLab | Launch sphere | `Space` |
| CollisionLab | Reset experiment | `R` |
| CollisionLab | Toggle telemetry | `T` or `C` |
| CollisionLab | Return to MainScene | `Esc` |

## Build for Windows

### Dedicated VR Sorting Challenge player

The Unity Editor menu item **SortLab VR → Build Dedicated VR Sorting Challenge Player** invokes the dedicated standalone builder.

It builds a Windows 64-bit player with this scene order:

1. `VRSortingChallenge.unity` — startup scene.
2. `MainScene.unity` — return-to-menu destination.
3. `CollisionLab.unity`.
4. `CollisionLab_VR.unity`.

Expected output:

```text
SortingAlgorithmVisualization_VR/
└── Builds/
    └── VRSortingChallenge-Windows/
        ├── VRSortingChallenge.exe
        ├── VRSortingChallenge_Data/
        ├── UnityPlayer.dll
        └── MonoBleedingEdge/
```

Keep the executable beside its generated data and runtime folders. Do not copy or distribute the `.exe` by itself.

### SortLab 3D player

The existing **SortLab3D → Build Windows Standalone (x86_64)** menu item builds the regular SortLab 3D application separately. It is not the dedicated VR Sorting Challenge player.

## Code organization

The main Unity project is under `SortingAlgorithmVisualization_VR/`.

| Path | Responsibility |
|---|---|
| `Assets/Scenes/` | Main, collision, and VR sorting scenes |
| `Assets/Scripts/Sorting/` | Sorting algorithm implementations |
| `Assets/Scripts/Performance/` | 3D algorithm animation, operation effects, and code-line support |
| `Assets/Scripts/Collision/` | Collision simulation, object behavior, and physics visualizers |
| `Assets/Scripts/UI/` | Playback, progress, sliders, panels, and scene navigation |
| `Assets/Scripts/VR/SortingChallenge/` | Cube, slot, manager, and table grab-transformer logic |
| `Assets/Scripts/VR/` | VR collision grabbing and XR scene bootstrap |
| `Assets/Editor/` | Scene builders, standalone builder, and verification utilities |
| `Packages/` | Unity package manifest and lock file |
| `ProjectSettings/` | Unity project and build settings |

Key VR sorting scripts:

- `SortingChallengeManager.cs` — attempt lifecycle, progress, validation, reset, and replay.
- `SortingChallengeCube.cs` — cube identity, grab/release lifecycle, staging, and slot placement.
- `SortingSlot.cs` — slot index, snap anchor, occupancy, and border state.
- `TableSurfaceGrabTransformer.cs` — controller-driven grab movement with a tabletop height constraint.
- `VRSortingChallengeSceneBuilder.cs` — Editor utility for creating the tabletop scene.
- `VRSortingChallengeStandaloneBuilder.cs` — dedicated Windows player build.
- `VRSortingChallengeVerifier.cs` — Editor-side scene and gameplay verification checks.

## Scope and verification notes

- This repository is primarily a **Computer Graphics and Virtual Reality** project with algorithm and physics visualizations.
- Although the application visualizes sorting operations, the inspected code does not currently implement a CUDA programming lab or a general CPU/GPU parallel-computing benchmark suite. Those would be separate future modules.
- The VR development workflow includes the XR Device Simulator. A successful simulator run does not by itself establish that every feature has been tested on a physical headset.
- Automated Editor verification is useful for scene structure and game-state logic; controller grabbing, visual readability, standalone navigation, and physical-headset behavior should also be checked interactively on the target setup.

## License and attribution

The Unity project includes an MIT license at `SortingAlgorithmVisualization_VR/LICENSE`. The license file retains the original attribution to **Dylan Cheng / newlooper** for the foundational sorting-visualization work. Review that license before redistributing or publishing modified assets and code.
