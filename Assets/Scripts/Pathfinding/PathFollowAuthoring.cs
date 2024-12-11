using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class PathFollowAuthoring : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 500;
    [SerializeField] private float rotationSpeed = 5;
    [SerializeField] private float checkDistanceFinal = 0.1f;
    [SerializeField] private float yValue = 0;

    private class Baker : Baker<PathFollowAuthoring>
    {
        public override void Bake(PathFollowAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PathFollow
            {
                movementSpeed = pAuthoring.movementSpeed,
                rotationSpeed = pAuthoring.rotationSpeed,
                checkDistanceFinal = pAuthoring.checkDistanceFinal,
                isInAttackMode = true,
                enemyPos = float3.zero,
                yValue = pAuthoring.yValue
            });
        }
    }
}

[BurstCompile]
public struct PathFollow : IComponentData
{
    public float movementSpeed;
    public float checkDistanceFinal;
    public float rotationSpeed;
    public float3 groupMovement;
    public bool isInAttackMode;
    public float3 enemyPos;
    public float yValue;
}

