using UnityEngine;
using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

public class PathFollowAuthoring : MonoBehaviour
{
    [SerializeField] private PathFollowSO pathFollowSO;

    private class Baker : Baker<PathFollowAuthoring>
    {
        public override void Bake(PathFollowAuthoring pAuthoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new PathFollow
            {
                movementSpeed = pAuthoring.pathFollowSO.movementSpeed,
                rotationSpeed = pAuthoring.pathFollowSO.rotationSpeed,
                checkDistanceEndDestination = pAuthoring.pathFollowSO.checkDistanceEndDestination,
                isInAttackMode = true,
                enemyPos = float3.zero,
                yValue = pAuthoring.pathFollowSO.yValue
            });
        }
    }
}

[BurstCompile]
public struct PathFollow : IComponentData
{
    public float movementSpeed;
    public float checkDistanceEndDestination;
    public float rotationSpeed;
    public float3 groupMovement;
    public bool isInAttackMode;
    public float3 enemyPos;
    public float yValue;
}

