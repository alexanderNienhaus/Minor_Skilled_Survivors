using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Transforms;

[WithAll(typeof(SelectedUnitTag))]
[BurstCompile]
public partial struct ThetaStarFindPathJob : IJobEntity
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    [NativeDisableContainerSafetyRestriction] public BufferLookup<PathPositions> pathPositions;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<PathNode> pathNodeArray;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> unitEndPositionOffsets;
    public int2 gridSize;
    public float3 mouseEndPos;
    public float3 gridOriginPos;
    public float gridCellSize;

    private float3 cellMiddleOffset;
    private int gridWidth;

    [BurstCompile]
    public void Execute(Entity pEntity, in LocalTransform pLocalTransform, [EntityIndexInQuery] int entityInQueryIndex)
    {
        pathPositions[pEntity].Clear();

        cellMiddleOffset = new float3(1, 0, 1) * gridCellSize * 0.5f;
        gridWidth = gridSize.x;

        NativeArray<PathNode> tmpPathNodeArray = new NativeArray<PathNode>(pathNodeArray, Allocator.Temp);

        //Get end pos
        float3 preciseEndPos = mouseEndPos + (unitEndPositionOffsets.Length > entityInQueryIndex ? unitEndPositionOffsets[entityInQueryIndex] : float3.zero);
        int2 endNodePos = GetNearestCornerXZ(preciseEndPos);
        ValidateGridPosition(gridSize[1], ref endNodePos.x, ref endNodePos.y);
        int endNodeIndex = CalculateIndex(endNodePos.x, endNodePos.y);
        bool endNodeChanged = false;
        if (!tmpPathNodeArray[endNodeIndex].isWalkable)
        {
            endNodeChanged = true;
            endNodeIndex = FindEndCandidate(endNodePos, preciseEndPos, tmpPathNodeArray, gridSize);
        }

        //Get start pos
        int2 startNodePos = GetNearestCornerXZ(pLocalTransform.Position); // + new float3(0.25f, 0, 0.25f) 
        ValidateGridPosition(gridSize[1], ref startNodePos.x, ref startNodePos.y);
        int startNodeIndex = CalculateIndex(startNodePos.x, startNodePos.y);
        PathNode startNode = tmpPathNodeArray[startNodeIndex];
        startNode.gCost = 0;
        startNode.CalculateFCost();
        tmpPathNodeArray[startNodeIndex] = startNode;

        if (endNodeIndex == startNodeIndex)
        {
            //Path of length 0, movement only inside start cell
            pathPositions[pEntity].Add(new PathPositions { pos = preciseEndPos });
            pathPositions[pEntity].Add(new PathPositions { pos = pLocalTransform.Position });
            return;
        }

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
            {
                //Reached Destination
                break;
            }

            //Remove current node from open list
            for (int i = 0; i < openList.Length; i++)
            {
                if (openList[i] == currentNodeIndex)
                {
                    openList.RemoveAtSwapBack(i);
                    break;
                }
            }

            closedList.Add(currentNodeIndex);

            for (int i = 0; i < neighbourOffsetArray.Length; i++)
            {
                int2 neighbourOffset = neighbourOffsetArray[i];
                int2 neighbourPos = new int2(currentNode.x + neighbourOffset.x, currentNode.z + neighbourOffset.y);

                if (!IsPositionInsideGrid(neighbourPos, gridSize))
                {
                    continue; //Neighbor outside grid
                }

                int neighbourNodeIndex = CalculateIndex(neighbourPos.x, neighbourPos.y);

                if (closedList.Contains(neighbourNodeIndex))
                {
                    continue; //Already searched neighbor
                }

                PathNode neighbourNode = tmpPathNodeArray[neighbourNodeIndex];
                if (!HasLineOfSight(currentNode, neighbourNode, tmpPathNodeArray))
                {
                    continue; //No LOS
                }

                UpdateVertix(ref tmpPathNodeArray, ref openList, currentNode, neighbourPos, neighbourNodeIndex, neighbourNode);
            }
        }

        PathNode endNode = tmpPathNodeArray[endNodeIndex];
        if (endNode.cameFromNodeIndex == -1)
        {
            //Didnt find a path
        }
        else
        {
            if (!endNodeChanged)
            {
                pathPositions[pEntity].Add(new PathPositions {
                    pos = preciseEndPos
                });
            }
            else
            {
                pathPositions[pEntity].Add(new PathPositions {
                    pos = GetWorldPositionXZ(endNode.x, endNode.z) + cellMiddleOffset
                });
            }
            CalculatePath(tmpPathNodeArray, endNode, pathPositions[pEntity]);
        }

        neighbourOffsetArray.Dispose();
        openList.Dispose();
        closedList.Dispose();
        tmpPathNodeArray.Dispose();
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
        if (tentativeGCost < neighbourNode.gCost)
        {
            int fromNodeIndex = CalculateIndex(fromNodePos.x, fromNodePos.y);
            neighbourNode.cameFromNodeIndex = fromNodeIndex;
            neighbourNode.gCost = tentativeGCost;
            neighbourNode.CalculateFCost();           

            tmpPathNodeArray[neighbourNodeIndex] = neighbourNode;
            if (!openList.Contains(neighbourNode.index))
            {
                openList.Add(neighbourNode.index);
            }
        }
    }

    [BurstCompile]
    private void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, DynamicBuffer<PathPositions> pathPositionBuffer)
    {
        PathNode currentNode = endNode;
        while (currentNode.cameFromNodeIndex != -1)
        {
            PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
            pathPositionBuffer.Add(new PathPositions
            {
                pos = GetWorldPositionXZ(cameFromNode.x, cameFromNode.z)
            });
            currentNode = cameFromNode;
        }
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

        if (dx >= dy) { 
            while (x0 != x1) {
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
    private int FindEndCandidate(int2 centerEndNode, float3 preciseEndPos, NativeArray<PathNode> tmpPathNodeArray, int2 gridSize)
    {
        NativeArray<int2> movementArray = new NativeArray<int2>(5, Allocator.Temp);
        movementArray[0] = new int2(-1, 1); //Left Up
        movementArray[1] = new int2(0, -1); //Down
        movementArray[2] = new int2(1, 0); //Right
        movementArray[3] = new int2(0, 1); //Up
        movementArray[4] = new int2(-1, 0); //Left

        int maxDistance = 100;
        for (int distance = 1; distance < maxDistance; distance++)
        {
            int numberOfCellsInDistance = 4 + 4 * (distance * 2 - 1);
            NativeList<int> freeCells = new NativeList<int>(numberOfCellsInDistance, Allocator.Temp);

            int2 neighbourPos = centerEndNode + new int2(distance * movementArray[0].x, distance * movementArray[0].y);

            for (int side = 1; side <= 4; side++)
            {
                for (int sideSteps = 1; sideSteps <= distance * 2; sideSteps++)
                {
                    neighbourPos += new int2(movementArray[side].x, movementArray[side].y);
                    int neighbourNodeIndex = CalculateIndex(neighbourPos.x, neighbourPos.y);
                    if (!IsPositionInsideGrid(neighbourPos, gridSize))
                    {
                        continue;
                    }
                    PathNode neighbourNode = tmpPathNodeArray[neighbourNodeIndex];
                    if (neighbourNode.isWalkable)
                    {
                        freeCells.Add(neighbourNodeIndex);
                    }
                }
            }

            if (freeCells.Length > 0)
            {
                float shortestDistance = float.MaxValue;
                int shortestDistancePathNodeIndex = -1;
                for (int i = 0; i < freeCells.Length; i++)
                {
                    PathNode endNodeCandidate = tmpPathNodeArray[freeCells[i]];
                    float3 worldPos = GetWorldPositionXZ(endNodeCandidate.x, endNodeCandidate.z) + cellMiddleOffset;
                    float currentDistance = math.length(preciseEndPos - worldPos);

                    if (currentDistance < shortestDistance)
                    {
                        shortestDistance = currentDistance;
                        shortestDistancePathNodeIndex = freeCells[i];
                    }
                }
                freeCells.Dispose();
                return shortestDistancePathNodeIndex;
            }
            freeCells.Dispose();
        }
        return -1;
    }

    [BurstCompile]
    private void ValidateGridPosition(int pLength, ref int x, ref int y)
    {
        x = math.clamp(x, 0, gridWidth - 1);
        y = math.clamp(y, 0, pLength - 1);
    }

    [BurstCompile]
    private int2 GetNearestCornerXZ(float3 pWorldPos)
    {
        int2 cellPosBotLeft = new int2((int)math.floor((pWorldPos - gridOriginPos).x / gridCellSize), (int)math.floor((pWorldPos - gridOriginPos).z / gridCellSize));
        float3 worldPosBotLeft = GetWorldPositionXZ(cellPosBotLeft.x, cellPosBotLeft.y);
        float3 woldPosCenter = worldPosBotLeft + new float3(gridCellSize / 2, 0, gridCellSize / 2);

        if (pWorldPos.x > woldPosCenter.x && pWorldPos.y > woldPosCenter.y)
        {
            float3 worldPosTopRight = worldPosBotLeft + new float3(gridCellSize, 0, gridCellSize);
            return new int2((int)math.floor((worldPosTopRight - gridOriginPos).x / gridCellSize), (int)math.floor((worldPosTopRight - gridOriginPos).z / gridCellSize));
        }
        else if (pWorldPos.x <= woldPosCenter.x && pWorldPos.y > woldPosCenter.y)
        {
            float3 worldPosTopLeft = worldPosBotLeft + new float3(0, 0, gridCellSize);
            return new int2((int)math.floor((worldPosTopLeft - gridOriginPos).x / gridCellSize), (int)math.floor((worldPosTopLeft - gridOriginPos).z / gridCellSize));
        }
        else if (pWorldPos.x > woldPosCenter.x && pWorldPos.y <= woldPosCenter.y)
        {
            float3 worldPosBotRight = worldPosBotLeft + new float3(gridCellSize, 0, 0);
            return new int2((int)math.floor((worldPosBotRight - gridOriginPos).x / gridCellSize), (int)math.floor((worldPosBotRight - gridOriginPos).z / gridCellSize));
        }
        else
        {
            return cellPosBotLeft;
        }
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
    private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < gridSize.x &&
            gridPosition.y < gridSize.y;
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
    private float CalculateDistanceCostStraight(int2 aPosition, int2 bPosition)
    {
        return math.length(aPosition - bPosition);
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
}