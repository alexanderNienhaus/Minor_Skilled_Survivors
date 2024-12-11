using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public partial class DronePathFindingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        RequireForUpdate<Drone>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        foreach ((RefRW<Drone> drone, Entity entity) in SystemAPI.Query<RefRW<Drone>>().WithEntityAccess())
        {
            if (drone.ValueRO.hasTarget)
                continue;

            GridXZ<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;
            int2 gridSize = new int2(grid.GetWidth(), grid.GetLength());
            float3 gridOriginPos = grid.GetOriginPos();
            float gridCellSize = grid.GetCellSize();            

            FindDronePathJob findDronePathJob = new FindDronePathJob
            {
                pathPositions = GetBufferLookup<PathPositions>(),
                pathNodeArray = GetPathNodeArray(grid, gridSize),
                gridSize = gridSize,
                endPos = SystemAPI.GetSingleton<Base>().position,
                gridOriginPos = gridOriginPos,
                gridCellSize = gridCellSize,
                thetaStar = new ThetaStar()
            };

            drone.ValueRW.hasTarget = true;
            findDronePathJob.ScheduleParallel();
        }
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