using UnityEngine;
using Unity.Entities;
using Unity.Burst;

public class PathFollowAuthoring : MonoBehaviour
{
   [SerializeField] private float speed = 500;
   [SerializeField] private float checkDistanceFinal = 0.1f;

    private class Baker : Baker<PathFollowAuthoring>
    {
        public override void Bake(PathFollowAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PathFollow
            {
                speed = pAuthoring.speed,
                checkDistanceFinal = pAuthoring.checkDistanceFinal
            });
        }
    }
}

[BurstCompile]
public struct PathFollow : IComponentData
{
    public float speed;
    public float checkDistanceFinal;
}

