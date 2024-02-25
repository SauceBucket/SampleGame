using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

[SelectionBase]
public class VoxelGrid : MonoBehaviour
{

    public GameObject voxelpf;

    Voxel[] voxels;

    float voxelsize , gridSize;

    int resolution;

    Material[] voxelMaterials;

    Mesh mesh;

    private List<Vector3> vert;

    private List<int> triangles;

    public VoxelGrid xNeighbor, yNeighbor, xyNeighbor;

    private Voxel dummyX, dummyY, dummyT;

    private int[] rowCacheMax, rowCacheMin;

    private int edgeCacheMin, edgeCacheMax;

    private float sharpFeatureLimit;

    private void Awake()
    {
        
    }

    public void Initialize(int resolution, float size, float maxangle)
    {
        sharpFeatureLimit = Mathf.Cos(maxangle * Mathf.Deg2Rad);
        gridSize = size;
        this.resolution = resolution;
        voxelsize = size / resolution;
        voxels = new Voxel[resolution * resolution];
        voxelMaterials = new Material[voxels.Length];

        dummyX = new Voxel();
        dummyY = new Voxel();
        dummyT = new Voxel();

        for (int i = 0, y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++, i++)
            {
                CreateVoxel(i, x, y, size);
            }
        }

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "VoxelGrid Mesh";
        vert = new List<Vector3>(); 
        triangles = new List<int>();

