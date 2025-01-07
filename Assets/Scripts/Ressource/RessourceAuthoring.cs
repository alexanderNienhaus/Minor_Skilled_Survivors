using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class RessourceAuthoring : MonoBehaviour
{
    [SerializeField] private int startRessourceCount = 100;

    private class Baker : Baker<RessourceAuthoring>
    {
        public override void Bake(RessourceAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Resource
            {
                currentRessourceCount = pAuthoring.startRessourceCount
            });
        }
    }
}

[BurstCompile]
public struct Resource : IComponentData
{
    public int currentRessourceCount;
}