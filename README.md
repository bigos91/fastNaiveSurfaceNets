# fastNaiveSurfaceNets
- Fast implementation of Naive SurfaceNets in Unity, using Burst and SIMD instructions.
- Around ~(0.1 - 0.4) ms per 32^3 voxels (depends on SDF complexity) (on first gen ryzen 1700, singlethreaded)
- Marching cubes version : https://github.com/bigos91/fastMarchingCubes


https://youtu.be/_Bix6-4O6mM

#### Features:
- Normals can be generated from SDF values of 2x2x2 cube. Or regenerated from triangles at the end of the job.
- It may be hard to read (pointers, native collections, assembly intrinsics)
- Cornermask calculations are done using SIMD stuff, 32 cubes at time (32x2x2 voxels), reusing values calculated from previous loop steps.
- All SIMD things are well commented to explain what it is and why it is, with links to Intel intrinsics pages.
- Optimal mesh. All vertices are shared. There are no duplicates.
- Different SDF generation mechanisms (sphere, noise, something like noise but with spheres - *sphereblobs*, simple terrain).
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
- https://github.com/TomaszFoster/NaiveSurfaceNets - I learnt most from this, and used algorithm for connecting vertices properly.
- https://github.com/Chaser324/unity-wireframe - for wireframe.

#### Simd stuff explanation:
Naive Surface Nets works similiar to Marching Cubes - we iterate over volume collecting 8 voxel samples for 'cube' at a time.
For such cube, we need to calculate something called 'corner mask' - it is 8bit mask describing which corner is below or above some isosurface value.
I decided to store voxel density data as signed bytes, and use sign bit to build cornermask - so i extracting isosurface at 0.
But, we can load 32 voxels into 2 128bit SSE registers, and use [movemask_epi8](https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274,5153,5153,5153,5596,3343,3864&cats=Miscellaneous&techs=SSE2&ig_expand=4873 "movemask_epi8") intrinsic to extract sign bits from 16 8bit values at a time. Need 2 operation for 32 voxels.

Such operations are performed 4 times, for 4 groups of 32 voxel each, creating 4 32bit masks. Those masks are reversed, to make first bits last. Then, we can extract highest bits from those 4 masks in the same way [movemask_epi_ps](https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274,5153,5153,5153,5596,3343,3864&cats=Miscellaneous&techs=SSE&ig_expand=4878 "movemask_epi_ps"), shift them all 4 bits to the left [slli_epi32](https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274&othertechs=BMI1,BMI2&techs=SSE,SSE2,SSE3,SSSE3,SSE4_1,SSE4_2&cats=Shift&ig_expand=6537 "slli_epi32") and extract second 4 bits to create 8bit cornermask. Half of cornermask can be reused while iterating (Z dimension), and 2 of those 32bit masks can also be reused while iterating (Y dimension).

Additionally, it is possible to check if whole column of 32x2x2 voxels is above or inside surface - [test_mix_ones_zeros](https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#expand=2528,951,4482,391,832,1717,291,338,5486,5304,5274,5153,5153,5153,5596,3343,3864,5903&techs=SSE4_1&cats=Logical&ig_expand=7214 "test_mix_ones_zeros") can be used to check if all 4 32bit masks have all bits set to same value or not.


#### Todo:
 - 16^3 size version
 - maybe 64^3 size version but on AVX
 - Use of [cmplt_epi8](https://www.intel.com/content/www/us/en/docs/intrinsics-guide/index.html#techs=SSE2&cats=Compare&text=_epi8&ig_expand=1180,1180) for extracting isosurface at isovalues other than 0.
