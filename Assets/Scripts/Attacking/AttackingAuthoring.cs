using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class AttackingAuthoring : MonoBehaviour
{
    [SerializeField] private float dmg;
    [SerializeField] private float range;
    [SerializeField] private float attackSpeed;

    private class Baker : Baker<AttackingAuthoring>
    {
        public override void Bake(AttackingAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Attacking
            {
                dmg = pAuthoring.dmg,
                range = pAuthoring.range,
                attackSpeed = pAuthoring.attackSpeed,
                currentTime = pAuthoring.attackSpeed
            });
        }
    }
}

[BurstCompile]
public struct Attacking : IComponentData
{
    public float dmg;
    public float range;
    public float attackSpeed;
    public float currentTime;
}