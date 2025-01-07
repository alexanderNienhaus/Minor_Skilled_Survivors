using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class BlobShadowAuthoring : MonoBehaviour
{
    [SerializeField] private GameObject shadow;
    [SerializeField] private float maxHeight;

    private class Baker : Baker<BlobShadowAuthoring>
    {
        public override void Bake(BlobShadowAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new BlobShadow
            {
                //shadow = GetEntity(pAuthoring.shadow, TransformUsageFlags.Dynamic)
            });
        }
    }
}

[BurstCompile]
public struct BlobShadow : IComponentData
{
    public Entity shadow;
}