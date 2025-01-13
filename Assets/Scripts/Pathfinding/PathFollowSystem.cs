using Unity.Entities;
using Unity.Jobs;

public partial class PathFollowSystem : SystemBase
{
    protected override void OnUpdate()
    {
        PathfindingGridSetup pathfindingGridSetup = PathfindingGridSetup.Instance;
        if (pathfindingGridSetup == null)
            return;

        GridXZ<GridNode> grid = pathfindingGridSetup.pathfindingGrid;

        PathFollowJob followpathJob = new ()
        {
            em = EntityManager,
            allPathPositions = GetBufferLookup<PathPositions>(),
            gridOriginPos = grid.GetOriginPos(),
            gridCellSize = grid.GetCellSize(),
            deltaTime = 0.01f //SystemAPI.Time.DeltaTime
        };

        Dependency = followpathJob.ScheduleParallel(Dependency);
    }
}
