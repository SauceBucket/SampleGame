using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class VoxelStencil
{
    protected float centerX, centerY, radius;

    protected bool fillType;

    public float XStart
    {
        get
        {
            return centerX - radius;
        }
    }

    public float XEnd
    {
        get
        {
            return centerX + radius;
        }
    }

    public float YStart
    {
        get
        {
            return centerY - radius;
        }
    }

    public float YEnd
    {
        get
        {
            return centerY + radius;
        }
    }

    public virtual void Initialize(bool fillType, float radius)
    {
        this.fillType = fillType;
        this.radius = radius;
    }

    public virtual void SetCenter(float x, float y) {
        centerX = x;
        centerY = y;

    }

    public virtual void Apply(Voxel voxel) {

        Vector2 p = voxel.position;
        if (p.x >= XStart && p.x <= XEnd && p.y >= YStart && p.y <= YEnd) {
            voxel.state = fillType;
        }
    }

    public void SetHorizontalIntersection(Voxel xMin, Voxel xMax) {
        
        if (xMin.state != xMax.state) {
            FindHorizontalIntersection(xMin,xMax);
        }
        else
        {
            xMin.xEdgePosition = float.MinValue;
        }

    }
    protected virtual void FindHorizontalIntersection(Voxel xMin, Voxel xMax) {

        if (xMin.position.y < YStart || xMax.position.y > YEnd)
        {
            return;
        }
        if (xMin.state == fillType)
        {
            if (xMin.position.x <= XEnd && xMax.position.x >= XEnd)
            {
                if (xMin.xEdgePosition == float.MinValue || xMin.xEdgePosition < XEnd)
                {
                    xMin.xEdgePosition = XEnd;
                    xMin.xNormal = new Vector2(fillType ? 1f : -1f, 0f);
                }
            }
        }
        else if (xMax.state == fillType)
        {
            if (xMin.position.x <= XStart && xMax.position.x >= XStart)
            {
                if (xMin.xEdgePosition == float.MinValue || xMin.xEdgePosition > XStart)
                {
                    xMin.xEdgePosition = XStart;
                    xMin.xNormal = new Vector2(fillType ? 1f : -1f, 0f);
                }
            }
        }


    }

    public void SetVerticalIntersection(Voxel yMin, Voxel yMax)
    {
        if (yMin.state != yMax.state)
        {
            FindVerticalIntersection(yMin, yMax);
        }
        else
        {
            yMin.yEdgePosition = float.MinValue;
        }
    }

    protected virtual void FindVerticalIntersection(Voxel yMin, Voxel yMax)
    {
        if (yMin.position.x < XStart || yMin.position.x > XEnd)
        {
            return;
        }
        if (yMin.state == fillType)
        {
            if (yMin.position.y <= YEnd && yMax.position.y >= YEnd)
            {
                if (yMin.yEdgePosition == float.MinValue || yMin.yEdgePosition < YEnd)
                {
                    yMin.yEdgePosition = YEnd;
                    yMin.yNormal = new Vector2(0f, fillType ? 1f : -1f);
                }
            }
        }
        else if (yMax.state == fillType)
        {
            if (yMin.position.y <= YStart && yMax.position.y >= YStart)
            {
                if (yMin.yEdgePosition == float.MinValue || yMin.yEdgePosition > YStart)
                {
                    yMin.yEdgePosition = YStart;
                    yMin.yNormal = new Vector2(0f, fillType ? 1f : -1f);
                }
            }
        }
    }
}
public class VoxelStencilCircle: VoxelStencil 
{
    private float sqrRadius;

    public override void Initialize(bool fillType, float radius)
    {
        base.Initialize(fillType, radius);
        sqrRadius = radius * radius;
    }

    public override void Apply(Voxel voxel)
    {
       float x = voxel.position.x - centerX;
       float y = voxel.position.y - centerY;

        if (x * x + y * y <= sqrRadius){
            voxel.state = fillType;
        }

    }

    protected override void FindHorizontalIntersection(Voxel xMin, Voxel xMax)
    {

        float y = xMin.position.y - centerY;
        if (xMin.state == fillType) {
            float x = xMin.position.x - centerX;
            if (x * x + y * y <= sqrRadius)
            {
                x = centerX + Mathf.Sqrt(sqrRadius - (y*y));
                if (xMin.xEdgePosition == float.MinValue || xMin.xEdgePosition < x)
                {
                    xMin.xEdgePosition = x;
                    xMin.xNormal = ComputeNormal(x, xMin.position.y);
                }
            }
        }
        else if (xMax.state == fillType)
        {
            float x = xMax.position.x - centerX;
            if (x * x + y * y <= sqrRadius)
            {
                x = centerX - Mathf.Sqrt(sqrRadius - y * y);
                if (xMin.xEdgePosition == float.MinValue || xMin.xEdgePosition > x)
                {
                    xMin.xEdgePosition = x;
                    xMin.xNormal = ComputeNormal(x, xMin.position.y);
                }
            }
        }



    }

    protected override void FindVerticalIntersection(Voxel yMin, Voxel yMax)
    {
        float x2 = yMin.position.x - centerX;
        x2 *= x2;
        if (yMin.state == fillType)
        {
            float y = yMin.position.y - centerY;
            if (y * y + x2 <= sqrRadius)
            {
                y = centerY + Mathf.Sqrt(sqrRadius - x2);
                if (yMin.yEdgePosition == float.MinValue || yMin.yEdgePosition < y)
                {
                    yMin.yEdgePosition = y;
                    yMin.yNormal = ComputeNormal(yMin.position.x, y);
                }
            }
        }
        else if (yMax.state == fillType)
        {
            float y = yMax.position.y - centerY;
            if (y * y + x2 <= sqrRadius)
            {
                y = centerY - Mathf.Sqrt(sqrRadius - x2);
                if (yMin.yEdgePosition == float.MinValue || yMin.yEdgePosition > y)
                {
                    yMin.yEdgePosition = y;
                    yMin.yNormal = ComputeNormal(yMin.position.x, y);
                }
            }
        }
    }

    private Vector2 ComputeNormal(float x, float y) {
        if (fillType) return new Vector2(x - centerX, y - centerY).normalized;
        else return new Vector2(x - centerX, y - centerY).normalized;
    }

}
