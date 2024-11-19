using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class AlienPathFindingAuthoring : MonoBehaviour
{
    [SerializeField] private Vector3 target;

    private class Baker : Baker<AlienPathFindingAuthoring>
    {
        public override void Bake(AlienPathFindingAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new AlienPathFinding
            {
                target = pAuthoring.target
            });
        }
    }
}

[BurstCompile]
public struct AlienPathFinding : IComponentData, IEnableableComponent
{
    public float3 target;
}


