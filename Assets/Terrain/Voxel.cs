using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Voxel
{

    public bool state;
    public Vector2 position;
    public float xEdgePosition, yEdgePosition;
    public Vector2 xNormal, yNormal;

    public Voxel(int x , int y , float size) {

        position.x = (x + 0.5f) * size;
        position.y = (y + 0.5f) * size;

        xEdgePosition = float.MinValue;
        yEdgePosition = float.MinValue;

    }

    public Voxel()
    {


    }

    public void BecomeDummyOf_XDir(Voxel voxel, float offset) { 
        state = voxel.state;
        position = voxel.position;
        position.x += offset;
        xEdgePosition = voxel.xEdgePosition + offset;
        yEdgePosition = voxel.yEdgePosition;
        yNormal = voxel.yNormal;    
    }

    public void BecomeDummyOf_YDir(Voxel voxel, float offset)
    {
        state = voxel.state;
        position = voxel.position;
        position.y += offset;
        xEdgePosition = voxel.xEdgePosition;
        yEdgePosition = voxel.yEdgePosition + offset;
        xNormal = voxel.xNormal;




    }
    public void BecomeDummyOf_BothDir(Voxel voxel, float offset)
    {
        state = voxel.state;
        position = voxel.position;
        position.x += offset;
        position.y += offset;
        xEdgePosition = voxel.xEdgePosition + offset;
        yEdgePosition = voxel.yEdgePosition + offset;

    }



}
