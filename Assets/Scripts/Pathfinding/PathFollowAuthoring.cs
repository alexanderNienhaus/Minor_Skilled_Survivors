using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class PathFollowAuthoring : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 500;
    [SerializeField] private float rotationSpeed = 5;
    [SerializeField] private float checkDistanceFinal = 0.1f;

    private class Baker : Baker<PathFollowAuthoring>
    {
        public override void Bake(PathFollowAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PathFollow
            {
                movementSpeed = pAuthoring.movementSpeed,
                rotationSpeed = pAuthoring.rotationSpeed,
                checkDistanceFinal = pAuthoring.checkDistanceFinal
            });
        }
    }
}

[BurstCompile]
public struct PathFollow : IComponentData
{
    public float movementSpeed;
    public float rotationSpeed;
    public float checkDistanceFinal;
    public quaternion targetRotation;
}

