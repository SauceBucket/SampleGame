using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class VoxelMap : MonoBehaviour
{
    public float size = 2f;

    public int VoxelGridResolution;

    public int chunkResolution;

    public VoxelGrid voxelGridPrefab;

    private VoxelGrid[] chunks;

    private float chunkSize, voxelSize, halfSize;

    private static string[] fillTypeNames = { "Filled", "Empty" };

    private static string[] radiusNames = { "0", "1", "2", "3", "4", "5" };

    private static string[] stencilNames = {"Square", "Circle" };

    private int fillTypeIndex,radius_index,stencil_index;

    public Transform[] StencilShapes;

    public bool snapToGrid;

    VoxelStencil[] Stencil_List = { 
        new VoxelStencil(),
        new VoxelStencilCircle()
    };

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(40f, 40f, 150f, 450f));
        GUILayout.Label("Fill Type");
        fillTypeIndex = GUILayout.SelectionGrid(fillTypeIndex, fillTypeNames, 2);
        GUILayout.Label("Radius");
        radius_index = GUILayout.SelectionGrid(radius_index, radiusNames, 6);
        GUILayout.Label("Stencil");
        stencil_index = GUILayout.SelectionGrid(stencil_index, stencilNames, 2);
        GUILayout.EndArea();
    }

    private void Awake()
    {

        halfSize = size * 0.5f;
        chunkSize = size / chunkResolution;
        voxelSize = chunkSize / VoxelGridResolution;

        chunks = new VoxelGrid[chunkResolution * chunkResolution];
        for (int i = 0, y = 0; y < chunkResolution; y++)
        {
            for (int x = 0; x < chunkResolution; x++, i++)
            {
                CreateChunk(i, x, y);
            }
        }
  
        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(size, size);
        

    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Transform visualization = StencilShapes[stencil_index];
        RaycastHit hitInfo;
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo) &&
            hitInfo.collider.gameObject == gameObject)
        {
            Vector2 center = transform.InverseTransformPoint(hitInfo.point);
            center.x += halfSize;
            center.y += halfSize;
            if (snapToGrid) {
                center.x = ((int)(center.x / voxelSize) + 0.5f) * voxelSize;
                center.y = ((int)(center.y / voxelSize) + 0.5f) * voxelSize;
            }
            if (Input.GetMouseButton(0))
            {
                EditVoxels(center);
            }

            center.x -= halfSize;
            center.y -= halfSize;
            visualization.localPosition = center;
            visualization.localScale = Vector3.one * ((radius_index + 0.5f) * voxelSize * 2f);
            visualization.gameObject.SetActive(true);
        }
        else
        {
            visualization.gameObject.SetActive(false);
        }
    }

    void EditVoxels(Vector2 center) {

        VoxelStencil activeStencil = Stencil_List[stencil_index];
        activeStencil.Initialize(fillTypeIndex == 0, (radius_index + 0.5f) * voxelSize);
        activeStencil.SetCenter(center.x, center.y);

        //this is start and end for CHUNKS, not voxels.
        int xStart = (int)((activeStencil.XStart - voxelSize) / chunkSize);
        int xEnd = (int)((activeStencil.XEnd + voxelSize) / chunkSize);
        int yStart = (int)((activeStencil.YStart - voxelSize) / chunkSize);
        int yEnd = (int)((activeStencil.YEnd + voxelSize) / chunkSize);

        if (xStart < 0)
        {
            xStart = 0;
        }
        if (yStart < 0)
        {
            yStart = 0;
        }
        if (xEnd >= chunkResolution)
        {
            xEnd = chunkResolution-1;
        }
        if (yEnd >= chunkResolution)
        {
            yEnd = chunkResolution-1;
        }

       
        for (int y = yEnd; y >= yStart; y--)
        {
            int i = y * chunkResolution + xEnd;
            for (int x = xEnd; x >= xStart; x--, i--)
            {
                activeStencil.SetCenter(center.x - x * chunkSize, center.y - y *chunkSize);
                chunks[i].Apply(activeStencil);
            }
        }

        //Debug.Log(centerX + ", " + centerY + "in chunk " + chunkx+", " + chunky);
    }

    private void CreateChunk(int i, int x, int y)
    {
        VoxelGrid chunk = Instantiate(voxelGridPrefab) as VoxelGrid;
        chunk.Initialize(VoxelGridResolution, chunkSize);
        chunk.transform.parent = transform;
        chunk.transform.localPosition = new Vector3(x * chunkSize - halfSize, y * chunkSize - halfSize);
        chunks[i] = chunk;
        if (x > 0) {
            chunks[i - 1].xNeighbor = chunk;
        }
        if (y > 0) {
            chunks[i - chunkResolution].yNeighbor = chunk;
            if (x > 0)
            {
                chunks[i - chunkResolution - 1].xyNeighbor = chunk;
            }
        }
    }
}
