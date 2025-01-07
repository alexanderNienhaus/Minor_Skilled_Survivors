using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public enum SpawnPoint
{
    Top,
    Mid,
    Bot
}

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

    protected override void OnCreate()
    {
        doOnce = true;
        RequireForUpdate<Base>();
    }

    protected override void OnUpdate()
    {
        if (doOnce)
        {
            topSpawn = new float3(175, 2, -185);
            midSpawn = new float3(175, 2, 0);
            botSpawn = new float3(175, 2, 185);
            topPath = FindPath(topSpawn + new float3(-25, 0, 0));
            midPath = FindPath(midSpawn + new float3(-25, 0, 0));
            botPath = FindPath(botSpawn + new float3(-25, 0, 0));
            doOnce = false;
        }

        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();

        foreach ((RefRW<Drone> drone, Entity entity)
            in SystemAPI.Query<RefRW<Drone>>().WithEntityAccess())
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

    public NativeList<PathPositions> FindPath(float3 startPos)
    {
        GridXZ<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;
        int2 gridSize = new (grid.GetWidth(), grid.GetLength());
        float3 gridOriginPos = grid.GetOriginPos();
        float gridCellSize = grid.GetCellSize();
        float3 cellMiddleOffset = new float3(1, 0, 1) * gridCellSize * 0.5f;
        int gridWidth = gridSize.x;

        int2 startNodePos = GetNearestCornerXZ(startPos, gridOriginPos, gridCellSize, gridWidth);
        ValidateGridPosition(gridSize[1], gridWidth, ref startNodePos.x, ref startNodePos.y);
        int startNodeIndex = CalculateIndex(gridWidth, startNodePos.x, startNodePos.y);

        int2 endNodePos = GetNearestCornerXZ(SystemAPI.GetSingleton<Base>().position, gridOriginPos, gridCellSize, gridWidth);
        ValidateGridPosition(gridSize[1], gridWidth, ref endNodePos.x, ref endNodePos.y);
        int endNodeIndex = CalculateIndex(gridWidth, endNodePos.x, endNodePos.y);

        ThetaStar thetaStar = new ();
        thetaStar.Initialize(gridSize, gridOriginPos, gridCellSize, gridWidth, endNodeIndex, endNodePos, startNodeIndex);
        NativeArray<PathNode> tmpPathNodeArray = GetPathNodeArray(grid, gridSize);
        thetaStar.FindPath(ref tmpPathNodeArray);

        NativeList<PathPositions> path = new(gridSize.x * gridSize.y, Allocator.Persistent);
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

        return path;
    }

    [BurstCompile]
    private float3 GetWorldPositionXZ(int pX, int pZ, float3 pGridOriginPos, float pGridCellSize)
    {
        return new float3(pX, 0, pZ) * pGridCellSize + pGridOriginPos;
    }

    [BurstCompile]
    private void ValidateGridPosition(int pLength, int pGridWidth, ref int x, ref int y)
    {
        x = math.clamp(x, 0, pGridWidth - 1);
        y = math.clamp(y, 0, pLength - 1);
    }

    [BurstCompile]
    private int CalculateIndex(int pGridWidth, int x, int y)
    {
        return x + y * pGridWidth;
    }

    [BurstCompile]
    private int2 GetNearestCornerXZ(float3 pWorldPos, float3 gridOriginPos, float gridCellSize, int gridWidth)
    {
        int2 cellPosBotLeft = new ((int)math.floor((pWorldPos - gridOriginPos).x / gridCellSize), (int)math.floor((pWorldPos - gridOriginPos).z / gridCellSize));
        float3 worldPosBotLeft = GetWorldPositionXZ(cellPosBotLeft.x, cellPosBotLeft.y, gridOriginPos, gridCellSize);
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
    private void CalculatePath(NativeArray<PathNode> pathNodeArray, PathNode endNode, ref NativeList<PathPositions> path, float3 pGridOriginPos, float pGridCellSize)
    {
        PathNode currentNode = endNode;
        while (currentNode.cameFromNodeIndex != -1)
        {
            PathNode cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
            path.Add(new PathPositions
            {
                pos = GetWorldPositionXZ(cameFromNode.x, cameFromNode.z, pGridOriginPos, pGridCellSize)
            });
            currentNode = cameFromNode;
        }
        //path.RemoveAt(path.Length - 1);
    }

    private NativeArray<PathNode> GetPathNodeArray(GridXZ<GridNode> pGrid, int2 pGridSize)
    {
        NativeArray<PathNode> pathNodeArray = new NativeArray<PathNode>(pGridSize.x * pGridSize.y, Allocator.Persistent);

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