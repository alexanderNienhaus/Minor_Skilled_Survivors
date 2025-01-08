using Unity.Entities;

public partial class ProjectileSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem beginFixedStepSimulationEcbSystem;

    protected override void OnCreate()
    {
        beginFixedStepSimulationEcbSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = beginFixedStepSimulationEcbSystem.CreateCommandBuffer();
        foreach ((RefRW<Projectile> projectile, Entity entity) in SystemAPI.Query<RefRW<Projectile>>().WithEntityAccess())
        {
            projectile.ValueRW.currentTimeToLife += SystemAPI.Time.DeltaTime;

            if (!float.IsNaN(projectile.ValueRO.maxTimeToLife) && projectile.ValueRO.currentTimeToLife <= projectile.ValueRO.maxTimeToLife)
                continue;
            
            ecb.DestroyEntity(entity);
        }
    }
}
