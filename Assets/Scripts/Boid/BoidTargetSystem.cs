using Unity.Entities;
using Unity.Transforms;

[UpdateAfter(typeof(BoidSpawningSystem))]
public partial class BoidTargetSystem : SystemBase
{
    private bool targetResetDone = false;

    protected override void OnCreate() { }

    protected override void OnUpdate()
    {
        //Enabled = false;
        int i = 0;
        foreach ((RefRO<BoidTarget> target, RefRO<LocalTransform> localTransformTarget)
            in SystemAPI.Query<RefRO<BoidTarget>, RefRO<LocalTransform>>())
        {
            BoidTargetJob findTargetJob = new BoidTargetJob
            {
                target = target.ValueRO,
                localTransformTarget = localTransformTarget.ValueRO
            };
            findTargetJob.ScheduleParallel();
            i++;
            targetResetDone = false;
        }

        if (i == 0 && !targetResetDone)
        {
            targetResetDone = true;
            foreach (RefRW<Boid> boid in SystemAPI.Query<RefRW<Boid>>())
            {
                boid.ValueRW.target = LocalTransform.Identity;
                boid.ValueRW.target.Scale = 0;
            }
        }
    }
}
