using Unity.Entities;
using Unity.Jobs;

public partial class PathFollowSystem : SystemBase
{
    protected override void OnUpdate()
    {
        GridXZ<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;

        FollowpathJob followpathJob = new FollowpathJob
        {
            allPathPositions = GetBufferLookup<PathPositions>(),
            gridOriginPos = grid.GetOriginPos(),
            gridCellSize = grid.GetCellSize(),
            deltaTime = SystemAPI.Time.DeltaTime,
        };

        followpathJob.ScheduleParallel();
    }
}
