README - LOD mesh generator for voxel chunks (Unity)

Files included:
 - LODMeshGenerator.cs        : Public API. Call GenerateLODMesh(blocks, lodLevel, chunkGlobalPosition).
 - MarchingCubes.cs           : Lightweight marching cubes implementation; uses MarchingCubesTables from your project.
 - ExampleUsage.cs            : Demo MonoBehaviour showing how to call the generator.
 - MarchingCubesTables.*      : NOT included. The implementation expects MarchingCubesTables.EdgeTable and TriangleTable[,] to exist in your project
                               as you mentioned. If you don't have them, add the standard marching cubes tables (many references online).

Usage notes and important practical details:
 - lodLevel semantics: lodLevel = 0 => step=1 (full sample), lodLevel=1 => step=2 (coarse), lodLevel=2 => step=4, etc.
 - The generator downsamples blocks into a density grid by counting solid blocks inside each sample cell then runs marching cubes.
 - Mesh vertices are in local chunk coordinates (so make sure the GameObject.transform.position equals the chunk origin in blocks).
 - The generator treats block id 0 as air. Change LODMeshGenerator.IsSolid if your ids differ.
 - Performance: this is CPU work. For many chunks generate LODs on background threads or via Unity Jobs/Burst and create Mesh on main thread.
 - Optimizations you should consider: weld vertices, reduce duplicate vertices, LOD clip by distance, combine chunk LODs into bigger meshes for farther distances.

Caveats:
 - This generator prioritizes silhouette quality over exact block shape; caveats around thin diagonal tunnels and tiny overhangs exist because of downsampling.
 - If your world uses liquids or translucent blocks you may want to treat some block ids as partially solid (adjust density sampling).
