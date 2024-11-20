using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class AttackableEnemyAuthoring : MonoBehaviour
{
    [SerializeField] private float startHp = 100;
    [SerializeField] private float bounds;

    private class Baker : Baker<AttackableEnemyAuthoring>
    {
        public override void Bake(AttackableEnemyAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new AttackableEnemy
            {
                startHp = pAuthoring.startHp,
                currentHp = pAuthoring.startHp,
                bounds = pAuthoring.bounds
            });
        }
    }
}

[BurstCompile]
public struct AttackableEnemy : IComponentData
{
    public float startHp;
    public float currentHp;
    public float bounds;
}