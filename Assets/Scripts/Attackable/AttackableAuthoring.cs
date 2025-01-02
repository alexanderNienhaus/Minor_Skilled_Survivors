using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class AttackableAuthoring : MonoBehaviour
{
    [SerializeField] private AttackableSO attackableSO;

    private class Baker : Baker<AttackableAuthoring>
    {
        public override void Bake(AttackableAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Attackable
            {
                attackableUnitType = pAuthoring.attackableSO.attackableUnitType,
                startHp = pAuthoring.attackableSO.startHp,
                currentHp = pAuthoring.attackableSO.startHp,
                boundsRadius = pAuthoring.attackableSO.boundsRadius,
                ressourceCost = pAuthoring.attackableSO.ressourceCost
            });
        }
    }
}

[BurstCompile]
public struct Attackable : IComponentData
{
    public AttackableUnitType attackableUnitType;
    public float startHp;
    public float currentHp;
    public float boundsRadius;
    public int ressourceCost;
}