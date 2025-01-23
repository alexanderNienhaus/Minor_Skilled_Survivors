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
    public void FindPath(ref NativeArray<PathNode> pTmpPathNodeArray)
    {
        //Get start pos
        PathNode startNode = pTmpPathNodeArray[startNodeIndex];
        startNode.gCost = 0;
        startNode.CalculateFCost();
        pTmpPathNodeArray[startNodeIndex] = startNode;

        for (int i = 0; i < pTmpPathNodeArray.Length; i++)
        {
            PathNode pathNode = pTmpPathNodeArray[i];
            pathNode.hCost = CalculateDistanceCost(new (pathNode.x, pathNode.z), endNodePos);
            pathNode.cameFromNodeIndex = -1;
            pTmpPathNodeArray[i] = pathNode;
        }

        NativeArray<int2> neighbourOffsetArray = new (8, Allocator.Temp);
        neighbourOffsetArray[0] = new (-1, 0); //Left
        neighbourOffsetArray[1] = new (1, 0); //Right
        neighbourOffsetArray[2] = new (0, 1); //Up
        neighbourOffsetArray[3] = new (0, -1); //Down
        neighbourOffsetArray[4] = new (-1, -1); //Left Down
        neighbourOffsetArray[5] = new (-1, 1); //Left Up
        neighbourOffsetArray[6] = new (1, 1); //Right Down
        neighbourOffsetArray[7] = new (1, 1); //Right Up
        NativeList<int> openList = new (Allocator.Temp);
        NativeList<int> closedList = new (Allocator.Temp);
        openList.Add(startNodeIndex);

        while (openList.Length > 0)
        {
            int currentNodeIndex = GetLowestCostFNodeIndex(openList, pTmpPathNodeArray);
            PathNode currentNode = pTmpPathNodeArray[currentNodeIndex];

            if (currentNodeIndex == endNodeIndex)
                break; //Reached Destination

            openList = RemoveNodeFromList(openList, currentNodeIndex);

            closedList.Add(currentNodeIndex);

            for (int i = 0; i < neighbourOffsetArray.Length; i++)
            {
                int2 neighbourOffset = neighbourOffsetArray[i];
                int2 neighbourPos = new (currentNode.x + neighbourOffset.x, currentNode.z + neighbourOffset.y);

                if (!IsPositionInsideGrid(neighbourPos, gridSize))
                    continue; //Neighbor outside grid

                int neighbourNodeIndex = CalculateIndex(neighbourPos.x, neighbourPos.y);

                if (closedList.Contains(neighbourNodeIndex))
                    continue; //Already searched neighbor

                PathNode neighbourNode = pTmpPathNodeArray[neighbourNodeIndex];
                if (!HasLineOfSight(currentNode, neighbourNode, pTmpPathNodeArray))
                    continue;

                UpdateVertix(ref pTmpPathNodeArray, ref openList, currentNode, neighbourPos, neighbourNodeIndex, neighbourNode);
            }
        }

        neighbourOffsetArray.Dispose();
        openList.Dispose();
        closedList.Dispose();
        //tmpPathNodeArray.Dispose();
    }

    private static NativeList<int> RemoveNodeFromList(NativeList<int> pOpenList, int pCurrentNodeIndex)
    {
        for (int i = 0; i < pOpenList.Length; i++)
        {
            if (pOpenList[i] != pCurrentNodeIndex)
                continue;

            pOpenList.RemoveAtSwapBack(i);
            break;
        }

        return pOpenList;
    }

    [BurstCompile]
    private void UpdateVertix(ref NativeArray<PathNode> pTmpPathNodeArray, ref NativeList<int> pOpenList, PathNode pCurrentNode, int2 pNeighbourPos, int pNeighbourNodeIndex,
        PathNode pNeighbourNode)
    {
        PathNode fromNode;
        if (pCurrentNode.cameFromNodeIndex != -1 && HasLineOfSight(pTmpPathNodeArray[pCurrentNode.cameFromNodeIndex], pNeighbourNode, pTmpPathNodeArray))
        {
            fromNode = pTmpPathNodeArray[pCurrentNode.cameFromNodeIndex];
        }
        else
        {
            fromNode = pCurrentNode;
        }

        int2 fromNodePos = new (fromNode.x, fromNode.z);
        int tentativeGCost = fromNode.gCost + CalculateDistanceCost(fromNodePos, pNeighbourPos);

        if (tentativeGCost >= pNeighbourNode.gCost)
            return;

        int fromNodeIndex = CalculateIndex(fromNodePos.x, fromNodePos.y);
        pNeighbourNode.cameFromNodeIndex = fromNodeIndex;
        pNeighbourNode.gCost = tentativeGCost;
        pNeighbourNode.CalculateFCost();
        pTmpPathNodeArray[pNeighbourNodeIndex] = pNeighbourNode;

        if (pOpenList.Contains(pNeighbourNode.index))
            return;
        
        pOpenList.Add(pNeighbourNode.index);
    }

    [BurstCompile]
    private bool HasLineOfSight(PathNode pNode1, PathNode pNode2, NativeArray<PathNode> pTmpPathNodeArray)
    {
        int sx = pNode1.x;
        int sy = pNode1.z;

        int x0 = sx;
        int y0 = sy;
        int x1 = pNode2.x;
        int y1 = pNode2.z;

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
                    if (!pTmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0 + ((sy - 1) / 2))].isWalkable)
                        return false;

                    y0 += sy;
                    f -= dx;
                }
                if (f != 0 && !pTmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0 + ((sy - 1) / 2))].isWalkable)
                    return false;

                if (dy == 0 && !pTmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0)].isWalkable  && !pTmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0 - 1)].isWalkable)
                    return false;

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
                    if (!pTmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0 + ((sy - 1) / 2))].isWalkable)
                        return false;

                    x0 += sx;
                    f -= dy;
                }
                if (f != 0 && !pTmpPathNodeArray[CalculateIndex(x0 + ((sx - 1) / 2), y0 + ((sy - 1) / 2))].isWalkable)
                    return false;

                if (dx == 0 && !pTmpPathNodeArray[CalculateIndex(x0, y0 + ((sy - 1) / 2))].isWalkable && !pTmpPathNodeArray[CalculateIndex(x0 - 1, y0 + ((sy - 1) / 2))].isWalkable)
                    return false;

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
    private int CalculateIndex(int pX, int pY)
    {
        return pX + pY * gridWidth;
    }

    [BurstCompile]
    private int CalculateDistanceCost(int2 pAPosition, int2 pBPosition)
    {
        int xDistance = math.abs(pAPosition.x - pBPosition.x);
        int yDistance = math.abs(pAPosition.y - pBPosition.y);
        int remaining = math.abs(xDistance - yDistance);
        return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    [BurstCompile]
    private int GetLowestCostFNodeIndex(NativeList<int> pOpenList, NativeArray<PathNode> pPathNodeArray)
    {
        PathNode lowestCostPathNode = pPathNodeArray[pOpenList[0]];
        for (int i = 1; i < pOpenList.Length; i++)
        {
            PathNode testPathNode = pPathNodeArray[pOpenList[i]];
            if (testPathNode.fCost < lowestCostPathNode.fCost)
            {
                lowestCostPathNode = testPathNode;
            }
        }
        return lowestCostPathNode.index;
    }

    [BurstCompile]
    private bool IsPositionInsideGrid(int2 pGridPosition, int2 pGridSize)
    {
        return
            pGridPosition.x >= 0 &&
            pGridPosition.y >= 0 &&
            pGridPosition.x < pGridSize.x &&
            pGridPosition.y < pGridSize.y;
    }

}
