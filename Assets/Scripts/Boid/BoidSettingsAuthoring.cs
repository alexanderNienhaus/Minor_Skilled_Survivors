using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class BoidSettingsAuthoring : MonoBehaviour
{
    [SerializeField] private BoidSettingsSO boidSettingsSO;

    private class Baker : Baker<BoidSettingsAuthoring>
    {
        public override void Bake(BoidSettingsAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BoidSettings
            {
                    dmg = pAuthoring.boidSettingsSO.dmg,
                    strikeDistance = pAuthoring.boidSettingsSO.strikeDistance,
                    minSpeed = pAuthoring.boidSettingsSO.minSpeed,
                    maxSpeed = pAuthoring.boidSettingsSO.maxSpeed,
                    perceptionRadius = pAuthoring.boidSettingsSO.perceptionRadius,
                    avoidanceRadius = pAuthoring.boidSettingsSO.avoidanceRadius,
                    maxSteerForce = pAuthoring.boidSettingsSO.maxSteerForce,
                    alignWeight = pAuthoring.boidSettingsSO.alignWeight,
                    cohesionWeight = pAuthoring.boidSettingsSO.cohesionWeight,
                    seperateWeight = pAuthoring.boidSettingsSO.seperateWeight,
                    targetWeight = pAuthoring.boidSettingsSO.targetWeight,
                    obstacleMask = pAuthoring.boidSettingsSO.obstacleMask,
                    boundsRadius = pAuthoring.boidSettingsSO.boundsRadius,
                    avoidCollisionWeight = pAuthoring.boidSettingsSO.avoidCollisionWeight,
                    collisionAvoidDst = pAuthoring.boidSettingsSO.collisionAvoidDst
            });
        }
    }
}

[BurstCompile]
public struct BoidSettings : IComponentData
{
    public int dmg;
    public float strikeDistance;

    public float minSpeed;
    public float maxSpeed;
    public float perceptionRadius;
    public float avoidanceRadius;
    public float maxSteerForce;

    public float alignWeight;
    public float cohesionWeight;
    public float seperateWeight;
    public float targetWeight;

    public LayerMask obstacleMask;
    public float boundsRadius;
    public float avoidCollisionWeight;
    public float collisionAvoidDst;
}
