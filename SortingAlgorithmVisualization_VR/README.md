# SortLab 3D & CollisionLab

An interactive, educational 3D laboratory built in Unity for Computer Graphics and Virtual Reality (CGVR), featuring real-time 3D sorting algorithm visualizations and rigorous interactive physics collision simulations.

---

## 1. Features

### SortLab 3D — Algorithm Visualization
- **12 Interactive Sorting Algorithms**:
  - Bubble Sort
  - Cocktail Shaker Sort
  - Comb Sort
  - Gnome Sort
  - Heap Sort (with synchronized 3D Complete Binary Tree)
  - Insertion Sort
  - Merge Sort (Bottom-Up)
  - Odd-Even Sort
  - Quick Sort
  - Radix Sort (LSD with 3D Digit Buckets)
  - Selection Sort
  - Shell Sort
- **Draggable Code Inspector**:
  - Floating, draggable inspector window clamped to screen boundaries.
  - Line-by-line synchronized algorithm pseudocode execution highlighting.
  - High-contrast typography and smooth scrolling viewports.
  - Shortcut toggle (`[C]`).
- **Interactive Playback Controls**:
  - Continuous Play / Pause with dynamic step interpolation.
  - Single-step forward and step backward navigation.
  - Array generation with configurable element count and value ranges.
  - Dynamic simulation speed slider.
- **Adaptive 3D Camera System**:
  - Free flight camera navigation (`W`, `A`, `S`, `D`, `E`, `Q` + Right Mouse Look).
  - Sprint modifier (`Left Shift`).
  - Obstacle proximity collision avoidance.
  - One-key adaptive framing (`[F]`) that levels and centers the camera on the active array.

### CollisionLab — Interactive Physics Visualization
- **Dynamic 3D Velocity Vectors**:
  - Real-time `LineRenderer` vector arrows showing instantaneous linear velocity.
  - Dynamic scaling and directional alignment for both impactor and target bodies.
- **Collision Manifold Visualization**:
  - Contact point marker (bright aquamarine sphere) rendered at the physical impact location.
  - Contact normal vector (coral/magenta arrow) illustrating the impact plane and impulse direction.
- **Interactive Physics Controls**:
  - Mass-independent initial launch speed slider (1.0 to 18.0 m/s).
  - Impactor sphere mass slider (0.1 to 10.0 kg).
  - Target cube mass slider (0.1 to 10.0 kg).
  - Restitution slider ($e = 0.0$ to $1.0$) with safe runtime `PhysicMaterial` instancing.
  - Quick experiment presets:
    - *Equal Mass (1:1)*
    - *Heavy Impactor (3:1)*
    - *Heavy Target (1:3)*
    - *Elastic ($e = 1.0$)*
    - *Inelastic ($e = 0.0$, No Bounce)*
- **Real-Time Viva & Telemetry Dashboard**:
  - Initial vs. final velocities ($\vec{v}_{1i}, \vec{v}_{2i} \to \vec{v}_{1f}, \vec{v}_{2f}$).
  - Vector momentum conservation telemetry ($\vec{p}_i$ vs. $\vec{p}_f$ with $\Delta p$ percentage error).
  - Kinetic energy conservation and dissipation ($\Delta KE = KE_f - KE_i$).
  - Measured coefficient of restitution along the contact normal ($e_{\text{measured}}$).
  - Dedicated CGVR physics reference panel covering PhysX pipeline mechanics (Persistent Contact Manifolds, projected Gauss-Seidel solver) and boundary impulses.
- **Seamless Scene Navigation**:
  - One-click navigation between SortLab 3D and CollisionLab via sidebar button (`Collision Lab ➔`) and shortcut (`[F2]`).
  - Instant return via dock button (`← Back to SortLab`) and `Escape` key.

---

## 2. Controls & Shortcuts

| Context | Action | Key / Input |
| :--- | :--- | :--- |
| **Global** | Toggle Left Sidebar | `[M]` |
| **SortLab 3D** | Toggle Draggable Code Inspector | `[C]` |
| **SortLab 3D** | Adaptive View Reset | `[F]` |
| **SortLab 3D** | Navigate to Collision Lab | `[F2]` or Sidebar Button |
| **SortLab 3D** | Camera Translation | `[W]`, `[A]`, `[S]`, `[D]`, `[E]`, `[Q]` |
| **SortLab 3D** | Camera Look Rotation | `Right Mouse Drag` |
| **SortLab 3D** | Camera Sprint | Hold `[Left Shift]` |
| **CollisionLab** | Launch Sphere | `[Space]` |
| **CollisionLab** | Reset Experiment | `[R]` |
| **CollisionLab** | Toggle Telemetry Dashboard | `[T]` or `[C]` |
| **CollisionLab** | Return to SortLab 3D | `[Esc]` or Back Button |

---

## 3. Technical Requirements

- **Unity Editor**: `2021.1.27f1`
- **Platform**: Windows Standalone (x86_64)
- **Scripting Backend**: Mono / .NET Standard 2.0
- **Physics Engine**: Unity Built-in PhysX

---

## 4. Building the Application

To generate a standalone Windows build:
1. Open the project in **Unity 2021.1.27f1**.
2. In the top Unity menu bar, select **`SortLab3D`** ➔ **`Build Windows Standalone (x86_64)`**.
3. The automated build pipeline will generate the executable in `Builds/SortLab3D-Windows/SortLab3D.exe` and open the output folder in Windows Explorer.

---

## 5. Attribution & License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
Foundational sorting animation concepts originate from open-source visualization work by Dylan Cheng (https://github.com/newlooper), extended with CGVR interactive physics, telemetry instrumentation, and UI integration.
