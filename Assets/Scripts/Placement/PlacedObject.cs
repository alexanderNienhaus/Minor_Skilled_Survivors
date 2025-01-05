using System.Collections.Generic;
using UnityEngine;

public class PlacedObject
{
    private int id;
    private PlacableObjectTypeSO placedObjectTypeSO;
    private Vector2Int origin;
    private PlacableObjectTypeSO.Dir direction;

    public PlacedObject(PlacableObjectTypeSO pPlacedObjectTypeSO, Vector2Int pOrigin, PlacableObjectTypeSO.Dir pDirection)
    {
        placedObjectTypeSO = pPlacedObjectTypeSO;
        origin = pOrigin;
        direction = pDirection;
    }

    public List<Vector2Int> GetGridPosList()
    {
        return placedObjectTypeSO.GetGridPosList(origin, direction);
    }

    public PlacableObjectTypeSO GetPlacedObjectTypeSO()
    {
        return placedObjectTypeSO;
    }

    public int GetId()
    {
        return id;
    }

    public void SetId(int pId)
    {
        id = pId;
    }
}
