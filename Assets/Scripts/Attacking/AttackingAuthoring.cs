using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class AttackingAuthoring : MonoBehaviour
{
    [SerializeField] private AttackingSO attackingSO;

    private class Baker : Baker<AttackingAuthoring>
    {
        public override void Bake(AttackingAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Attacking
            {
                projectilePrefab = GetEntity(pAuthoring.attackingSO.projectilePrefab, TransformUsageFlags.Dynamic),
                parent = GetEntity(pAuthoring.gameObject, TransformUsageFlags.Dynamic),
                dmg = pAuthoring.attackingSO.dmg,
                range = pAuthoring.attackingSO.range,
                attackSpeed = pAuthoring.attackingSO.attackSpeed,
                currentTime = pAuthoring.attackingSO.attackSpeed,
                projectileSpeed = pAuthoring.attackingSO.projectileSpeed,
                projectileSpawnOffset = pAuthoring.attackingSO.projectileSpawnOffset,
                hasTarget = false
            });

            AddBuffer<PossibleAttackTargets>(entity);
            foreach (AttackableUnitType attackableUnitType in pAuthoring.attackingSO.possibleAttackTargets)
            {
                AppendToBuffer(entity, new PossibleAttackTargets
                {
                    possibleAttackTarget = attackableUnitType
                });
            }
        }
    }
}

[BurstCompile]
public struct Attacking : IComponentData
{
    public Entity projectilePrefab;
    public Entity parent;
    public float dmg;
    public float range;
    public float attackSpeed;
    public float currentTime;
    public float projectileSpeed;
    public float3 projectileSpawnOffset;
    public bool hasTarget;
}

[BurstCompile]
[InternalBufferCapacity(0)]
public struct PossibleAttackTargets : IBufferElementData
{
    public AttackableUnitType possibleAttackTarget;
}