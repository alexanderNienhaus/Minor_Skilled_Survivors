using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class TrafficJamPreventionAuthoring : MonoBehaviour
{
    [SerializeField] private float timeUntilJam;

    private class Baker : Baker<TrafficJamPreventionAuthoring>
    {
        public override void Bake(TrafficJamPreventionAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new TrafficJamPrevention
            {
                timeUntilJam = pAuthoring.timeUntilJam
            });
        }
    }
}

[BurstCompile]
public struct TrafficJamPrevention : IComponentData
{
    public float timeUntilJam;
    public float timeStuck;
    public float3 lastPosition;
}
