using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class AttackableUnitAuthoring : MonoBehaviour
{
    [SerializeField] private float startHp = 100;
    [SerializeField] private float bounds;

    private class Baker : Baker<AttackableUnitAuthoring>
    {
        public override void Bake(AttackableUnitAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new AttackableUnit
            {
                startHp = pAuthoring.startHp,
                currentHp = pAuthoring.startHp,
                bounds = pAuthoring.bounds
            });
        }
    }
}

[BurstCompile]
public struct AttackableUnit : IComponentData
{
    public float startHp;
    public float currentHp;
    public float bounds;
}