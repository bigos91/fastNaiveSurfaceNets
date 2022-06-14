# fastNaiveSurfaceNets
Fast implementation of Naive SurfaceNets in Unity, using Burst and SIMD instructions.
Around ~(0.2 - 0.4) ms per 32^3 voxels (depends on SDF complexity) (on first gen ryzen 1700, singlethreaded)

https://youtu.be/_Bix6-4O6mM

#### Features:
- Normals can be generated from SDF values of 2x2x2 cube. Or regenerated from triangles at the end of the job.
- It may be hard to read (pointers, native collections, assembly intrinsics)
- Cornermask calculations are done using SIMD stuff, 32 cubes at time (32x2x2 voxels), reusing values calculated from previous loop steps.
- All SIMD things are well commented to explain what it is and why it is, with links to Intel intrinsics pages.
- Optimal mesh. All vertices are shared. There are no duplicates.
- 3 Different SDF generation mechanisms (sphere, noise, something like noise but with spheres - *sphereblobs*).
Default 3d noise values does not match real SDF values, so I just filled volume with spheres of different sizes at different locations.
- Use of advanced mesh api for faster uploading meshes. (SetVertexBufferData... etc.)
- Because its done entirely on cpu, output mesh can be easily used for collisions.

#### Limitations:
- Meshed area must have 32 voxels in at least one dimension. (this implementation support only chunks 32^3, but it is possible to make it working with 32xNxM)

#### Requirements:
- Unity (2020.3 works fine, dont know about previous versions)
- CPU with SSE4.1 support (around year 2007)

#### Usage:
- Clone, run, open scene [FastNaiveSurfaceNets/Scenes/SampleScene],
- Disable everything what makes burst safe to make it faster :)

#### Resources:
https://github.com/TomaszFoster/NaiveSurfaceNets - I learnt most from this, and used algorithm for connecting vertices properly.

https://github.com/Chaser324/unity-wireframe - for wireframe.

#### Todo:
 - 16^3 size version
 - maybe 64^3 size version but on AVX
 - Currently working on similiar stuff for Marching cubes
