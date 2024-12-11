using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class TankAuthoring : MonoBehaviour
{
    private class Baker : Baker<TankAuthoring>
    {
        public override void Bake(TankAuthoring pAuthoring)
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

