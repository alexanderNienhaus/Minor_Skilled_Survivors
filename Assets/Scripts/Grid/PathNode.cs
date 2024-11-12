using Unity.Burst;

[BurstCompile]
public struct PathNode
{
    public int x;
    public int z;

    public int index;

    public int gCost;
    public int hCost;
    public int fCost;

    public bool isWalkable;

    public int cameFromNodeIndex;

    [BurstCompile]
    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }

    [BurstCompile]
    public void SetIsWalkable(bool pIsWalkable)
    {
        isWalkable = pIsWalkable;
    }
}
