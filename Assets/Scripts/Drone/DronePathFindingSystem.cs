using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public enum SpawnPoint
{
    Top,
    Mid,
    Bot
}

[BurstCompile]
[UpdateAfter(typeof(RegisterMapLayoutSystem))]
public partial class DronePathFindingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;
    private NativeList<PathPositions> topPath;
    private NativeList<PathPositions> midPath;
    private NativeList<PathPositions> botPath;
    private float3 topSpawn;
    private float3 midSpawn;
    private float3 botSpawn;
    private bool doOnce;

    [BurstCompile]
    protected override void OnCreate()
    {
        doOnce = true;
        RequireForUpdate<Base>();
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        if (doOnce)
        {
            topSpawn = new float3(225, 2, -185);
            midSpawn = new float3(225, 2, 0);
            botSpawn = new float3(225, 2, 185);
            topPath = FindPath(topSpawn + new float3(-25, 0, 0));
            midPath = FindPath(midSpawn + new float3(-25, 0, 0));
            botPath = FindPath(botSpawn + new float3(-25, 0, 0));
            doOnce = false;
        }

        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        foreach ((RefRW<Drone> drone, Entity entity) in SystemAPI.Query<RefRW<Drone>>().WithEntityAccess())
        {
            if (drone.ValueRO.foundPath)
                continue;

            if (drone.ValueRO.spawnPoint == SpawnPoint.Top)
            {
                ecb.SetBuffer<PathPositions>(entity).AddRange(topPath.AsArray());
            }
            else if (drone.ValueRO.spawnPoint == SpawnPoint.Mid)
            {
                ecb.SetBuffer<PathPositions>(entity).AddRange(midPath.AsArray());
            }
            else if (drone.ValueRO.spawnPoint == SpawnPoint.Bot)
            {
                ecb.SetBuffer<PathPositions>(entity).AddRange(botPath.AsArray());
            }

            drone.ValueRW.foundPath = true;
            //ecb.SetBuffer<PathPositions>(entity).Add(new PathPositions() { pos = worldPos });
        }
    }

    [BurstCompile]
    public NativeList<PathPositions> FindPath(float3 pStartPos)
    {
        GridXZ<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;
        int2 gridSize = new (grid.GetWidth(), grid.GetLength());
        float3 gridOriginPos = grid.GetOriginPos();
        float gridCellSize = grid.GetCellSize();
        float3 cellMiddleOffset = new float3(1, 0, 1) * gridCellSize * 0.5f;
        int gridWidth = gridSize.x;

        int2 startNodePos = GetNearestCornerXZ(pStartPos, gridOriginPos, gridCellSize);
        ValidateGridPosition(gridSize[1], gridWidth, ref startNodePos.x, ref startNodePos.y);
        int startNodeIndex = CalculateIndex(gridWidth, startNodePos.x, startNodePos.y);

        int2 endNodePos = GetNearestCornerXZ(SystemAPI.GetSingleton<Base>().position, gridOriginPos, gridCellSize);
        ValidateGridPosition(gridSize[1], gridWidth, ref endNodePos.x, ref endNodePos.y);
        int endNodeIndex = CalculateIndex(gridWidth, endNodePos.x, endNodePos.y);

        ThetaStar thetaStar = new ();
        thetaStar.Initialize(gridSize, gridOriginPos, gridCellSize, gridWidth, endNodeIndex, endNodePos, startNodeIndex);
        NativeArray<PathNode> tmpPathNodeArray = GetPathNodeArray(grid, gridSize);
        thetaStar.FindPath(ref tmpPathNodeArray);

        NativeList<PathPositions> path = new (gridSize.x * gridSize.y, Allocator.Persistent);
        PathNode endNode = tmpPathNodeArray[endNodeIndex];
        if (endNode.cameFromNodeIndex == -1)
        {
            //Didnt find a path
        }
        else
        {
            path.Add(new PathPositions
            {
                pos = GetWorldPositionXZ(endNode.x, endNode.z, gridOriginPos, gridCellSize) + cellMiddleOffset
            });

            CalculatePath(tmpPathNodeArray, endNode, ref path, gridOriginPos, gridCellSize);
        }

        tmpPathNodeArray.Dispose();
        return path;
    }

    protected override void OnDestroy()
    {
        topPath.Dispose();
        midPath.Dispose();
        botPath.Dispose();
    }

    [BurstCompile]
    private float3 GetWorldPositionXZ(int pX, int pZ, float3 pGridOriginPos, float pGridCellSize)
    {
        return new float3(pX, 0, pZ) * pGridCellSize + pGridOriginPos;
    }

    [BurstCompile]
    private void ValidateGridPosition(int pLength, int pGridWidth, ref int pX, ref int pZ)
    {
        pX = math.clamp(pX, 0, pGridWidth - 1);
        pZ = math.clamp(pZ, 0, pLength - 1);
    }

    [BurstCompile]
    private int CalculateIndex(int pGridWidth, int pX, int pZ)
    {
        return pX + pZ * pGridWidth;
    }

    [BurstCompile]
    private int2 GetNearestCornerXZ(float3 pWorldPos, float3 pGridOriginPos, float pGridCellSize)
    {
        int2 cellPosBotLeft = new ((int)math.floor((pWorldPos - pGridOriginPos).x / pGridCellSize), (int)math.floor((pWorldPos - pGridOriginPos).z / pGridCellSize));
        float3 worldPosBotLeft = GetWorldPositionXZ(cellPosBotLeft.x, cellPosBotLeft.y, pGridOriginPos, pGridCellSize);
        float3 woldPosCenter = worldPosBotLeft + new float3(pGridCellSize / 2, 0, pGridCellSize / 2);

        if (pWorldPos.x > woldPosCenter.x)
        {
            if (pWorldPos.y > woldPosCenter.y)
            {
                float3 worldPosTopRight = worldPosBotLeft + new float3(pGridCellSize, 0, pGridCellSize);
                return new int2((int)math.floor((worldPosTopRight - pGridOriginPos).x / pGridCellSize), (int)math.floor((worldPosTopRight - pGridOriginPos).z / pGridCellSize));
            }
            else
            {
                float3 worldPosBotRight = worldPosBotLeft + new float3(pGridCellSize, 0, 0);
                return new int2((int)math.floor((worldPosBotRight - pGridOriginPos).x / pGridCellSize), (int)math.floor((worldPosBotRight - pGridOriginPos).z / pGridCellSize));
            }
        }
        else
        {
            if (pWorldPos.y > woldPosCenter.y)
            {
                float3 worldPosTopLeft = worldPosBotLeft + new float3(0, 0, pGridCellSize);
                return new int2((int)math.floor((worldPosTopLeft - pGridOriginPos).x / pGridCellSize), (int)math.floor((worldPosTopLeft - pGridOriginPos).z / pGridCellSize));
            }
            else
            {
                return cellPosBotLeft;
            }
        }
    }

    [BurstCompile]
    private void CalculatePath(NativeArray<PathNode> pPathNodeArray, PathNode pEndNode, ref NativeList<PathPositions> pPath, float3 pGridOriginPos, float pGridCellSize)
    {
        PathNode currentNode = pEndNode;
        while (currentNode.cameFromNodeIndex != -1)
        {
            PathNode cameFromNode = pPathNodeArray[currentNode.cameFromNodeIndex];
            pPath.Add(new PathPositions
            {
                pos = GetWorldPositionXZ(cameFromNode.x, cameFromNode.z, pGridOriginPos, pGridCellSize)
            });
            currentNode = cameFromNode;
        }
        //path.RemoveAt(path.Length - 1);
    }

    private NativeArray<PathNode> GetPathNodeArray(GridXZ<GridNode> pGrid, int2 pGridSize)
    {
        NativeArray<PathNode> pathNodeArray = new (pGridSize.x * pGridSize.y, Allocator.Temp);

        for (int x = 0; x < pGridSize.x; x++)
        {
            for (int z = 0; z < pGridSize.y; z++)
            {
                PathNode pathNode = new PathNode
                {
                    x = x,
                    z = z,
                    index = x + z * pGridSize.x,

                    gCost = int.MaxValue,

                    isWalkable = pGrid.GetGridObject(x, z).GetIsWalkable(),
                    cameFromNodeIndex = -1
                };

                pathNodeArray[pathNode.index] = pathNode;
            }
        }

        return pathNodeArray;
    }
}