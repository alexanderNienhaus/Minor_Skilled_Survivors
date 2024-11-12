using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class BoidSpawningAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private Vector3 spawnPosition;
    [SerializeField] private float spawnRadius = 10;
    [SerializeField] private int spawnCount = 10;
    [SerializeField] private float scale = 1;

    private class Baker : Baker<BoidSpawningAuthoring>
    {
        public override void Bake(BoidSpawningAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BoidSpawning
            {
                prefab = GetEntity(pAuthoring.prefab, TransformUsageFlags.Dynamic),
                parent = GetEntity(pAuthoring.gameObject, TransformUsageFlags.Dynamic),
                spawnPosition = pAuthoring.spawnPosition,
                spawnRadius = pAuthoring.spawnRadius,
                spawnCount = pAuthoring.spawnCount,
                scale = pAuthoring.scale
            });
        }
    }
}

[BurstCompile]
public struct BoidSpawning : IComponentData
{
    public Entity prefab;
    public Entity parent;
    public float3 spawnPosition;
    public float spawnRadius;
    public int spawnCount;
    public float scale;
}
