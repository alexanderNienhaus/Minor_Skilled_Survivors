public class GridObject
{
    private GridXZ<GridObject> grid;
    private int x;
    private int z;
    private PlacedObject placedObject;
    public GridObject(GridXZ<GridObject> pGrid, int pX, int pZ)
    {
        grid = pGrid;
        x = pX;
        z = pZ;
    }

    public void SetPlacedObject(PlacedObject pPlacedObject)
    {
        placedObject = pPlacedObject;
        grid.TriggerGridObjectChanged(x, z);
    }

    public PlacedObject GetPlacedObject()
    {
        return placedObject;
    }

    public void ClearPlacedObject()
    {
        placedObject = null;
        grid.TriggerGridObjectChanged(x, z);
    }

    public bool CanBuild()
    {
        return placedObject == null;
    }

    public override string ToString()
    {
        return placedObject != null ? "X" : "0";
    }
}