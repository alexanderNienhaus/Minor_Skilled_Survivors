using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class BaseAuthoring : MonoBehaviour
{
    private class Baker : Baker<BaseAuthoring>
    {
        public override void Bake(BaseAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Base
            {

            });
        }
    }
}

[BurstCompile]
public struct Base : IComponentData
{

}

