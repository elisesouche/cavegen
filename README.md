# Procedural Cave Generation

Procedural game-oriented cave geometry. Goals are aesthetics and fun; this is
not intended as a geological simulation.

It runs in the Godot game engine.

Repository is on [GitHub](https://github.com/elisesouche/cavegen) :( 
For more details, check the [report](./report/main.pdf).

## Requirements

- Godot 4.6 (Mono build)
- .NET / Mono runtime supporting .NET 8
- Vulkan-capable GPU with compute shader support

## Usage

2. Open the project folder [./godot](./godot) in the Godot editor. Allow the editor to
   build the C# assemblies.
3. Open the main scene [./godot/main.tscn](./godot/main.tscn) (it should open by default).
4. Navigate the scene tree under the `Cave` node. You can edit parameters in the
   Godot editor, then click the "Generate Cave" button in the Inspector of the
   `Cave` node.
5. You can press the "Run Project" (it is the little "play" arrow) button at the
   top of the editor to explore the cave!

## Architecture

Pipeline stages:
1. Layout: stochastic L-system generates a tree-like graph; macros and
   branching rules control topology. The code for this is in [./godot/Layout](./godot/Layout).
2. Volume: layout samples are applied to a voxel scalar field via metaball-like
   brushes (smooth falloff, optional noise). The code for this is [./godot/Voxel](./godot/Voxel).
3. Surface extraction & render: isosurface (density = 0) extracted with Marching
   Cubes running as a compute shader. The code is in
   [./godot/Mesh](./godot/Mesh). The mesh is rendered with a PBR shader with
   triplanar mapping.


## Generation parameters

- L-system rule set and stochastic choices: see the `Layout` node.
- Turtle: step length, pitch/yaw increments, branching probability. Also in the `Layout` node.
- Voxel grid: resolution and world bounds (primary tradeoff: quality vs.
  time/memory). In the `Voxels` node.
- Brush: radius, falloff, noise amplitude. The default brush is `NoisyBrush`
  which gives the most organic results. You can replace it with `SmoothBrush` or
  `SphereBrush` by setting the field "Brush" in the Inspector of the `Cave`
  node.

## Performance and limitations

Brush application is the main computational bottleneck; generation time
increases with voxel count. With the default parameters, complete cave
generation take a few minutes on my laptop.

Author: Élise Souche, ENS de Lyon. 
License: GPL-3.0-or-later
