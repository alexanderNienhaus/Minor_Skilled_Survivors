public class GridNode {

    private GridXZ<GridNode> grid;
    private int x;
    private int z;

    private bool isWalkable;

    public GridNode(GridXZ<GridNode> pGrid, int pX, int pZ) {
        grid = pGrid;
        x = pX;
        z = pZ;
        isWalkable = true;
    }

    public bool GetIsWalkable() {
        return isWalkable;
    }

    public void SetIsWalkable(bool pIsWalkable) {
        isWalkable = pIsWalkable;
        grid.TriggerGridObjectChanged(x, z);
    }

    public override string ToString()
    {
        return isWalkable ? x + ";" + z : "X";
    }
}
