using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using RaycastHit = Unity.Physics.RaycastHit;
using System.Numerics;

[BurstCompile]
public partial struct BoidMovementJob : IJobEntity
{
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Boid> allBoids;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<LocalTransform> allLocalTransforms;
    [ReadOnly] public CollisionWorld collisionWorld;
    public BoidSettings boidSettings;
    public float deltaTime;

    [BurstCompile]
    public void Execute(ref Boid pBoidA, ref LocalTransform pLocalTransformA)
    {
        pBoidA.numPerceivedFlockmates = 0;
        pBoidA.avgFlockHeading = 0;
        pBoidA.centreOfFlockmates = 0;
        pBoidA.avgAvoidanceHeading = 0;

        for (int i = 0; i < allBoids.Length; i++)
        {
            Boid boidB = allBoids[i];

            if (pBoidA.id == boidB.id)
                continue;

            LocalTransform localTransformB = allLocalTransforms[i];

            float3 offset = localTransformB.Position - pLocalTransformA.Position;
            float sqrDst = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;

            if (sqrDst >= boidSettings.perceptionRadius * boidSettings.perceptionRadius)
                continue;

            pBoidA.numPerceivedFlockmates += 1;
            pBoidA.avgFlockHeading += localTransformB.Forward();
            pBoidA.centreOfFlockmates += localTransformB.Position;

            if (sqrDst >= boidSettings.avoidanceRadius * boidSettings.avoidanceRadius)
                continue;

            pBoidA.avgAvoidanceHeading -= offset / sqrDst;
        }
        UpdateBoid(ref pBoidA, ref pLocalTransformA, boidSettings, deltaTime);
    }

    [BurstCompile]
    private void UpdateBoid(ref Boid pBoid, ref LocalTransform pLocalTransform, BoidSettings pBoidSettings, float pDeltaTime)
    {
        float3 acceleration = float3.zero;

        if (!math.all(pBoid.targetPosition == float3.zero))
        {
            float3 offsetToTarget = pBoid.targetPosition - pLocalTransform.Position;
            acceleration = SteerTowards(offsetToTarget, pBoid, pBoidSettings) * pBoidSettings.targetWeight;
        }

        if (pBoid.numPerceivedFlockmates != 0)
        {
            pBoid.centreOfFlockmates /= pBoid.numPerceivedFlockmates;

            float3 offsetToFlockmatesCentre = (pBoid.centreOfFlockmates - pLocalTransform.Position);

            float3 alignmentForce = SteerTowards(pBoid.avgFlockHeading, pBoid, pBoidSettings) * pBoidSettings.alignWeight;
            float3 cohesionForce = SteerTowards(offsetToFlockmatesCentre, pBoid, pBoidSettings) * pBoidSettings.cohesionWeight;
            float3 avoidanceForce = SteerTowards(pBoid.avgAvoidanceHeading, pBoid, pBoidSettings) * pBoidSettings.seperateWeight;

            acceleration += alignmentForce;
            acceleration += cohesionForce;
            acceleration += avoidanceForce;
        }

        if (IsHeadingForCollision(pLocalTransform, pBoidSettings))
        {
            float3 collisionAvoidDir = ObstacleRays(pLocalTransform, pBoidSettings);
            float3 collisionAvoidForce = SteerTowards(collisionAvoidDir, pBoid, pBoidSettings) * pBoidSettings.avoidCollisionWeight;
            acceleration += collisionAvoidForce;
        }

        pBoid.velocity += acceleration * pDeltaTime;
        float speed = math.length(pBoid.velocity);
        float3 dir = pBoid.velocity / speed;
        speed = math.clamp(speed, pBoidSettings.minSpeed, pBoidSettings.maxSpeed);
        pBoid.velocity = dir * speed;

        pLocalTransform.Position += pBoid.velocity * pDeltaTime;
        //physicsVelocity.Linear = boid.velocity * deltaTime;
        pLocalTransform.Rotation = quaternion.LookRotation(dir, pLocalTransform.Up());
    }

    [BurstCompile]
    private float3 SteerTowards(float3 pDirection, Boid pBoid, BoidSettings pBoidSettings)
    {
        float3 direction = math.normalizesafe(pDirection, float3.zero) * pBoidSettings.maxSpeed - pBoid.velocity;
        return math.normalize(direction) * math.min(math.length(direction), pBoidSettings.maxSteerForce);
    }

    [BurstCompile]
    private bool Raycast(float3 pRayStart, float3 pRayEnd, out RaycastHit pRaycastHit)
    {        
        RaycastInput raycastInput = new RaycastInput
        {
            Start = pRayStart,
            End = pRayEnd,
            Filter = new CollisionFilter
            {
                BelongsTo = (uint)CollisionLayers.Boid,
                CollidesWith = (uint)(CollisionLayers.Ground | CollisionLayers.Building) //CollisionLayers.Walls | 
            }
        };
        return collisionWorld.CastRay(raycastInput, out pRaycastHit);
    }

    [BurstCompile]
    private bool IsHeadingForCollision(LocalTransform pLocalTransform, BoidSettings pBoidSettings)
    {
        if (Raycast(pLocalTransform.Position, pLocalTransform.Position + pLocalTransform.Forward() * pBoidSettings.collisionAvoidDst, out _)) //collisionWorld.SphereCast(localTransform.Position, boidSettings.boundsRadius, localTransform.Forward(), boidSettings.collisionAvoidDst, filter)
            return true;

        return false;
    }

    [BurstCompile]
    private float3 ObstacleRays(LocalTransform pLocalTransform, BoidSettings pBoidSettings)
    {
        NativeArray<float3> rayDirections = GetDirections();

        for (int i = 0; i < rayDirections.Length; i++)
        {
            float3 dir = pLocalTransform.TransformDirection(rayDirections[i]);
            if (!Raycast(pLocalTransform.Position, pLocalTransform.Position + dir * pBoidSettings.collisionAvoidDst, out _)) //collisionWorld.SphereCast(localTransform.Position, boidSettings.boundsRadius, dir, boidSettings.collisionAvoidDst, filter))
                return dir;

        }
        rayDirections.Dispose();
        return pLocalTransform.Forward();
    }

    [BurstCompile]
    private NativeArray<float3> GetDirections()
    {
        int numViewDirections = 300;
        NativeArray<float3> directions = new (numViewDirections, Allocator.Temp);

        float goldenRatio = (1 + math.sqrt(5)) / 2;
        float angleIncrement = math.PI * 2 * goldenRatio;

        for (int i = 0; i < numViewDirections; i++)
        {
            float t = (float)i / numViewDirections;
            float inclination = math.acos(1 - 2 * t);
            float azimuth = angleIncrement * i;

            float x = math.sin(inclination) * math.cos(azimuth);
            float y = math.sin(inclination) * math.sin(azimuth);
            float z = math.cos(inclination);
            directions[i] = new float3(x, y, z);
        }

        return directions;
    }
}
