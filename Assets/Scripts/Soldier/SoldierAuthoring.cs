using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class SoldierAuthoring : MonoBehaviour
{
    private class Baker : Baker<SoldierAuthoring>
    {
        public override void Bake(SoldierAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Alien
            {

            });
        }
    }
}

[BurstCompile]
public struct Soldier : IComponentData
{

}

