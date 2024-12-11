using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class AttackingAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float dmg;
    [SerializeField] private float range;
    [SerializeField] private float attackSpeed;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private Vector3 projectileSpawnOffset;

    private class Baker : Baker<AttackingAuthoring>
    {
        public override void Bake(AttackingAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Attacking
            {
                projectilePrefab = GetEntity(pAuthoring.projectilePrefab, TransformUsageFlags.Dynamic),
                parent = GetEntity(pAuthoring.gameObject, TransformUsageFlags.Dynamic),
                dmg = pAuthoring.dmg,
                range = pAuthoring.range,
                attackSpeed = pAuthoring.attackSpeed,
                currentTime = pAuthoring.attackSpeed,
                projectileSpeed = pAuthoring.projectileSpeed,
                projectileSpawnOffset = pAuthoring.projectileSpawnOffset,
                hasTarget = false
            });
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