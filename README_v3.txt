README v3 - Mu-based vertex-colored LOD for voxel chunks (URP)

What's updated:
 - Vertex colors are now computed using mu-based interpolation between the two grid-corner block colors
   that generated each marching-cubes edge-vertex. This is cheap (uses already computed interpolation factor)
   and gives much nicer, smoother color transitions along the silhouette compared to nearest-block sampling.

Files:
 - LODMeshGenerator.cs : Generates density grid, passes delegates to MarchingCubes to sample block IDs and color map.
 - MarchingCubes.cs    : Now computes per-vertex Color32 using getBlockID & colorForID delegates and returns them.
 - ExampleUsage.cs     : Demo that assigns a material.
 - URPVertexColor.shader: Minimal URP shader that renders vertex color unlit.

How it works (simple):
 - For each marching-cubes edge-vertex we already compute an interpolation factor mu (where along the edge the surface sits).
 - We read block IDs at the two cube corners that form this edge, map them to colors, then lerp the two colors by mu.
 - Result: each generated vertex gets a color that's consistent with the geometry interpolation — smooth and cheap.

Notes & performance:
 - This approach is almost free compared to density generation — it reuses mu that we already calculate.
 - The only extra cost is calling the getBlockID delegate for two corners per active edge. If your WorldGenerator is heavy, consider caching
   or prefetching block IDs for the density grid corners.
 - If you want even softer blends use neighborhood averaging (3x3x3) but expect CPU cost to grow.

Integration tips:
 - Create a material using "Custom/URPVertexColor" and assign it to your LOD MeshRenderer.
 - Ensure GameObject.transform.position == chunkGlobalPosition (in block coordinates).
 - If you produce many LOD meshes, combine them into larger meshes for drawcall savings.
