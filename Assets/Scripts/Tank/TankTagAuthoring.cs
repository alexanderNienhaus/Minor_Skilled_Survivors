using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class TankTagAuthoring : MonoBehaviour
{
    private class Baker : Baker<TankTagAuthoring>
    {
        public override void Bake(TankTagAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Tank
            {

            });
        }
    }
}

[BurstCompile]
public struct Tank : IComponentData
{

}

