// DumpChunkToFile.cs
// Utility to write a chunk byte[,,] to a text file (comma separated) for debug upload.
using System.IO;
using UnityEngine;

public static class DumpChunkToFile
{
    public static void SaveChunk(byte[,,] blocks, string path)
    {
        int sx = blocks.GetLength(0), sy = blocks.GetLength(1), sz = blocks.GetLength(2);
        using (var w = new StreamWriter(path, false))
        {
            bool first = true;
            for (int z=0; z<sz; z++)
                for (int y=0; y<sy; y++)
                    for (int x=0; x<sx; x++)
                    {
                        if (!first) w.Write(",");
                        w.Write(blocks[x,y,z]);
                        first = false;
                    }
        }
        Debug.Log($"Chunk saved to {path}");
    }
}
