using UnityEngine;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Transforms;
using Unity.Physics;

using Ray = UnityEngine.Ray;
using RaycastHit = Unity.Physics.RaycastHit;
using Material = Unity.Physics.Material;
using Collider = Unity.Physics.Collider;

[Flags]
public enum CollisionLayers
{
    Selection = 1 << 0,
    Ground = 1 << 1,
    Tanks = 1 << 2,
    Walls = 1 << 3,
    Boid = 1 << 4,
    Building = 1 << 5,
    Drone = 1 << 6,
    Projectile = 1 << 7,
    AATurret = 1 << 8,
    Radiostation = 1 << 9,
    Base = 1 << 10
}

[BurstCompile]
//[UpdateAfter(typeof(UnitInformationSystem))]
public partial class UnitSelectionSystem : SystemBase
{
    private PhysicsWorld physicsWorld;
    private CollisionWorld collisionWorld;
    private EntityArchetype selectionArchetype;
    private float3 mouseStartPos;
    private bool isDragging;
    private int minDragDistance = 25;
    private float selectionAreaDepth = 300;
    private string selectionVolumeName = "SelectionVolume";

    public bool GetIsDragging()
    {
        return isDragging;
    }

    public float3 GetMouseStartPos()
    {
        return mouseStartPos;
    }

    protected override void OnCreate()
    {
        selectionArchetype = EntityManager.CreateArchetype(typeof(PhysicsCollider), typeof(LocalToWorld), typeof(SelectionVolumeTag), typeof(PhysicsWorldIndex));
    }

    protected override void OnUpdate()
    {
        physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        if (Input.GetMouseButtonDown(0))
        {
            mouseStartPos = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) && !isDragging && math.distance(mouseStartPos, Input.mousePosition) > minDragDistance)
        {
            isDragging = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (!Input.GetKey(KeyCode.LeftShift))
            {
                DeselectAllUnits();
            }

            if (!isDragging)
            {
                SelectSingleUnit();
            }
            else
            {
                SelectMultipleUnits();
            }
            isDragging = false;
        }
    }

    private void SelectSingleUnit()
    {
        collisionWorld = physicsWorld.CollisionWorld;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 rayStart = ray.origin;
        Vector3 rayEnd = ray.GetPoint(100f);

        if (Raycast(rayStart, rayEnd, out RaycastHit raycastHit))
        {
            Entity hitEntity = physicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
            if (EntityManager.HasComponent<SelectedUnitTag>(hitEntity))
            {
                bool isEnabled = EntityManager.IsComponentEnabled<SelectedUnitTag>(hitEntity);
                if (isEnabled)
                {
                    //Deselect
                    EntityManager.SetComponentEnabled<SelectedUnitTag>(hitEntity, false);
                }
                else
                {
                    //Select
                    EntityManager.SetComponentEnabled<SelectedUnitTag>(hitEntity, true);
                }
            }
        } else
        {
            DeselectAllUnits();
        }
    }

    private void SelectMultipleUnits()
    {
        float3 topLeft = math.min(mouseStartPos, Input.mousePosition);
        float3 botRight = math.max(mouseStartPos, Input.mousePosition);

        Rect rect = Rect.MinMaxRect(topLeft.x, topLeft.y, botRight.x, botRight.y);

        Ray[] cornerRays = new[]
        {
            Camera.main.ScreenPointToRay(rect.min),
            Camera.main.ScreenPointToRay(rect.max),
            Camera.main.ScreenPointToRay(new Vector2(rect.xMin, rect.yMax)),
            Camera.main.ScreenPointToRay(new Vector2(rect.xMax, rect.yMin))
        };

        NativeArray<float3> vertices = new NativeArray<float3>(5, Allocator.Temp);
        for (int i = 0; i < cornerRays.Length; i++)
        {
            vertices[i] = cornerRays[i].GetPoint(selectionAreaDepth);
        }
        vertices[4] = Camera.main.transform.position;

        BlobAssetReference<Collider> selectionVolume = CreateSelectionVolume(vertices);

        Entity selectionEntity = EntityManager.CreateEntity(selectionArchetype);
        EntityManager.SetName(selectionEntity, selectionVolumeName);
        EntityManager.SetComponentData(selectionEntity, new PhysicsCollider { Value = selectionVolume });
        vertices.Dispose();
    }

    private BlobAssetReference<Collider> CreateSelectionVolume(NativeArray<float3> vertices)
    {
        CollisionFilter collisionFilter = new CollisionFilter
        {
            BelongsTo = (uint)CollisionLayers.Selection,
            CollidesWith = (uint)CollisionLayers.Tanks
        };

        Material physicsMaterial = Material.Default;
        physicsMaterial.CollisionResponse = CollisionResponsePolicy.RaiseTriggerEvents;

        ConvexHullGenerationParameters convexHullGenerationParameters = new ConvexHullGenerationParameters
        {
            BevelRadius = 0,
            MinimumAngle = 0,
            SimplificationTolerance = 0
        };

        BlobAssetReference<Collider> selectionCollider = ConvexCollider.Create(vertices, convexHullGenerationParameters, collisionFilter, physicsMaterial);
        return selectionCollider;
    }

    private void DeselectAllUnits()
    {
        foreach ((RefRO<SelectedUnitTag> selectedTag, Entity entity)
            in SystemAPI.Query<RefRO<SelectedUnitTag>>().WithEntityAccess())
        {
            EntityManager.SetComponentEnabled<SelectedUnitTag>(entity, false);
        }
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
                BelongsTo = (uint)CollisionLayers.Selection,
                CollidesWith = (uint)(CollisionLayers.Ground | CollisionLayers.Tanks)
            }
        };
        return collisionWorld.CastRay(raycastInput, out pRaycastHit);
    }
}
