using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Transforms;

[WithAll(typeof(SelectedUnitTag))]
[BurstCompile]
public partial struct FindSoldierPathJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction] public BufferLookup<PathPositions> pathPositions;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<PathNode> pathNodeArray;
    //[DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float3> unitEndPositionOffsets;
    public quaternion targetRotation;
    public int2 gridSize;
    public float3 endPos;
    public float3 gridOriginPos;
    public float gridCellSize;
    public ThetaStar thetaStar;

    private float3 cellMiddleOffset;
    private int gridWidth;

    [BurstCompile]
    public void Execute(Entity pEntity, ref PathFollow pathFollow, in LocalTransform pLocalTransform, in FormationPosition formationPosition)
        //, [EntityIndexInQuery] int entityInQueryIndex)
    {
        pathPositions[pEntity].Clear();

        cellMiddleOffset = new float3(1, 0, 1) * gridCellSize * 0.5f;
        gridWidth = gridSize.x;

        int2 startNodePos = GetNearestCornerXZ(pLocalTransform.Position);
        ValidateGridPosition(gridSize[1], ref startNodePos.x, ref startNodePos.y);
        int startNodeIndex = CalculateIndex(startNodePos.x, startNodePos.y);

        //Get end pos
        //float3 preciseEndPos = endPos + (unitEndPositionOffsets.Length > entityInQueryIndex ? unitEndPositionOffsets[entityInQueryIndex] : float3.zero);
        float3 preciseEndPos = formationPosition.position;
        int2 endNodePos = GetNearestCornerXZ(preciseEndPos);
        ValidateGridPosition(gridSize[1], ref endNodePos.x, ref endNodePos.y);
        int endNodeIndex = CalculateIndex(endNodePos.x, endNodePos.y);
        bool endNodeChanged = false;

        NativeArray<PathNode> tmpPathNodeArray = new NativeArray<PathNode>(pathNodeArray, Allocator.Temp);
        if (!tmpPathNodeArray[endNodeIndex].isWalkable)
        {
            endNodeChanged = true;
            endNodeIndex = FindEndCandidate(endNodePos, preciseEndPos, tmpPathNodeArray, gridSize);
        }

        if (endNodeIndex == startNodeIndex)
        {
            //Path of length 0, movement only inside start cell
            pathPositions[pEntity].Add(new PathPositions { pos = preciseEndPos });
            pathPositions[pEntity].Add(new PathPositions { pos = pLocalTransform.Position });
            return;
        }

        thetaStar.Initialize(gridSize, gridOriginPos, gridCellSize, gridWidth, endNodeIndex, endNodePos, startNodeIndex);
        thetaStar.FindPath(ref tmpPathNodeArray);

        PathNode endNode = tmpPathNodeArray[endNodeIndex];
        if (endNode.cameFromNodeIndex == -1)
        {
            //Didnt find a path
        }
        else
        {
            if (!endNodeChanged)
            {
                pathPositions[pEntity].Add(new PathPositions
                {
                    pos = preciseEndPos
                });
            }
            else
            {
                pathPositions[pEntity].Add(new PathPositions
                {
                    pos = GetWorldPositionXZ(endNode.x, endNode.z) + cellMiddleOffset
                });
            }
            CalculatePath(tmpPathNodeArray, endNode, pathPositions[pEntity]);
            pathFollow.targetRotation = targetRotation;
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
    private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < gridSize.x &&
            gridPosition.y < gridSize.y;
    }

    [BurstCompile]
    private int2 GetNearestCornerXZ(float3 pWorldPos)
    {
        int2 cellPosBotLeft = new int2((int)math.floor((pWorldPos - gridOriginPos).x / gridCellSize), (int)math.floor((pWorldPos - gridOriginPos).z / gridCellSize));
        float3 worldPosBotLeft = GetWorldPositionXZ(cellPosBotLeft.x, cellPosBotLeft.y);
        float3 woldPosCenter = worldPosBotLeft + new float3(gridCellSize / 2, 0, gridCellSize / 2);

        if (pWorldPos.x > woldPosCenter.x)
        {
            if (pWorldPos.y > woldPosCenter.y)
            {
                float3 worldPosTopRight = worldPosBotLeft + new float3(gridCellSize, 0, gridCellSize);
                return new int2((int)math.floor((worldPosTopRight - gridOriginPos).x / gridCellSize), (int)math.floor((worldPosTopRight - gridOriginPos).z / gridCellSize));
            }
            else
            {
                float3 worldPosBotRight = worldPosBotLeft + new float3(gridCellSize, 0, 0);
                return new int2((int)math.floor((worldPosBotRight - gridOriginPos).x / gridCellSize), (int)math.floor((worldPosBotRight - gridOriginPos).z / gridCellSize));
            }
        }
        else
        {
            if (pWorldPos.y > woldPosCenter.y)
            {
                float3 worldPosTopLeft = worldPosBotLeft + new float3(0, 0, gridCellSize);
                return new int2((int)math.floor((worldPosTopLeft - gridOriginPos).x / gridCellSize), (int)math.floor((worldPosTopLeft - gridOriginPos).z / gridCellSize));
            }
            else
            {
                return cellPosBotLeft;
            }
        }
    }

    [BurstCompile]
    private float3 GetWorldPositionXZ(int pX, int pZ)
    {
        return new float3(pX, 0, pZ) * gridCellSize + gridOriginPos;
    }

    [BurstCompile]
    private void ValidateGridPosition(int pLength, ref int x, ref int y)
    {
        x = math.clamp(x, 0, gridWidth - 1);
        y = math.clamp(y, 0, pLength - 1);
    }

    [BurstCompile]
    private int CalculateIndex(int x, int y)
    {
        return x + y * gridWidth;
    }
}