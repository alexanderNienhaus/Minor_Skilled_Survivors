using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public partial class AlienPathFindingSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        RequireForUpdate<AlienPathFinding>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        foreach ((RefRO<AlienPathFinding> alienPathFinding, Entity entity) in SystemAPI.Query<RefRO<AlienPathFinding>>().WithEntityAccess())
        {
            GridXZ<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;
            int2 gridSize = new int2(grid.GetWidth(), grid.GetLength());
            float3 gridOriginPos = grid.GetOriginPos();
            float gridCellSize = grid.GetCellSize();

            FindAlienPathJob findAlienPathJob = new FindAlienPathJob
            {
                pathPositions = GetBufferLookup<PathPositions>(),
                pathNodeArray = GetPathNodeArray(grid, gridSize),
                gridSize = gridSize,
                endPos = alienPathFinding.ValueRO.target,
                gridOriginPos = gridOriginPos,
                gridCellSize = gridCellSize,
                thetaStar = new ThetaStar()
            };

            findAlienPathJob.ScheduleParallel();

            ecb.SetComponentEnabled<AlienPathFinding>(entity, false);
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