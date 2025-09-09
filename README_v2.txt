README v2 - Vertex-colored LOD for voxel chunks (URP)

What's new:
 - The LOD generator now assigns vertex colors by sampling the block ID nearest to each generated vertex.
 - Included a minimal URP-compatible shader "Custom/URPVertexColor" that reads vertex colors and outputs them unlit.
 - Color mapping: grass ID=1 -> green, stone ID=2 -> gray, else -> brown. Adjust in LODMeshGenerator.ColorForBlockID.

Usage:
 - Use the mesh produced by GenerateLODMesh as before. Create/assign a Material that uses the "Custom/URPVertexColor" shader.
 - The GameObject's transform.position should equal the chunk global origin in block coordinates as before.

Performance notes:
 - Color sampling is done per-vertex, so meshes with many vertices will pay additional cost. For far LODs this is negligible.
 - Use Color32 (already used) for lower memory and faster mesh upload.
 - Consider merging LOD meshes into larger combined meshes for distant chunks to reduce drawcalls.
 - If you need smooth color blending between block types use density-weighted blends instead of nearest-block sampling. This will look nicer but costs a bit more time.

Shader notes:
 - The provided shader is minimal and unlit. If you want lighting, either author a simple Lit variant in Shader Graph that multiplies Albedo by vertex color, or extend this shader to sample a lighting function.

