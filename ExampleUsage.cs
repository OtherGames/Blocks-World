using UnityEngine;

/*
 ExampleUsage.cs - demo showing how to call GenerateLODMesh with vertex color blending.
 Attach to a GameObject whose transform.position equals the chunk's global origin in block coordinates.
*/

public class ExampleUsage : MonoBehaviour
{
    public byte[,,] chunkBlocks;
    public int lodLevel = 2;
    public Material lodMaterial; // assign material that uses the URP vertex color shader

    void Start()
    {
        if (chunkBlocks == null)
        {
            chunkBlocks = new byte[16,16,16];
            for (int x=0;x<16;x++) for (int y=0;y<16;y++) for (int z=0;z<16;z++)
            {
                Vector3 c = new Vector3(x-8,y-6,z-8);
                if (c.magnitude < 6f) chunkBlocks[x,y,z] = 1;
            }
        }

        Vector3Int chunkOrigin = Vector3Int.RoundToInt(transform.position);
        Mesh m = LODMeshGenerator.GenerateLODMesh(chunkBlocks, lodLevel, chunkOrigin);

        var mf = gameObject.GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        var mr = gameObject.GetComponent<MeshRenderer>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();

        mf.mesh = m;
        if (lodMaterial != null) mr.material = lodMaterial;
    }
}
