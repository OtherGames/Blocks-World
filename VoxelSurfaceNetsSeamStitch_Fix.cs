// VoxelSurfaceNetsSeamStitch_Fix.cs
// Place this in Assets/... and call GenerateLodMesh(...) as shown in the debug helper.
// (See accompanying DumpChunkToFile.cs and DebugSaveMeshObj.cs)
using System;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelSurfaceNetsSeamStitch_Fix
{
    static readonly int[,] cornerOffset = new int[8,3] {
        {0,0,0},{1,0,0},{0,1,0},{1,1,0},{0,0,1},{1,0,1},{0,1,1},{1,1,1}
    };
    static readonly int[,] edgeCorners = new int[12,2] {
        {0,1},{1,3},{3,2},{2,0},{4,5},{5,7},{7,6},{6,4},{0,4},{1,5},{3,7},{2,6}
    };

    public static Mesh GenerateLodMesh(byte[,,] localBlocks, int lod, float voxelSize,
        int originX, int originY, int originZ, Func<int,int,int,byte> globalSampler, int? overrideSdfRadius = null)
    {
        if (localBlocks == null) throw new ArgumentNullException(nameof(localBlocks));
        int sx = localBlocks.GetLength(0), sy = localBlocks.GetLength(1), sz = localBlocks.GetLength(2);
        if (sx==0||sy==0||sz==0) return new Mesh();
        int factor = 1 << Math.Max(0,lod);
        int gx = Mathf.CeilToInt((float)sx / factor), gy = Mathf.CeilToInt((float)sy / factor), gz = Mathf.CeilToInt((float)sz / factor);
        int pad = Math.Max(1, overrideSdfRadius ?? Math.Max(4, factor * 4));
        int ex = sx + 2*pad, ey = sy + 2*pad, ez = sz + 2*pad;
        int extOriginX = originX - pad, extOriginY = originY - pad, extOriginZ = originZ - pad;
        var extended = new byte[ex,ey,ez];
        for (int z=0; z<ez; z++) for (int y=0; y<ey; y++) for (int x=0; x<ex; x++)
        {
            int wx = extOriginX + x, wy = extOriginY + y, wz = extOriginZ + z;
            byte v = 0;
            if (globalSampler != null) { try { v = globalSampler(wx,wy,wz);} catch { v = 0; } }
            else { int lx = wx - originX, ly = wy - originY, lz = wz - originZ; if (lx>=0 && ly>=0 && lz>=0 && lx<sx && ly<sy && lz<sz) v = localBlocks[lx,ly,lz]; else v = 0; }
            extended[x,y,z] = v;
        }

        Func<int,int,int,byte> SampleExtended = (bx,by,bz) =>
        {
            int lx = bx - extOriginX, ly = by - extOriginY, lz = bz - extOriginZ;
            if (lx>=0 && ly>=0 && lz>=0 && lx<ex && ly<ey && lz<ez) return extended[lx,ly,lz];
            return (byte)0;
        };

        int cx = gx+1, cy = gy+1, cz = gz+1;
        var sdf = new float[cx,cy,cz];
        for (int iz=0; iz<cz; iz++) for (int iy=0; iy<cy; iy++) for (int ix=0; ix<cx; ix++)
        {
            int cornerWorldX = originX + ix*factor, cornerWorldY = originY + iy*factor, cornerWorldZ = originZ + iz*factor;
            float nearestSolid = float.MaxValue, nearestEmpty = float.MaxValue;
            for (int r=0;r<=pad;r++)
            {
                if (nearestSolid!=float.MaxValue && nearestEmpty!=float.MaxValue) break;
                for (int dz=-r; dz<=r; dz++) for (int dy=-(r - Math.Abs(dz)); dy<= (r - Math.Abs(dz)); dy++)
                {
                    int dx = r - Math.Abs(dz) - Math.Abs(dy); if (dx<0) continue;
                    int[] signs = (dx==0)? new int[]{0} : new int[]{-1,1};
                    foreach(var s in signs)
                    {
                        int ddx = s*dx;
                        int bx = cornerWorldX + ddx, by = cornerWorldY + dy, bz = cornerWorldZ + dz;
                        byte id = SampleExtended(bx,by,bz);
                        float dist = Mathf.Sqrt(ddx*ddx + dy*dy + dz*dz);
                        if (id!=0) { if (dist < nearestSolid) nearestSolid = dist; } else { if (dist < nearestEmpty) nearestEmpty = dist; }
                    }
                }
            }
            sdf[ix,iy,iz] = (nearestSolid - nearestEmpty) * voxelSize;
        }

        float iso = 0f;
        var edgeCache = new Dictionary<string,int>();
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var cellVert = new int[gx,gy,gz];
        for (int z=0; z<gz; z++) for (int y=0; y<gy; y++) for (int x=0; x<gx; x++) cellVert[x,y,z] = -1;

        for (int z=0; z<gz; z++) for (int y=0; y<gy; y++) for (int x=0; x<gx; x++)
        {
            float[] d = new float[8];
            for (int c=0;c<8;c++){ int cx0 = x + cornerOffset[c,0]; int cy0 = y + cornerOffset[c,1]; int cz0 = z + cornerOffset[c,2]; d[c] = sdf[cx0,cy0,cz0]; }
            bool allPos=true, allNeg=true; for (int i=0;i<8;i++){ if (d[i] < iso) allPos=false; if (d[i] >= iso) allNeg=false; }
            if (allPos || allNeg) continue;
            Vector3 acc = Vector3.zero; int ac = 0;
            for (int e=0;e<12;e++)
            {
                int ca = edgeCorners[e,0], cb = edgeCorners[e,1];
                float da = d[ca], db = d[cb];
                if ((da < iso && db < iso) || (da >= iso && db >= iso)) continue;
                float denom = (db - da); float t = (Math.Abs(denom) > 1e-9f)? ((iso - da)/denom) : 0.5f;
                int aBlockX = originX + (x + cornerOffset[ca,0]) * factor;
                int aBlockY = originY + (y + cornerOffset[ca,1]) * factor;
                int aBlockZ = originZ + (z + cornerOffset[ca,2]) * factor;
                int bBlockX = originX + (x + cornerOffset[cb,0]) * factor;
                int bBlockY = originY + (y + cornerOffset[cb,1]) * factor;
                int bBlockZ = originZ + (z + cornerOffset[cb,2]) * factor;
                long ax=aBlockX, ay=aBlockY, az=aBlockZ, bx=bBlockX, by=bBlockY, bz=bBlockZ;
                if (CompareTriplet(ax,ay,az,bx,by,bz) > 0) { long tx=ax, ty=ay, tz=az; ax=bx; ay=by; az=bz; bx=tx; by=ty; bz=tz; }
                string key = $"{ax},{ay},{az}|{bx},{by},{bz}|{e}";
                if (!edgeCache.TryGetValue(key, out int vid))
                {
                    Vector3 pa = new Vector3((aBlockX + 0.5f) * voxelSize, (aBlockY + 0.5f) * voxelSize, (aBlockZ + 0.5f) * voxelSize);
                    Vector3 pb = new Vector3((bBlockX + 0.5f) * voxelSize, (bBlockY + 0.5f) * voxelSize, (bBlockZ + 0.5f) * voxelSize);
                    Vector3 pos = Vector3.Lerp(pa, pb, Mathf.Clamp01(t));
                    vid = verts.Count; verts.Add(pos); edgeCache[key] = vid;
                }
                acc += verts[edgeCache[key]]; ac++;
            }
            Vector3 finalPos = (ac>0)? (acc/ac) : new Vector3((originX + (x + 0.5f) * factor) * voxelSize, (originY + (y + 0.5f) * factor) * voxelSize, (originZ + (z + 0.5f) * factor) * voxelSize);
            int idx = verts.Count; verts.Add(finalPos); cellVert[x,y,z] = idx;
        }

        for (int z=0; z<gz; z++) for (int y=0; y<gy; y++) for (int x=0; x<gx; x++)
        {
            int v0 = cellVert[x,y,z]; if (v0<0) continue;
            if (x+1<gx && y+1<gy) { float a=sdf[x+1,y,z], b=sdf[x+1,y+1,z], c=sdf[x+1,y,z+1], d=sdf[x+1,y+1,z+1]; if (!SameSign(a,b,c,d,iso)){ int v1=cellVert[x+1,y,z], v2=cellVert[x+1,y+1,z], v3=cellVert[x,y+1,z]; if (v1>=0&&v2>=0&&v3>=0) AddQuad(tris,v0,v1,v2,v3); } }
            if (y+1<gy && x+1<gx) { float a=sdf[x,y+1,z], b=sdf[x+1,y+1,z], c=sdf[x,y+1,z+1], d=sdf[x+1,y+1,z+1]; if (!SameSign(a,b,c,d,iso)){ int v1=cellVert[x,y+1,z], v2=cellVert[x+1,y+1,z], v3=cellVert[x+1,y,z]; if (v1>=0&&v2>=0&&v3>=0) AddQuad(tris,v0,v1,v2,v3); } }
            if (z+1<gz && x+1<gx) { float a=sdf[x,y,z+1], b=sdf[x+1,y,z+1], c=sdf[x,y+1,z+1], d=sdf[x+1,y+1,z+1]; if (!SameSign(a,b,c,d,iso)){ int v1=cellVert[x,y,z+1], v2=cellVert[x+1,y,z+1], v3=cellVert[x+1,y,z]; if (v1>=0&&v2>=0&&v3>=0) AddQuad(tris,v0,v1,v2,v3); } }
        }

        WeldAndClean(ref verts, ref tris, Mathf.Max(1e-4f * voxelSize * factor, 1e-4f));
        Mesh mesh = new Mesh();
        mesh.indexFormat = (verts.Count > 65000) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts); mesh.SetTriangles(tris,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return mesh;
    }

    static bool SameSign(float a,float b,float c,float d,float iso){ bool A=a>=iso,B=b>=iso,C=c>=iso,D=d>=iso; return (A&&B&&C&&D) || (!A&&!B&&!C&&!D); }
    static void AddQuad(List<int> tris,int a,int b,int c,int d){ tris.Add(a);tris.Add(b);tris.Add(c);tris.Add(a);tris.Add(c);tris.Add(d); }
    static int CompareTriplet(long ax,long ay,long az,long bx,long by,long bz){ if (ax!=bx) return ax<bx?-1:1; if (ay!=by) return ay<by?-1:1; if (az!=bz) return az<bz?-1:1; return 0; }
    static void WeldAndClean(ref List<Vector3> verts, ref List<int> tris, float weldThreshold)
    {
        var cleaned = new List<int>(); for (int i=0;i<tris.Count;i+=3){ int a=tris[i],b=tris[i+1],c=tris[i+2]; if (a==b||b==c||a==c) continue; Vector3 va=verts[a], vb=verts[b], vc=verts[c]; if (Vector3.Cross(vb-va,vc-va).sqrMagnitude<=1e-12f) continue; cleaned.Add(a); cleaned.Add(b); cleaned.Add(c); } tris=cleaned;
        float inv = (weldThreshold>0f)?1f/weldThreshold:1f; var map = new Dictionary<long,int>(); var newVerts=new List<Vector3>(); var remap=new int[verts.Count];
        for (int i=0;i<verts.Count;i++){ Vector3 v=verts[i]; long xi=(long)Mathf.Round(v.x*inv), yi=(long)Mathf.Round(v.y*inv), zi=(long)Mathf.Round(v.z*inv); long key=(xi & 0x1FFFFF) | ((yi & 0x1FFFFF) << 21) | ((zi & 0x1FFFFF) << 42); if (map.TryGetValue(key,out int idx)) remap[i]=idx; else { idx=newVerts.Count; newVerts.Add(v); map[key]=idx; remap[i]=idx; } }
        var final = new List<int>(); for (int i=0;i<tris.Count;i+=3){ int a=remap[tris[i]], b=remap[tris[i+1]], c=remap[tris[i+2]]; if (a==b||b==c||a==c) continue; Vector3 va=newVerts[a], vb=newVerts[b], vc=newVerts[c]; if (Vector3.Cross(vb-va,vc-va).sqrMagnitude<=1e-12f) continue; final.Add(a); final.Add(b); final.Add(c); } tris=final; verts=newVerts;
    }
}
