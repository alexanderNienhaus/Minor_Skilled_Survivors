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
            AddComponent(entity, new Ressource
            {
                currentRessourceCount = pAuthoring.startRessourceCount
            });
        }
    }
}

[BurstCompile]
public struct Ressource : IComponentData
{
    public int currentRessourceCount;
}