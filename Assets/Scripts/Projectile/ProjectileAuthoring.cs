using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class ProjectileAuthoring : MonoBehaviour
{    
    private class Baker : Baker<ProjectileAuthoring>
    {
        public override void Bake(ProjectileAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Projectile
            {
            });
        }
    }
}

[BurstCompile]
public struct Projectile : IComponentData
{
    public float maxTimeToLife;
    public float currentTimeToLife;
}