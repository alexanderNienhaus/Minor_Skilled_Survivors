using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class AlienAuthoring : MonoBehaviour
{
    private class Baker : Baker<AlienAuthoring>
    {
        public override void Bake(AlienAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Alien
            {

            });
        }
    }
}

[BurstCompile]
public struct Alien : IComponentData
{

}

