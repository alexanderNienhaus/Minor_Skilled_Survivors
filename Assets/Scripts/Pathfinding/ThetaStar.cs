using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

[BurstCompile]
public struct ThetaStar
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private int2 gridSize;
    private float3 gridOriginPos;
    private float gridCellSize;
    private int gridWidth;
    private int endNodeIndex;
    private int2 endNodePos;
    private int startNodeIndex;

    public void Initialize(int2 pGridSize, float3 pGridOriginPos, float pGridCellSize, int pGridWidth, int pEndNodeIndex, int2 pEndNodePos, int pStartNodeIndex)
    {
        gridSize = pGridSize;
        gridOriginPos = pGridOriginPos;
        gridCellSize = pGridCellSize;
        gridWidth = pGridWidth;
        endNodeIndex = pEndNodeIndex;
        endNodePos = pEndNodePos;
        startNodeIndex = pStartNodeIndex;
    }

    [BurstCompile]
    public void FindPath(ref NativeArray<PathNode> tmpPathNodeArray)
    {
        //Get start pos
        PathNode startNode = tmpPathNodeArray[startNodeIndex];
        startNode.gCost = 0;
        startNode.CalculateFCost();
        tmpPathNodeArray[startNodeIndex] = startNode;

        for (int i = 0; i < tmpPathNodeArray.Length; i++)
        {
            PathNode pathNode = tmpPathNodeArray[i];
            pathNode.hCost = CalculateDistanceCost(new int2(pathNode.x, pathNode.z), endNodePos);
            pathNode.cameFromNodeIndex = -1;
            tmpPathNodeArray[i] = pathNode;
        }

        NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(8, Allocator.Temp);
        neighbourOffsetArray[0] = new int2(-1, 0); //Left
        neighbourOffsetArray[1] = new int2(1, 0); //Right
        neighbourOffsetArray[2] = new int2(0, 1); //Up
        neighbourOffsetArray[3] = new int2(0, -1); //Down
        neighbourOffsetArray[4] = new int2(-1, -1); //Left Down
        neighbourOffsetArray[5] = new int2(-1, 1); //Left Up
        neighbourOffsetArray[6] = new int2(1, 1); //Right Down
        neighbourOffsetArray[7] = new int2(1, 1); //Right Up
        NativeList<int> openList = new NativeList<int>(Allocator.Temp);
        NativeList<int> closedList = new NativeList<int>(Allocator.Temp);
        openList.Add(startNodeIndex);

        while (openList.Length > 0)
        {
            int currentNodeIndex = GetLowestCostFNodeIndex(openList, tmpPathNodeArray);
            PathNode currentNode = tmpPathNodeArray[currentNodeIndex];

            if (currentNodeIndex == endNodeIndex)
                break; //Reached Destination

            openList = RemoveNodeFromList(openList, currentNodeIndex);

            closedList.Add(currentNodeIndex);

            for (int i = 0; i < neighbourOffsetArray.Length; i++)
            {
                int2 neighbourOffset = neighbourOffsetArray[i];
                int2 neighbourPos = new int2(currentNode.x + neighbourOffset.x, currentNode.z + neighbourOffset.y);

                if (!IsPositionInsideGrid(neighbourPos, gridSize))
                    continue; //Neighbor outside grid

                int neighbourNodeIndex = CalculateIndex(neighbourPos.x, neighbourPos.y);

                if (closedList.Contains(neighbourNodeIndex))
                    continue; //Already searched neighbor

                PathNode neighbourNode = tmpPathNodeArray[neighbourNodeIndex];
                if (!HasLineOfSight(currentNode, neighbourNode, tmpPathNodeArray))
                    continue;

                UpdateVertix(ref tmpPathNodeArray, ref openList, currentNode, neighbourPos, neighbourNodeIndex, neighbourNode);
            }
        }

        //neighbourOffsetArray.Dispose();
        //openList.Dispose();
        //closedList.Dispose();
        //tmpPathNodeArray.Dispose();
    }

    private static NativeList<int> RemoveNodeFromList(NativeList<int> openList, int currentNodeIndex)
    {
        for (int i = 0; i < openList.Length; i++)
        {
            if (openList[i] != currentNodeIndex)
                continue;

            openList.RemoveAtSwapBack(i);
            break;
        }

        return openList;
    }

    [BurstCompile]
    private void UpdateVertix(ref NativeArray<PathNode> tmpPathNodeArray, ref NativeList<int> openList, PathNode currentNode, int2 neighbourPos, int neighbourNodeIndex, PathNode neighbourNode)
    {
        PathNode fromNode;
        if (currentNode.cameFromNodeIndex != -1 && HasLineOfSight(tmpPathNodeArray[currentNode.cameFromNodeIndex], neighbourNode, tmpPathNodeArray))
        {
            fromNode = tmpPathNodeArray[currentNode.cameFromNodeIndex];
        }
        else
        {
            fromNode = currentNode;
        }

        int2 fromNodePos = new int2(fromNode.x, fromNode.z);
        int tentativeGCost = fromNode.gCost + CalculateDistanceCost(fromNodePos, neighbourPos);

        if (tentativeGCost >= neighbourNode.gCost)
            return;

        int fromNodeIndex = CalculateIndex(fromNodePos.x, fromNodePos.y);
        neighbourNode.cameFromNodeIndex = fromNodeIndex;
        neighbourNode.gCost = tentativeGCost;
        neighbourNode.CalculateFCost();
        tmpPathNodeArray[neighbourNodeIndex] = neighbourNode;

        if (openList.Contains(neighbourNode.index))
            return;
        
        openList.Add(neighbourNode.index);
    }

    [BurstCompile]
    private bool HasLineOfSight(PathNode node1, PathNode node2, NativeArray<PathNode> tmpPathNodeArray)
    {
        int sx = node1.x;
        int sy = node1.z;

        int x0 = sx;
        int y0 = sy;
        int x1 = node2.x;
        int y1 = node2.z;

        int dy = y1 - y0;
        int dx = x1 - x0;

        int f = 0;

        if (dy < 0)
        {
            dy = -dy;
            sy = -1;
        }
        else
        {
            sy = 1;
        }

        if (dx < 0)
        {
            dx = -dx;
            sx = -1;
        }
        else
        {
            sx = 1;
        }

        if (dx >= dy)
        {
            while (x0 != x1)
            {
                f += dy;
                if (f >= dx)
                {
                    if (!tmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0 + ((sy - 1) / 2))].isWalkable)
                    {
                        return false;
                    }
                    y0 += sy;
                    f -= dx;
                }
                if (f != 0 && !tmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0 + ((sy - 1) / 2))].isWalkable)
                {
                    return false;
                }
                if (dy == 0 && !tmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0)].isWalkable
                            && !tmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0 - 1)].isWalkable)
                {
                    return false;
                }
                x0 += sx;
            }
        }
        else
        {
            while (y0 != y1)
            {
                f += dx;
                if (f >= dy)
                {
                    if (!tmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0 + ((sy - 1) / 2))].isWalkable)
                    {
                        return false;
                    }
                    x0 += sx;
                    f -= dy;
                }
                if (f != 0 && !tmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0 + ((sy - 1) / 2))].isWalkable)
                {
                    return false;
                }
                if (dx == 0 && !tmpPathNodeArray[CalculateIndex(x0, y0 + ((sy - 1) / 2))].isWalkable
                            && !tmpPathNodeArray[CalculateIndex(x0 - 1, y0 + ((sy - 1) / 2))].isWalkable)
                {
                    return false;
                }
                y0 += sy;
            }
        }
        return true;
    }

    [BurstCompile]
    private void GetXZ(float3 pWorldPos, out int pX, out int pZ)
    {
        pX = (int)math.floor((pWorldPos - gridOriginPos).x / gridCellSize);
        pZ = (int)math.floor((pWorldPos - gridOriginPos).z / gridCellSize);
    }

    [BurstCompile]
    private float3 GetWorldPositionXZ(int pX, int pZ)
    {
        return new float3(pX, 0, pZ) * gridCellSize + gridOriginPos;
    }

    [BurstCompile]
    private int CalculateIndex(int x, int y)
    {
        return x + y * gridWidth;
    }

    [BurstCompile]
    private int CalculateDistanceCost(int2 aPosition, int2 bPosition)
    {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        int remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    [BurstCompile]
    private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNode> pathNodeArray)
    {
        PathNode lowestCostPathNode = pathNodeArray[openList[0]];
        for (int i = 1; i < openList.Length; i++)
        {
            PathNode testPathNode = pathNodeArray[openList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost)
            {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }

    [BurstCompile]
    private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < gridSize.x &&
            gridPosition.y < gridSize.y;
    }

}
