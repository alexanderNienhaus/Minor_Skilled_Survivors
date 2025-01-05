using Unity.Entities;
using Unity.Transforms;

public partial class ProjectileSystem : SystemBase
{
    private EndFixedStepSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        RequireForUpdate<Attackable>();
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<EndFixedStepSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        foreach ((RefRW<Projectile> projectile, Entity entity) in SystemAPI.Query<RefRW<Projectile>>().WithEntityAccess())
        {
            projectile.ValueRW.currentTimeToLife += SystemAPI.Time.DeltaTime;
            if (projectile.ValueRO.currentTimeToLife > projectile.ValueRO.maxTimeToLife)
            {
                ecb.DestroyEntity(entity);
            }

            //ecb.RemoveComponent<Parent>(entity);
        }
    }
}
