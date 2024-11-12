using System.Collections.Generic;
using UnityEngine;

public class PlacedObject
{
    private int id;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private Vector2Int origin;
    private PlacedObjectTypeSO.Dir direction;

    public PlacedObject(PlacedObjectTypeSO pPlacedObjectTypeSO, Vector2Int pOrigin, PlacedObjectTypeSO.Dir pDirection)
    {
        placedObjectTypeSO = pPlacedObjectTypeSO;
        origin = pOrigin;
        direction = pDirection;
    }

    public List<Vector2Int> GetGridPosList()
    {
        return placedObjectTypeSO.GetGridPosList(origin, direction);
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSO()
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
