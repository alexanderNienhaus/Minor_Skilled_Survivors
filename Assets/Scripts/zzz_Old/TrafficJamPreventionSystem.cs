using Unity.Entities;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;

[BurstCompile]
public partial class TrafficJamPreventionSystem : SystemBase
{

    [BurstCompile]
    protected override void OnUpdate()
    {
        GridXZ<GridNode> grid = PathfindingGridSetup.Instance.pathfindingGrid;

        TrafficJamPreventionJob followpathJob = new TrafficJamPreventionJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };

        followpathJob.ScheduleParallel();
    }

    [BurstCompile]
    private partial struct TrafficJamPreventionJob : IJobEntity
    {
        public float deltaTime;

        [BurstCompile]
        public void Execute(ref TrafficJamPrevention trafficJamPrevention, ref PhysicsCollider physicsCollider, in LocalTransform pLocalTransform,
            in PhysicsVelocity pPhysicsVelocity)
        {
            if (math.any(pPhysicsVelocity.Linear > float3.zero) ){
                float dist = math.distance(trafficJamPrevention.lastPosition, pLocalTransform.Position);
                float length = math.length(pPhysicsVelocity.Linear * deltaTime) / 2;
                if (dist <= length)
                {
                    trafficJamPrevention.timeStuck += deltaTime;
                    if (trafficJamPrevention.timeStuck >= trafficJamPrevention.timeUntilJam)
                    {
                        //physicsCollider.Value.Value.SetCollisionFilter(trafficJamPrevention.trafficJamFilter);
                        trafficJamPrevention.timeStuck = 0;
                    }
                }
                else
                {
                    //physicsCollider.Value.Value.SetCollisionFilter(trafficJamPrevention.orginialFilter);
                }
                trafficJamPrevention.lastPosition = pLocalTransform.Position;
            }
        }
    }
}
