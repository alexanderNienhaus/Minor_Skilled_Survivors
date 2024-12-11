using Unity.Entities;
using Unity.Jobs;

public partial class PathFollowSystem : SystemBase
{
    protected override void OnUpdate()
    {
        GridXZ<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;

        PathFollowJob followpathJob = new PathFollowJob
        {
            allPathPositions = GetBufferLookup<PathPositions>(),
            gridOriginPos = grid.GetOriginPos(),
            gridCellSize = grid.GetCellSize(),
            deltaTime = 0.01f//SystemAPI.Time.DeltaTime
        };

        followpathJob.ScheduleParallel();
    }
}
