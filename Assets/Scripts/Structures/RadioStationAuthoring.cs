using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class RadioStationAuthoring : MonoBehaviour
{
    [SerializeField] private SpawnSO spawn;

    private class Baker : Baker<RadioStationAuthoring>
    {
        public override void Bake(RadioStationAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new RadioStation
            {
                prefab = GetEntity(pAuthoring.spawn.prefab, TransformUsageFlags.Dynamic),
                parent = GetEntity(pAuthoring.gameObject, TransformUsageFlags.Dynamic),
                unitSize = pAuthoring.spawn.unitSize,
                unitType = pAuthoring.spawn.unitType,
                amountToSpawn = pAuthoring.spawn.amountToSpawn,
                spawnPosition = pAuthoring.spawn.spawnPosition,
                isSphericalSpawn = pAuthoring.spawn.isSphericalSpawn,
                spawnRadiusMax = pAuthoring.spawn.spawnRadiusMax,
                spawnRadiusMin = pAuthoring.spawn.spawnRadiusMin,
                whenToSpawn = pAuthoring.spawn.whenToSpawn,
                spawningDuration = pAuthoring.spawn.spawningDuration
            });
        }
    }
}

[BurstCompile]
public struct RadioStation : IComponentData
{
    public bool hasSpawned;
    public Entity prefab;
    public Entity parent;
    public UnitType unitType;
    public float unitSize;
    public int amountToSpawn;
    public float3 spawnPosition;
    public float spawnRadiusMax;
    public float spawnRadiusMin;
    public bool isSphericalSpawn;
    public float whenToSpawn;
    public float spawningDuration;
}

