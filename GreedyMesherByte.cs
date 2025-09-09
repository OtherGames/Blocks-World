// GreedyMesher.cs
using System;
using System.Collections.Generic;
using UnityEngine;

// ѕроста€ структура дл€ результата перед созданием Unity Mesh
public class MeshData
{
    public List<Vector3> verts = new List<Vector3>();
    public List<int> tris = new List<int>();
    public List<Vector2> uvs = new List<Vector2>();
    public List<Vector3> normals = new List<Vector3>();

    public Mesh ToUnityMesh()
    {
        Mesh m = new Mesh();
        m.indexFormat = verts.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.SetUVs(0, uvs);
        m.SetNormals(normals);
        m.RecalculateBounds();
        return m;
    }
}

public static class GreedyMesherEbat
{
    // faces: 0 - +X, 1 - -X, 2 - +Y, 3 - -Y, 4 - +Z, 5 - -Z
    // face normals for adding correct normals
    static readonly Vector3[] faceNormals = {
        new Vector3(1,0,0), new Vector3(-1,0,0),
        new Vector3(0,1,0), new Vector3(0,-1,0),
        new Vector3(0,0,1), new Vector3(0,0,-1)
    };

    // ќсновной метод: blocks[x,y,z] -> MeshData
    // getBlock: функци€, возвращающа€ блок id дл€ координат (может возвращать 0 дл€ воздуха или за пределами)
    // isOpaque: функци€, определ€юща€, €вл€етс€ ли блок непрозрачным (дл€ мерджа непрозрачных)
    // tileSize, atlasWidth, atlasHeight Ч дл€ UV. ≈сли нет атласа, возвращай простые UV.
    public static MeshData GenerateMesh(Func<int, int, int, byte> getBlock, Func<byte, bool> isOpaque, int sizeX, int sizeY, int sizeZ,
                                        Func<byte, int, Vector2[]> getUVsForFace = null)
    {
        MeshData mesh = new MeshData();

        // локальна€ функци€ чтени€ блока с воздухом по умолчанию
        byte Read(int x, int y, int z)
        {
            if (x < 0 || x >= sizeX || y < 0 || y >= sizeY || z < 0 || z >= sizeZ) return 0;
            return getBlock(x, y, z);
        }

        // если нет UV-функции, делаем простую заглушку с 0..1 uv квадом
        if (getUVsForFace == null)
        {
            getUVsForFace = (id, face) => new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        }

        // ѕеребираем 3 направлени€ (ось d = 0,1,2)
        for (int d = 0; d < 3; d++)
        {
            int u = (d + 1) % 3;
            int v = (d + 2) % 3;

            int[] dims = new int[3] { sizeX, sizeY, sizeZ };

            int iU = dims[u];
            int iV = dims[v];
            int iD = dims[d];

            // mask размером iU * iV
            byte[] mask = new byte[iU * iV];
            byte[] maskBlockId = new byte[iU * iV]; // сохран€ем id блока владеющего видимой гранью
            bool[] maskFacePositive = new bool[iU * iV]; // true если видна положительна€ сторона

            // дл€ каждого среза вдоль d
            for (int slice = 0; slice <= iD; slice++)
            {
                // build mask
                for (int j = 0; j < iV; j++)
                    for (int i = 0; i < iU; i++)
                    {
                        // координаты в 3D дл€ позиции (slice, i, j) с осью d
                        int[] a = new int[3];
                        int[] b = new int[3];
                        a[d] = slice;
                        b[d] = slice - 1;
                        a[u] = i; a[v] = j;
                        b[u] = i; b[v] = j;

                        byte blockA = (a[d] >= 0 && a[d] < iD) ? Read(a[0], a[1], a[2]) : (byte)0;
                        byte blockB = (b[d] >= 0 && b[d] < iD) ? Read(b[0], b[1], b[2]) : (byte)0;

                        bool aOpaque = isOpaque(blockA);
                        bool bOpaque = isOpaque(blockB);

                        // если одна сторона непрозрачна, а друга€ нет => видима€ грань
                        if (aOpaque == bOpaque)
                        {
                            mask[i + j * iU] = 0;
                            maskBlockId[i + j * iU] = 0;
                            maskFacePositive[i + j * iU] = false;
                        }
                        else if (aOpaque && !bOpaque)
                        {
                            // грань смотрит в -d направлении (т.е. негативна€ сторона блока a is visible) Ч
                            // но мы помечаем маску как положительную сторону дл€ slice
                            mask[i + j * iU] = 1;
                            maskBlockId[i + j * iU] = blockA;
                            maskFacePositive[i + j * iU] = false; // face is toward negative d (we'll use face index 2*d+1)
                        }
                        else // !aOpaque && bOpaque
                        {
                            mask[i + j * iU] = 1;
                            maskBlockId[i + j * iU] = blockB;
                            maskFacePositive[i + j * iU] = true; // face toward positive d (face index 2*d)
                        }
                    }

                // greedy merge on mask
                for (int j = 0; j < iV; j++)
                {
                    for (int i = 0; i < iU;)
                    {
                        int idx = i + j * iU;
                        if (mask[idx] == 0) { i++; continue; }

                        // determine width
                        int w = 1;
                        byte id = maskBlockId[idx];
                        bool facePos = maskFacePositive[idx];
                        while (i + w < iU && mask[(i + w) + j * iU] != 0 && maskBlockId[(i + w) + j * iU] == id && maskFacePositive[(i + w) + j * iU] == facePos)
                            w++;

                        // determine height
                        int h = 1;
                        bool done = false;
                        while (j + h < iV && !done)
                        {
                            for (int k = 0; k < w; k++)
                            {
                                int idx2 = (i + k) + (j + h) * iU;
                                if (mask[idx2] == 0 || maskBlockId[idx2] != id || maskFacePositive[idx2] != facePos)
                                {
                                    done = true; break;
                                }
                            }
                            if (!done) h++;
                        }

                        // We have rectangle (i, j) size w x h. Create quad.
                        // Compute coordinates of the quad in 3D space
                        int[] pos = new int[3];
                        pos[d] = slice;          // slice is the plane between blocks
                        pos[u] = i;
                        pos[v] = j;

                        // Depending on face orientation (positive/negative), the quad is at slice or slice-1
                        // We'll compute the four corner vertices in order to make consistent winding (CW/CCW)
                        Vector3 du = Vector3.zero, dv = Vector3.zero;
                        du[u] = w;
                        dv[v] = h;

                        // compute base point
                        Vector3 basePoint = new Vector3(pos[0], pos[1], pos[2]);

                        // If face is positive (visible on +d side), base point stays at slice, normal = +axis
                        // If face is negative, base needs shift by -1 along d so quad sits on correct face.
                        int faceIndex = facePos ? (2 * d) : (2 * d + 1); // mapping to our face normals
                        Vector3 normal = faceNormals[faceIndex];

                        if (!facePos)
                            basePoint[d] -= 1f;

                        // convert basePoint + du/dv to actual vertices (vertex positions)
                        // We need four corners: (0,0), (w,0), (w,h), (0,h) in uv space
                        Vector3 p0 = basePoint;
                        Vector3 p1 = basePoint + ToVector3(du);
                        Vector3 p2 = basePoint + ToVector3(du) + ToVector3(dv);
                        Vector3 p3 = basePoint + ToVector3(dv);

                        // If normal points negative, we must invert winding to keep triangle front-facing.
                        int vertStart = mesh.verts.Count;
                        if (Vector3.Dot(normal, Vector3.up) < -0.5f || Vector3.Dot(normal, Vector3.right) < -0.5f || Vector3.Dot(normal, Vector3.forward) < -0.5f)
                        {
                            // negative normal Ч add reversed order
                            mesh.verts.Add(p0); mesh.verts.Add(p3); mesh.verts.Add(p2); mesh.verts.Add(p1);
                            mesh.tris.Add(vertStart + 0); mesh.tris.Add(vertStart + 1); mesh.tris.Add(vertStart + 2);
                            mesh.tris.Add(vertStart + 0); mesh.tris.Add(vertStart + 2); mesh.tris.Add(vertStart + 3);
                        }
                        else
                        {
                            // positive normal
                            mesh.verts.Add(p0); mesh.verts.Add(p1); mesh.verts.Add(p2); mesh.verts.Add(p3);
                            mesh.tris.Add(vertStart + 0); mesh.tris.Add(vertStart + 1); mesh.tris.Add(vertStart + 2);
                            mesh.tris.Add(vertStart + 0); mesh.tris.Add(vertStart + 2); mesh.tris.Add(vertStart + 3);
                        }

                        // normals
                        for (int n = 0; n < 4; n++) mesh.normals.Add(normal);

                        // UVs from atlas function (face index 0..5, but we use faceIndex)
                        Vector2[] faceUvs = getUVsForFace(id, faceIndex);
                        // expecting faceUvs length == 4 in correct order
                        mesh.uvs.Add(faceUvs[0]);
                        mesh.uvs.Add(faceUvs[1]);
                        mesh.uvs.Add(faceUvs[2]);
                        mesh.uvs.Add(faceUvs[3]);

                        // zero out mask region
                        for (int jj = 0; jj < h; jj++)
                            for (int ii = 0; ii < w; ii++)
                            {
                                int mIdx = (i + ii) + (j + jj) * iU;
                                mask[mIdx] = 0;
                            }

                        // advance i
                        i += w;
                    }
                } // end greedy loop
            } // end slices
        } // end dims

        return mesh;
    }

    static Vector3 ToVector3(int[] a)
    {
        return new Vector3(a[0], a[1], a[2]);
    }

    static Vector3 ToVector3(Vector3 v) => new Vector3(v.x, v.y, v.z);
}
