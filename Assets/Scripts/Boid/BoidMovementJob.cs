using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using RaycastHit = Unity.Physics.RaycastHit;

[BurstCompile]
public partial struct BoidMovementJob : IJobEntity
{
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Boid> allBoids;
    [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<LocalTransform> allLocalTransforms;
    [ReadOnly] public CollisionWorld collisionWorld;
    public BoidSettings boidSettings;
    public float deltaTime;

    [BurstCompile]
    public void Execute(ref Boid boidA, ref LocalTransform localTransformA, ref PhysicsVelocity physicsVelocityA)
    {
        boidA.numPerceivedFlockmates = 0;
        boidA.avgFlockHeading = 0;
        boidA.centreOfFlockmates = 0;
        boidA.avgAvoidanceHeading = 0;

        for (int i = 0; i < allBoids.Length; i++)
        {
            Boid boidB = allBoids[i];

            if (boidA.id == boidB.id)
                continue;

            LocalTransform localTransformB = allLocalTransforms[i];

            float3 offset = localTransformB.Position - localTransformA.Position;
            float sqrDst = offset.x * offset.x + offset.y * offset.y + offset.z * offset.z;

            if (sqrDst >= boidSettings.perceptionRadius * boidSettings.perceptionRadius)
                continue;

            boidA.numPerceivedFlockmates += 1;
            boidA.avgFlockHeading += localTransformB.Forward();
            boidA.centreOfFlockmates += localTransformB.Position;

            if (sqrDst >= boidSettings.avoidanceRadius * boidSettings.avoidanceRadius)
                continue;

            boidA.avgAvoidanceHeading -= offset / sqrDst;
        }
        UpdateBoid(ref boidA, ref localTransformA, ref physicsVelocityA, collisionWorld, boidSettings, deltaTime);
    }

    [BurstCompile]
    private void UpdateBoid(ref Boid boid, ref LocalTransform localTransform, ref PhysicsVelocity physicsVelocity, CollisionWorld collisionWorld, BoidSettings boidSettings, float deltaTime)
    {
        float3 acceleration = float3.zero;

        if (!math.all(boid.targetPosition == float3.zero))
        {
            float3 offsetToTarget = boid.targetPosition - localTransform.Position;
            acceleration = SteerTowards(offsetToTarget, boid, boidSettings) * boidSettings.targetWeight;
        }

        if (boid.numPerceivedFlockmates != 0)
        {
            boid.centreOfFlockmates /= boid.numPerceivedFlockmates;

            float3 offsetToFlockmatesCentre = (boid.centreOfFlockmates - localTransform.Position);

            float3 alignmentForce = SteerTowards(boid.avgFlockHeading, boid, boidSettings) * boidSettings.alignWeight;
            float3 cohesionForce = SteerTowards(offsetToFlockmatesCentre, boid, boidSettings) * boidSettings.cohesionWeight;
            float3 seperationForce = SteerTowards(boid.avgAvoidanceHeading, boid, boidSettings) * boidSettings.seperateWeight;

            acceleration += alignmentForce;
            acceleration += cohesionForce;
            acceleration += seperationForce;
        }

        if (IsHeadingForCollision(localTransform, collisionWorld, boidSettings))
        {
            float3 collisionAvoidDir = ObstacleRays(localTransform, collisionWorld, boidSettings);
            float3 collisionAvoidForce = SteerTowards(collisionAvoidDir, boid, boidSettings) * boidSettings.avoidCollisionWeight;
            acceleration += collisionAvoidForce;
        }

        boid.velocity += acceleration * deltaTime;
        float speed = math.length(boid.velocity);
        float3 dir = boid.velocity / speed;
        speed = math.clamp(speed, boidSettings.minSpeed, boidSettings.maxSpeed);
        boid.velocity = dir * speed;

        localTransform.Position += boid.velocity * deltaTime;
        //physicsVelocity.Linear = boid.velocity * deltaTime;
        localTransform.Rotation = quaternion.LookRotation(dir, localTransform.Up());
    }

    [BurstCompile]
    private float3 SteerTowards(float3 vector, Boid boid, BoidSettings boidSettings)
    {
        float3 v = math.normalizesafe(vector, float3.zero) * boidSettings.maxSpeed - boid.velocity;
        return math.normalize(v) * math.min(math.length(v), boidSettings.maxSteerForce);
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
    private bool IsHeadingForCollision(LocalTransform localTransform, CollisionWorld collisionWorld, BoidSettings boidSettings)
    {
        if (Raycast(localTransform.Position, localTransform.Position + localTransform.Forward() * boidSettings.collisionAvoidDst, out RaycastHit pRaycastHit)) //collisionWorld.SphereCast(localTransform.Position, boidSettings.boundsRadius, localTransform.Forward(), boidSettings.collisionAvoidDst, filter)
        {
            return true;
        }
        return false;
    }

    [BurstCompile]
    private float3 ObstacleRays(LocalTransform localTransform, CollisionWorld collisionWorld, BoidSettings boidSettings)
    {
        NativeArray<float3> rayDirections = GetDirections();

        for (int i = 0; i < rayDirections.Length; i++)
        {
            float3 dir = localTransform.TransformDirection(rayDirections[i]);
            if (!Raycast(localTransform.Position, localTransform.Position + dir * boidSettings.collisionAvoidDst, out _)) //collisionWorld.SphereCast(localTransform.Position, boidSettings.boundsRadius, dir, boidSettings.collisionAvoidDst, filter))
            {
                return dir;
            }
        }
        rayDirections.Dispose();
        return localTransform.Forward();
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