        rowCacheMax = new int[resolution * 2 + 1];
        rowCacheMin = new int[resolution * 2 + 1];
        Refresh();

    }


    public void CreateVoxel(int i, int x, int y, float size) {
        GameObject o = Instantiate(voxelpf) as GameObject;
        o.transform.parent = transform;
        o.transform.localPosition = new Vector3((x + size * .5f) * voxelsize, (y + size * .5f) * voxelsize, 0.01f);
        o.transform.localScale = Vector3.one * voxelsize * .9f * .1f;
        voxelMaterials[i] = o.GetComponent<MeshRenderer>().material;
        voxels[i] = new Voxel(x, y, voxelsize);
    }

    private void SetVoxelColors()
    {
        for (int i = 0; i < voxels.Length; i++)
        {
            voxelMaterials[i].color = voxels[i].state ? Color.black : Color.white;
        }
    }

    private void Refresh()
    {
        SetVoxelColors();
        Triangulate();
    }

    private void Triangulate() { 
        vert.Clear();
        triangles.Clear();
        mesh.Clear();

        FillFirstRowCache();

        TriangulateCellRows();

        if (yNeighbor != null)
        {
            TriangulateGapRow();
        }


        mesh.vertices = vert.ToArray();
        mesh.triangles = triangles.ToArray();
    }
    private void TriangulateGapRow()
    {
        dummyY.BecomeDummyOf_YDir(yNeighbor.voxels[0], gridSize);
        int cells = resolution - 1;
        int offset = cells * resolution;
        SwapRowCaches();
        CacheFirstCorner(dummyY);
        CacheNextYEdge(voxels[cells * resolution], dummyY);

        for (int x = 0; x < cells; x++)
        {
            Voxel dummySwap = dummyT;
            dummySwap.BecomeDummyOf_YDir(yNeighbor.voxels[x + 1], gridSize);
            dummyT = dummyY;
            dummyY = dummySwap;
            int cacheidx = x * 2;
            CacheNextXEdgeAndCorner(cacheidx, dummyT, dummyY);
            CacheNextYEdge(voxels[x + offset + 1], dummyY);
            TriangulateCell(cacheidx, voxels[x + offset], voxels[x + offset + 1], dummyT, dummyY);
        }
        if (xNeighbor != null)
        {
            int cacheidx = cells * 2;
            dummyT.BecomeDummyOf_BothDir(xyNeighbor.voxels[0], gridSize);
            CacheNextXEdgeAndCorner(cacheidx, dummyY, dummyT);
            CacheNextYEdge(dummyX, dummyT);
            TriangulateCell(cacheidx,voxels[voxels.Length - 1], dummyX, dummyY, dummyT);
        }
    }
    private void TriangulateCellRows()
    {
        
        int cells = resolution - 1;
        for (int i = 0, y = 0; y < cells; y++, i++)
        {
            SwapRowCaches();
            CacheFirstCorner(voxels[i + resolution]);
            CacheNextYEdge(voxels[i], voxels[i + resolution]);
            for (int x = 0; x < cells; x++, i++)
            {
                
                Voxel
                    a = voxels[i],
                    b = voxels[i + 1],
                    c = voxels[i + resolution],
                    d = voxels[i + resolution + 1];
                int cacheidx = x * 2;
                CacheNextXEdgeAndCorner(cacheidx, c, d);
                CacheNextYEdge(b,d);
                TriangulateCell(cacheidx,a, b,c,d);


            }
            if (xNeighbor != null)
            {
                TriangulateGapCell(i);
            }
            
        }
    }

    private void SwapRowCaches() {

        int[] temp = rowCacheMax;
        rowCacheMax = rowCacheMin;
        rowCacheMin = temp;

    }

    private void TriangulateGapCell(int i) {
        Voxel dummySwap = dummyT;
        dummySwap.BecomeDummyOf_XDir(xNeighbor.voxels[i+1], gridSize);
        dummyT = dummyX;
        dummyX = dummySwap;
        int cacheidx = (resolution - 1) * 2;
        CacheNextXEdgeAndCorner(cacheidx, voxels[i + resolution], dummyX);
        CacheNextYEdge(dummyT, dummyX);
        TriangulateCell(cacheidx, voxels[i], dummyT, voxels[i + resolution], dummyX);
    }

    private void TriangulateCell(int i, Voxel bl, Voxel br, Voxel tl, Voxel tr)
    {

        int celltype = 0;

        if (bl.state)
        {
            celltype |= 1;
        }
        if (br.state)
        {
            celltype |= 2;
        }
        if (tl.state)
        {
            celltype |= 4;
        }
        if (tr.state)
        {
            celltype |= 8;
        }

        switch (celltype)
        {
            case 0: TriangulateCase0(i, bl, br, tl, tr); break;
            case 1: TriangulateCase1(i, bl, br, tl, tr); break;
            case 2: TriangulateCase2(i, bl, br, tl, tr); break;
            case 3: TriangulateCase3(i, bl, br, tl, tr); break;
            case 4: TriangulateCase4(i, bl, br, tl, tr); break;
            case 5: TriangulateCase5(i, bl, br, tl, tr); break;
            case 6: TriangulateCase6(i, bl, br, tl, tr); break;
            case 7: TriangulateCase7(i, bl, br, tl, tr); break;
            case 8: TriangulateCase8(i, bl, br, tl, tr); break;
            case 9: TriangulateCase9(i, bl, br, tl, tr); break;
            case 10: TriangulateCase10(i, bl, br, tl, tr); break;
            case 11: TriangulateCase11(i, bl, br, tl, tr); break;
            case 12: TriangulateCase12(i, bl, br, tl, tr); break;
            case 13: TriangulateCase13(i, bl, br, tl, tr); break;
            case 14: TriangulateCase14(i, bl, br, tl, tr); break;
            case 15: TriangulateCase15(i, bl, br, tl, tr); break;
            
        }
    }
    #region triangulation!!
    private void TriangulateCase0(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        return;
    }
    private void TriangulateCase1(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddTriangleA(i);
    }

    private void TriangulateCase2(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddTriangleB(i);
    }

    private void TriangulateCase3(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddQuadAB(i);
    }

    private void TriangulateCase4(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddTriangleC(i);
    }

    private void TriangulateCase5(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddQuadAC(i);
    }

    private void TriangulateCase6(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddTriangleB(i);
        AddTriangleC(i);
    }

    private void TriangulateCase7(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddPentagonABC(i);
    }

    private void TriangulateCase8(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddTriangleD(i);
    }

    private void TriangulateCase9(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddTriangleA(i);
        AddTriangleD(i);
    }

    private void TriangulateCase10(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddQuadBD(i);
    }

    private void TriangulateCase11(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddPentagonABD(i);
    }

    private void TriangulateCase12(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddQuadCD(i);
    }

    private void TriangulateCase13(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddPentagonACD(i);
    }

    private void TriangulateCase14(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddPentagonBCD(i);
    }

    private void TriangulateCase15(int i, Voxel a, Voxel b, Voxel c, Voxel d)
    {
        AddQuadABCD(i);
    }

    private void AddQuadABCD(int i)
    {
        AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 2], rowCacheMin[i + 2]);
    }

    private void AddTriangleA(int i)
    {
        AddTriangle(rowCacheMin[i], edgeCacheMin, rowCacheMin[i + 1]);
    }

    private void AddTriangleB(int i)
    {
        AddTriangle(rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMax);
    }

    private void AddTriangleC(int i)
    {
        AddTriangle(rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMin);
    }

    private void AddTriangleD(int i)
    {
        AddTriangle(rowCacheMax[i + 2], edgeCacheMax, rowCacheMax[i + 1]);
    }

    private void AddPentagonABC(int i)
    {
        AddPentagon(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], edgeCacheMax, rowCacheMin[i + 2]);
    }

    private void AddPentagonABD(int i)
    {
        AddPentagon(rowCacheMin[i + 2], rowCacheMin[i], edgeCacheMin, rowCacheMax[i + 1], rowCacheMax[i + 2]);
    }

    private void AddPentagonACD(int i)
    {
        AddPentagon(rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax, rowCacheMin[i + 1], rowCacheMin[i]);
    }

    private void AddPentagonBCD(int i)
    {
        AddPentagon(rowCacheMax[i + 2], rowCacheMin[i + 2], rowCacheMin[i + 1], edgeCacheMin, rowCacheMax[i]);
    }

    private void AddQuadAB(int i)
    {
        AddQuad(rowCacheMin[i], edgeCacheMin, edgeCacheMax, rowCacheMin[i + 2]);
    }

    private void AddQuadAC(int i)
    {
        AddQuad(rowCacheMin[i], rowCacheMax[i], rowCacheMax[i + 1], rowCacheMin[i + 1]);
    }

    private void AddQuadBD(int i)
    {
        AddQuad(rowCacheMin[i + 1], rowCacheMax[i + 1], rowCacheMax[i + 2], rowCacheMin[i + 2]);
    }

    private void AddQuadCD(int i)
    {
        AddQuad(edgeCacheMin, rowCacheMax[i], rowCacheMax[i + 2], edgeCacheMax);
    }

    private bool IsSharpFeature(Vector2 n1, Vector2 n2) { 
        float dot = Vector2.Dot(n1, n2);
        return dot >= sharpFeatureLimit && dot <= .09999f;
    }

    #endregion
    private void FillFirstRowCache() {
        CacheFirstCorner(voxels[0]);
        int i;
        for (i = 0; i < resolution - 1 ;++i)
        {
            CacheNextXEdgeAndCorner(i*2,voxels[i],voxels[i+1]);
        }

        if (xNeighbor != null)
        {
            dummyX.BecomeDummyOf_XDir(xNeighbor.voxels[0], gridSize);
            CacheNextXEdgeAndCorner(i * 2, voxels[i], dummyX);
        }

    }

    private void CacheFirstCorner(Voxel vox) {

        if (vox.state == true) {
            rowCacheMax[0] = vert.Count;
            vert.Add(vox.position);
        }
    
    
    }

    private void CacheNextXEdgeAndCorner(int i, Voxel xMin, Voxel xMax) {

        if (xMin.state != xMax.state) { 
            
            rowCacheMax[i+1] = vert.Count;
            Vector3 pos = new Vector3(
                xMin.xEdgePosition,
                xMin.position.y,
                0
            );

            vert.Add(pos);
        }
        if (xMax.state == true ) {
            rowCacheMax[i + 2] = vert.Count;
            vert.Add(xMax.position);
        }   

    }

    private void CacheNextYEdge(Voxel yMin, Voxel yMax) {
        edgeCacheMin = edgeCacheMax;
        if (yMin.state != yMax.state) { 
            edgeCacheMax = vert.Count;
            Vector3 pos = new Vector3(
                yMin.position.x,
                yMin.yEdgePosition,
                0
            );
            vert.Add(pos);
        
        }
        
    }


    private void AddTriangle(int a, int b, int c)
    {

        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);



    }

    private void AddQuad(int a, int b, int c, int d )
    {

        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);



    }
    private void AddPentagon(int a, int b, int c, int d, int e)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
        triangles.Add(a);
        triangles.Add(d);
        triangles.Add(e);
    }

    public void Apply(VoxelStencil stencil)
    {
        int xStart = (int)(stencil.XStart / voxelsize);
        if (xStart < 0)
        {
            xStart = 0;
        }
        int xEnd = (int)(stencil.XEnd / voxelsize);
        if (xEnd >= resolution)
        {
            xEnd = resolution - 1;
        }
        int yStart = (int)(stencil.YStart / voxelsize);
        if (yStart < 0)
        {
            yStart = 0;
        }
        int yEnd = (int)(stencil.YEnd / voxelsize);
        if (yEnd >= resolution)
        {
            yEnd = resolution - 1;
        }

        for (int y = yStart; y <= yEnd; ++y) {
            int i = y * resolution + xStart;
            for (int x = xStart; x <= xEnd; ++x, ++i) {
                stencil.Apply(voxels[i]);
            }
        }
        SetIntersections(stencil, xStart, xEnd, yStart, yEnd);
        Refresh();
    }

    private void SetIntersections(VoxelStencil stencil, int xStart, int xEnd, int yStart, int yEnd)
    {
        bool crossHorizontalGap = false;
        bool lastVerticalRow = false;
        bool crossVerticalGap = false;

        if (xStart > 0)
        {
            xStart--;
        }
        if (xEnd == resolution - 1)
        {
            xEnd--;
            crossHorizontalGap = xNeighbor != null;
        }
        if (yStart > 0)
        {
            yStart--;
        }
        if (yEnd == resolution - 1)
        {
            yEnd--;
            lastVerticalRow = true;
            crossVerticalGap = yNeighbor != null;
        }
        Voxel a, b;
        for (int y = yStart; y <= yEnd; ++y)
        {
            int i = y * resolution + xStart;
            b = voxels[i];
            for (int x = xStart; x <= xEnd; x++, i++)
            {
                a = b;
                b = voxels[i + 1];
                stencil.SetHorizontalIntersection(a, b);
                stencil.SetVerticalIntersection(a, voxels[i + resolution]);
            }
            stencil.SetVerticalIntersection(b, voxels[i + resolution]);
            if (crossHorizontalGap)
            {
                dummyX.BecomeDummyOf_XDir(xNeighbor.voxels[y * resolution], gridSize);
                stencil.SetHorizontalIntersection(b, dummyX);
            }
            
        }
        if (lastVerticalRow)
        {
            int i = voxels.Length - resolution + xStart;
            b = voxels[i];
            for (int x = xStart; x <= xEnd; x++, i++)
            {
                a = b;
                b = voxels[i + 1];
                stencil.SetHorizontalIntersection(a, b);
                if (crossVerticalGap)
                {
                    dummyY.BecomeDummyOf_YDir(yNeighbor.voxels[x], gridSize);
                    stencil.SetVerticalIntersection(a, dummyY);
                }
            }
            if (crossVerticalGap)
            {
                dummyY.BecomeDummyOf_YDir(yNeighbor.voxels[xEnd + 1], gridSize);
                stencil.SetVerticalIntersection(b, dummyY);
            }
            if (crossHorizontalGap)
            {
                dummyX.BecomeDummyOf_XDir(xNeighbor.voxels[voxels.Length - resolution], gridSize);
                stencil.SetHorizontalIntersection(b, dummyX);
            }
        }
    }


}
