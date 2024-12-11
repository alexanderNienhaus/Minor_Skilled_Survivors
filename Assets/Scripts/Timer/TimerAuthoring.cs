using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class TimerAuthoring : MonoBehaviour
{
    [SerializeField] private float maxBuildTime = 120;

    private class Baker : Baker<TimerAuthoring>
    {
        public override void Bake(TimerAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Timer
            {
                maxBuildTime = pAuthoring.maxBuildTime
            });
        }
    }
}

[BurstCompile]
public struct Timer : IComponentData
{
    public float maxBuildTime;
}