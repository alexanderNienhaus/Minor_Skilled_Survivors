using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using System.Collections.Generic;

public class PlacableObjectsBufferAuthoring : MonoBehaviour
{
    [SerializeField] private List<GameObject> prefabs;

    private class Baker : Baker<PlacableObjectsBufferAuthoring>
    {
        public override void Bake(PlacableObjectsBufferAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddBuffer<PlacableObjectsBuffer>(entity);
            foreach (GameObject prefab in pAuthoring.prefabs)
            {
                AppendToBuffer(entity, new PlacableObjectsBuffer
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
public struct PlacableObjectsBuffer : IBufferElementData
{
    public Entity prefab;
    public Entity parent;
}