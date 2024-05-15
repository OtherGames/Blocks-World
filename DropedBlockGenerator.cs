using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropedBlockGenerator 
{
    public const float TextureOffset = 1f / 16f;
    readonly Dictionary<BlockSide, List<Vector3>> blockVerticesSet;
    readonly Dictionary<BlockSide, List<int>> blockTrianglesSet;

    readonly List<Vector3> vertices = new();
    readonly List<int> triangulos = new();
    readonly List<Vector2> uvs = new();


    public DropedBlockGenerator()
    {
        blockVerticesSet = new Dictionary<BlockSide, List<Vector3>>();
        blockTrianglesSet = new Dictionary<BlockSide, List<int>>();

        DictionaryInits();
        InitTriangulos();

    }

    public Mesh GenerateBlockMesh(byte blockID)
    {
        Mesh mesh = new();
        mesh.Clear();

        triangulos.Clear();
        vertices.Clear();
        uvs.Clear();

        BlockUVS b = BlockUVS.GetBlock(blockID);

        CreateBlockSide(BlockSide.Front, 0.5f, -0.5f, -0.5f, b);
        CreateBlockSide(BlockSide.Back, 0.5f, -0.5f, -0.5f, b);
        CreateBlockSide(BlockSide.Right, 0.5f, -0.5f, -0.5f, b);
        CreateBlockSide(BlockSide.Left, 0.5f, -0.5f, -0.5f, b);
        CreateBlockSide(BlockSide.Top, 0.5f, -0.5f, -0.5f, b);
        CreateBlockSide(BlockSide.Bottom, 0.5f, -0.5f, -0.5f, b);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangulos.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.OptimizeReorderVertexBuffer();
        mesh.Optimize();

        return mesh;
    }

    public Mesh GenerateMeshBlock(byte blockID)
    {
        Mesh mesh = new();
        mesh.Clear();

        triangulos.Clear();
        vertices.Clear();
        uvs.Clear();

        BlockUVS b = BlockUVS.GetBlock(blockID);

        CreateBlockSide(BlockSide.Front, 0, 0, 0, b);
        CreateBlockSide(BlockSide.Back, 0, 0, 0, b);
        CreateBlockSide(BlockSide.Right, 0, 0, 0, b);
        CreateBlockSide(BlockSide.Left, 0, 0, 0, b);
        CreateBlockSide(BlockSide.Top, 0, 0, 0, b);
        CreateBlockSide(BlockSide.Bottom, 0, 0, 0, b);

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangulos.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.OptimizeReorderVertexBuffer();
        mesh.Optimize();

        return mesh;
    }


    void CreateBlockSide(BlockSide side, float x, float y, float z, BlockUVS b)
    {
        List<Vector3> vrtx = blockVerticesSet[side];
        List<int> trngls = blockTrianglesSet[side];
        int offset = 1;

        triangulos.Add(trngls[0] - offset + vertices.Count);
        triangulos.Add(trngls[1] - offset + vertices.Count);
        triangulos.Add(trngls[2] - offset + vertices.Count);

        triangulos.Add(trngls[3] - offset + vertices.Count);
        triangulos.Add(trngls[4] - offset + vertices.Count);
        triangulos.Add(trngls[5] - offset + vertices.Count);

        vertices.Add(new Vector3(x + vrtx[0].x, y + vrtx[0].y, z + vrtx[0].z)); // 1
        vertices.Add(new Vector3(x + vrtx[1].x, y + vrtx[1].y, z + vrtx[1].z)); // 2
        vertices.Add(new Vector3(x + vrtx[2].x, y + vrtx[2].y, z + vrtx[2].z)); // 3
        vertices.Add(new Vector3(x + vrtx[3].x, y + vrtx[3].y, z + vrtx[3].z)); // 4

        AddUVS(side, b);
    }

    void AddUVS(BlockSide side, BlockUVS b)
    {
        switch (side)
        {
            case BlockSide.Front:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));
                break;
            case BlockSide.Back:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));
                break;
            case BlockSide.Right:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));

                break;
            case BlockSide.Left:
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, TextureOffset * b.TextureYSide));
                uvs.Add(new Vector2((TextureOffset * b.TextureXSide) + TextureOffset, (TextureOffset * b.TextureYSide) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXSide, (TextureOffset * b.TextureYSide) + TextureOffset));

                break;
            case BlockSide.Top:
                uvs.Add(new Vector2(TextureOffset * b.TextureX, TextureOffset * b.TextureY));
                uvs.Add(new Vector2((TextureOffset * b.TextureX) + TextureOffset, TextureOffset * b.TextureY));
                uvs.Add(new Vector2((TextureOffset * b.TextureX) + TextureOffset, (TextureOffset * b.TextureY) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureX, (TextureOffset * b.TextureY) + TextureOffset));

                break;
            case BlockSide.Bottom:
                uvs.Add(new Vector2(TextureOffset * b.TextureXBottom, TextureOffset * b.TextureYBottom));
                uvs.Add(new Vector2((TextureOffset * b.TextureXBottom) + TextureOffset, TextureOffset * b.TextureYBottom));
                uvs.Add(new Vector2((TextureOffset * b.TextureXBottom) + TextureOffset, (TextureOffset * b.TextureYBottom) + TextureOffset));
                uvs.Add(new Vector2(TextureOffset * b.TextureXBottom, (TextureOffset * b.TextureYBottom) + TextureOffset));

                break;

        }

    }

    void DictionaryInits()
    {
        List<Vector3> verticesFront = new List<Vector3>
            {
                new Vector3( 0, 0, 1 ),
                new Vector3(-1, 0, 1 ),
                new Vector3(-1, 1, 1 ),
                new Vector3( 0, 1, 1 ),
            };
        List<Vector3> verticesBack = new List<Vector3>
            {
                new Vector3( 0, 0, 0 ),
                new Vector3(-1, 0, 0 ),
                new Vector3(-1, 1, 0 ),
                new Vector3( 0, 1, 0 ),
            };
        List<Vector3> verticesRight = new List<Vector3>
            {
                new Vector3( 0, 0, 0 ),
                new Vector3( 0, 0, 1 ),
                new Vector3( 0, 1, 1 ),
                new Vector3( 0, 1, 0 ),
            };
        List<Vector3> verticesLeft = new List<Vector3>
            {
                new Vector3(-1, 0, 0 ),
                new Vector3(-1, 0, 1 ),
                new Vector3(-1, 1, 1 ),
                new Vector3(-1, 1, 0 ),
            };
        List<Vector3> verticesTop = new List<Vector3>
            {
                new Vector3( 0, 1, 0 ),
                new Vector3(-1, 1, 0 ),
                new Vector3(-1, 1, 1 ),
                new Vector3( 0, 1, 1 ),
            };
        List<Vector3> verticesBottom = new List<Vector3>
            {
                new Vector3( 0, 0, 0 ),
                new Vector3(-1, 0, 0 ),
                new Vector3(-1, 0, 1 ),
                new Vector3( 0, 0, 1 ),
            };

        blockVerticesSet.Add(BlockSide.Front, null);
        blockVerticesSet.Add(BlockSide.Back, null);
        blockVerticesSet.Add(BlockSide.Right, null);
        blockVerticesSet.Add(BlockSide.Left, null);
        blockVerticesSet.Add(BlockSide.Top, null);
        blockVerticesSet.Add(BlockSide.Bottom, null);

        blockVerticesSet[BlockSide.Front] = verticesFront;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Back] = verticesBack;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Right] = verticesRight;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Left] = verticesLeft;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Top] = verticesTop;//.ToNativeArray(Allocator.Persistent);
        blockVerticesSet[BlockSide.Bottom] = verticesBottom;
    }

    void InitTriangulos()
    {
        List<int> trianglesFront = new List<int> { 3, 2, 1, 4, 3, 1 };
        List<int> trianglesBack = new List<int> { 1, 2, 3, 1, 3, 4 };
        List<int> trianglesRight = new List<int> { 1, 3, 2, 4, 3, 1 };
        List<int> trianglesLeft = new List<int> { 2, 3, 1, 1, 3, 4 };
        List<int> trianglesTop = new List<int> { 1, 2, 3, 1, 3, 4 };
        List<int> trianglesBottom = new List<int> { 3, 2, 1, 4, 3, 1 };

        blockTrianglesSet.Add(BlockSide.Front, trianglesFront);
        blockTrianglesSet.Add(BlockSide.Back, trianglesBack);
        blockTrianglesSet.Add(BlockSide.Right, trianglesRight);
        blockTrianglesSet.Add(BlockSide.Left, trianglesLeft);
        blockTrianglesSet.Add(BlockSide.Top, trianglesTop);
        blockTrianglesSet.Add(BlockSide.Bottom, trianglesBottom);
    }
}
