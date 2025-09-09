using UnityEngine;

/*
 ExampleUsage.cs - tiny demo MonoBehaviour showing how to call GenerateLODMesh.
 Attach this to a GameObject whose transform.position equals the chunk's global origin in world-block coordinates.
 This example does not spawn the chunk buffer; it's only illustrative.
*/

public class ExampleUsage : MonoBehaviour
{
    // Imagine this is the block buffer for this chunk. In practice you have this already.
    public byte[,,] chunkBlocks;

    // LOD level to generate
    public int lodLevel = 2;

    void Start()
    {
        // If you have a chunk blocks buffer, pass it in. Here we'll fabricate an example if it's null.
        if (chunkBlocks == null)
        {
            chunkBlocks = new byte[16,16,16];
            // simple test: a sphere-ish solid center
            for (int x=0;x<16;x++) for (int y=0;y<16;y++) for (int z=0;z<16;z++)
            {
                Vector3 c = new Vector3(x-8,y-6,z-8);
                if (c.magnitude < 6f) chunkBlocks[x,y,z] = 1;
            }
        }

        Vector3Int chunkOrigin = Vector3Int.RoundToInt(transform.position); // user said transform.position == global chunk pos
        Mesh m = LODMeshGenerator.GenerateLODMesh(chunkBlocks, lodLevel, chunkOrigin);

        var mf = gameObject.GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        var mr = gameObject.GetComponent<MeshRenderer>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();

        mf.mesh = m;
    }
}
