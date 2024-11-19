using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class WaveSpawningAuthoring : MonoBehaviour
{
    [SerializeField] private WaveSO waveSO;

    private class Baker : Baker<WaveSpawningAuthoring>
    {
        public override void Bake(WaveSpawningAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new WaveSpawner
            {
                prefab = GetEntity(pAuthoring.waveSO.prefab, TransformUsageFlags.Dynamic),
                parent = GetEntity(pAuthoring.gameObject, TransformUsageFlags.Dynamic),
                amount = pAuthoring.waveSO.amount,
                position = pAuthoring.waveSO.position,
                radius = pAuthoring.waveSO.radius,
                time = pAuthoring.waveSO.time
            });
        }
    }
}

[BurstCompile]
public struct WaveSpawner : IComponentData
{
    public Entity prefab;
    public Entity parent;
    public int amount;
    public float3 position;
    public float radius;
    public float time;
}

