using UnityEngine;
using System;

public class GridXZ<TGridObject>
{
    public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs
    {
        public int x;
        public int z;
    }

    private int width;
    private int length;
    private float cellSize;
    private TGridObject[,] gridArray;
    private Vector3 originPos;

    public GridXZ(int pWidth, int pLength, float pCellSize, Vector3 pOriginPos, bool pShowDebug, int pDebugTextSize,
        Func<GridXZ<TGridObject>, int, int, TGridObject> pCreateGridObject)
    {
        width = pWidth;
        length = pLength;
        cellSize = pCellSize;
        originPos = pOriginPos;

        gridArray = new TGridObject[width, length];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] = pCreateGridObject(this, x, z);
            }
        }

        if (pShowDebug)
        {
            TextMesh[,] debugTextArray = new TextMesh[width, length];

            int steps = 2;
            //bool middleOfCell = false;
            for (int x = 0; x < gridArray.GetLength(0); x += steps)
            {
                for (int z = 0; z < gridArray.GetLength(1); z += steps)
                {
                    //debugTextArray[x, z] = UtilsClass.CreateWorldText(gridArray[x, z]?.ToString(), null, GetWorldPositionXZ(x, z) + (middleOfCell ? new Vector3(cellSize, 0, cellSize) * 0.5f : Vector3.zero), pDebugTextSize, Color.white, TextAnchor.MiddleCenter);
                    Debug.DrawLine(GetWorldPositionXZ(x, z), GetWorldPositionXZ(x, z + (int)cellSize), Color.white, 1000f);
                    Debug.DrawLine(GetWorldPositionXZ(x, z), GetWorldPositionXZ(x + (int)cellSize, z), Color.white, 1000f);
                }
            }
            Debug.DrawLine(GetWorldPositionXZ(0, length), GetWorldPositionXZ(width, length), Color.white, 1000f);
            Debug.DrawLine(GetWorldPositionXZ(width, 0), GetWorldPositionXZ(width, length), Color.white, 1000f);

            OnGridValueChanged += (object sender, OnGridValueChangedEventArgs eventArgs) =>
            {
                //debugTextArray[eventArgs.x, eventArgs.z].text = gridArray[eventArgs.x, eventArgs.z]?.ToString();
                /*
                UtilsClass.CreateWorldTextPopup(null, gridArray[eventArgs.x, eventArgs.z]?.ToString(), GetWorldPositionXZ(eventArgs.x, eventArgs.z)
                    + (middleOfCell ? new Vector3(cellSize, 0, cellSize) * 0.5f : Vector3.zero), pDebugTextSize, Color.white, GetWorldPositionXZ(eventArgs.x, eventArgs.z)
                    + (middleOfCell ? new Vector3(cellSize, 0, cellSize) * 0.5f : Vector3.zero), 1000);
                */
                //UtilsClass.CreateWorldText(gridArray[eventArgs.x, eventArgs.z]?.ToString(), null, GetWorldPositionXZ(eventArgs.x, eventArgs.z) + new Vector3(cellSize, cellSize) * 0.5f, pDebugTextSize, Color.white, TextAnchor.MiddleCenter);
            };
        }
    }

    public int GetWidth()
    {
        return width;
    }

    public int GetLength()
    {
        return length;
    }

    public Vector3 GetWorldPositionXZ(int pX, int pZ)
    {
        return new Vector3(pX, 0, pZ) * cellSize + originPos;
    }

    public void GetXZ(Vector3 pWorldPos, out int pX, out int pZ)
    {
        pX = Mathf.FloorToInt((pWorldPos - originPos).x / cellSize);
        pZ = Mathf.FloorToInt((pWorldPos - originPos).z / cellSize);
    }

    public void SetGridObject(int pX, int pZ, TGridObject pGridObject)
    {
        if (pX >= 0 && pZ >= 0 && pX < width && pZ < length)
        {
            gridArray[pX, pZ] = pGridObject;
            if (OnGridValueChanged != null)
            {
                OnGridValueChanged(this, new OnGridValueChangedEventArgs() { x = pX, z = pZ });
            }
        }
    }

    public void TriggerGridObjectChanged(int pX, int pZ)
    {
        if (OnGridValueChanged != null)
        {
            OnGridValueChanged(this, new OnGridValueChangedEventArgs() { x = pX, z = pZ });
        }
    }

    public void SetGridObject(Vector3 pWorldPos, TGridObject pGridObject)
    {
        int x, z;
        GetXZ(pWorldPos, out x, out z);
        SetGridObject(x, z, pGridObject);
    }

    public TGridObject GetGridObject(int pX, int pZ)
    {
        if (pX >= 0 && pZ >= 0 && pX < width && pZ < length)
        {
            return gridArray[pX, pZ];
        }
        else
        {
            return default(TGridObject);
        }
    }

    public TGridObject GetGridObject(Vector3 pWorldPos)
    {
        int x, z;
        GetXZ(pWorldPos, out x, out z);
        return GetGridObject(x, z);
    }

    public float GetCellSize()
    {
        return cellSize;
    }

    public Vector3 GetOriginPos()
    {
        return originPos;
    }
}
