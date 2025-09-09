// DebugSaveMeshObj.cs
// Attach to a GameObject in the scene; assign a chunk (or provide it programmatically) and run SaveDebugMesh to export an OBJ.
using System.IO;
using UnityEngine;

public class DebugSaveMeshObj : MonoBehaviour
{
    public int chunkX, chunkY, chunkZ;
    public int size = 16;
    public int lod = 1;
    public float voxelSize = 1f;
    public string outName = "debug_chunk.obj";

    // Provide a way to call from inspector
    [ContextMenu("SaveDebugMesh")]
    public void SaveDebugMesh()
    {
        // Example: obtain blocks from your world. Replace this with your own accessor.
        byte[,,] blocks = new byte[size,size,size];
        for (int z=0; z<size; z++) for (int y=0; y<size; y++) for (int x=0; x<size; x++)
            blocks[x,y,z] = WorldGenerator.Inst.procedural.GetBlockID(chunkX*size + x, chunkY*size + y, chunkZ*size + z);

        Mesh m = VoxelSurfaceNetsSeamStitch_Fix.GenerateLodMesh(blocks, lod, voxelSize, chunkX*size, chunkY*size, chunkZ*size, WorldGenerator.Inst.procedural.GetBlockID, 24);
        if (m == null) { Debug.LogError("Mesh generation returned null"); return; }
        string path = Path.Combine(Application.dataPath, outName);
        SaveMeshToObj(m, path);
        Debug.Log($"Saved OBJ to {path}");
    }

    void SaveMeshToObj(Mesh m, string path)
    {
        using (var sw = new StreamWriter(path))
        {
            var verts = m.vertices;
            var tris = m.triangles;
            for (int i=0;i<verts.Length;i++) sw.WriteLine($"v {verts[i].x} {verts[i].y} {verts[i].z}");
            for (int i=0;i<tris.Length;i+=3) sw.WriteLine($"f {tris[i]+1} {tris[i+1]+1} {tris[i+2]+1}");
        }
    }
}
