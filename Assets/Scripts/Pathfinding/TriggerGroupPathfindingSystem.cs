using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

[BurstCompile]
public partial class TriggerGroupPathfindingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (Input.GetMouseButtonDown(1) && PathfindingGridSetup.Instance != null)
        {
            GridXZ<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;
            int2 gridSize = new int2(grid.GetWidth(), grid.GetLength());
            float3 gridOriginPos = grid.GetOriginPos();
            float gridCellSize = grid.GetCellSize();

            int selectedUnitCount = World.GetOrCreateSystemManaged<SelectedUnitCountSystem>().GetSelectedUnitCount();
            int boxSize = (int)math.ceil(math.sqrt(selectedUnitCount));

            ThetaStarFindPathJob findPathJob = new ThetaStarFindPathJob
            {
                pathPositions = GetBufferLookup<PathPositions>(),
                pathNodeArray = GetPathNodeArray(grid, gridSize),
                unitEndPositionOffsets = CalculateEndPositionOffsets(boxSize, boxSize, 0, gridCellSize, false),
                gridSize = gridSize,
                mouseEndPos = GetMouseWorldPos(),
                gridOriginPos = gridOriginPos,
                gridCellSize = gridCellSize
            };

            findPathJob.ScheduleParallel();
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f))
        {
            return raycastHit.point;
        }
        else
        {
            return Vector3.zero;
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

    [BurstCompile]
    private NativeArray<float3> CalculateEndPositionOffsets(int pUnitWidth, int pUnitDepth, float pNthOffset, float pSpread, bool pHollow)
    {
        NativeArray<float3> endPositionOffsets = new NativeArray<float3>(pUnitWidth * pUnitDepth, Allocator.Persistent);
        if (pUnitWidth == 1 && pUnitDepth == 1)
        {
            endPositionOffsets[0] = new float3(0, 0, 0);
            return endPositionOffsets;
        }

        float3 middleOffset = new float3(pUnitWidth * 0.25f, 0, pUnitDepth * 0.25f);
        int i = 0;
        for (int x = 0; x < pUnitWidth; x++)
        {
            for (int z = 0; z < pUnitDepth; z++)
            {
                if (pHollow && x != 0 && x != pUnitWidth - 1 && z != 0 && z != pUnitDepth - 1)
                {
                    continue;
                }
                float3 pos = new float3(x + (z % 2 == 0 ? 0 : pNthOffset), 0, z);
                pos -= middleOffset;
                pos *= pSpread;
                endPositionOffsets[i] = pos;
                i++;
            }
        }
        return endPositionOffsets;
    }
}
