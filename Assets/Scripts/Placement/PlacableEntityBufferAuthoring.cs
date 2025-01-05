using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using System.Collections.Generic;

public class PlacableEntityBufferAuthoring : MonoBehaviour
{
    [SerializeField] private List<GameObject> prefabs;

    private class Baker : Baker<PlacableEntityBufferAuthoring>
    {
        public override void Bake(PlacableEntityBufferAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddBuffer<PlacableEntityBuffer>(entity);
            foreach (GameObject prefab in pAuthoring.prefabs)
            {
                AppendToBuffer(entity, new PlacableEntityBuffer
                {
                    prefab = GetEntity(prefab, TransformUsageFlags.Dynamic),
                    parent = GetEntity(pAuthoring.gameObject, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}

[BurstCompile]
[InternalBufferCapacity(0)]
public struct PlacableEntityBuffer : IBufferElementData
{
    public Entity prefab;
    public Entity parent;
}