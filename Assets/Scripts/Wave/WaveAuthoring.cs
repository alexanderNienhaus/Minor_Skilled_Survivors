using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using System.Collections.Generic;

public class WaveAuthoring : MonoBehaviour
{
    [SerializeField] private List<WaveSO> waves;

    private class Baker : Baker<WaveAuthoring>
    {
        public override void Bake(WaveAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddBuffer<Spawn>(entity);
            AddBuffer<Wave>(entity);
            int waveNumber = 0;
            foreach (WaveSO wave in pAuthoring.waves)
            {
                int numberOfEnemies = 0;
                waveNumber++;
                int spawnNumber = 0;

                foreach (SpawnSO spawn in wave.spawns)
                {
                    spawnNumber++;
                    numberOfEnemies += spawn.amountToSpawn;
                    AppendToBuffer(entity, new Spawn
                    {
                        waveNumber = waveNumber,
                        spawnNumber = spawnNumber,
                        prefab = GetEntity(spawn.prefab, TransformUsageFlags.Dynamic),
                        parent = GetEntity(pAuthoring.gameObject, TransformUsageFlags.Dynamic),
                        unitSize = spawn.unitSize,
                        unitType = spawn.unitType,
                        amountToSpawn = spawn.amountToSpawn,
                        spawnPosition = spawn.spawnPosition,
                        isSphericalSpawn = spawn.isSphericalSpawn,
                        spawnRadiusMax = spawn.spawnRadiusMax,
                        spawnRadiusMin = spawn.spawnRadiusMin,
                        whenToSpawn = spawn.whenToSpawn,
                        spawningDuration = spawn.spawningDuration
                    });
                }

                AppendToBuffer(entity, new Wave
                {
                    waveNumber = waveNumber,
                    numberOfEnemies = numberOfEnemies
                });
            }
        }
    }
}

[BurstCompile]
[InternalBufferCapacity(0)]
public struct Spawn : IBufferElementData
{
    public int waveNumber;
    public int spawnNumber;
    public Entity prefab;
    public Entity parent;
    public AttackableUnitType unitType;
    public float unitSize;
    public int amountToSpawn;
    public float3 spawnPosition;
    public float spawnRadiusMax;
    public float spawnRadiusMin;
    public bool isSphericalSpawn;
    public float whenToSpawn;
    public float spawningDuration;
}

[BurstCompile]
[InternalBufferCapacity(0)]
public struct Wave : IBufferElementData
{
    public int waveNumber;
    public int numberOfEnemies;
}
