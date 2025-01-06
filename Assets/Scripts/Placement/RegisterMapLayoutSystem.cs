using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(WaveSystem))]
public partial class RegisterMapLayoutSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<RegisterMapLayout>();
    }

    protected override void OnUpdate()
    {
        Enabled = false;

        GridObjectPlacementManager gridObjectPlacementInstance = GridObjectPlacementManager.Instance;
        foreach ((RefRO<RegisterMapLayout> registerMapLayout, RefRO<LocalTransform> localTransform)
            in SystemAPI.Query<RefRO<RegisterMapLayout>, RefRO<LocalTransform>>())
        {
            PlacableObjectTypeSO.Dir direction = PlacableObjectTypeSO.GetRotationDirection(registerMapLayout.ValueRO.rotationAngle);
            gridObjectPlacementInstance.SetDirection(direction);
            gridObjectPlacementInstance.SetPlacedObjectTypeSO(registerMapLayout.ValueRO.type);

            float3 positionOffset = float3.zero;
            switch (direction)
            {
                case PlacableObjectTypeSO.Dir.Left:
                    positionOffset = new float3(0, 0, registerMapLayout.ValueRO.halfBoundsX * 2);
                    break;
                case PlacableObjectTypeSO.Dir.Right:
                    positionOffset = new float3(registerMapLayout.ValueRO.halfBoundsZ * 2, 0, 0);
                    break;
                case PlacableObjectTypeSO.Dir.Up:
                    positionOffset = new float3(registerMapLayout.ValueRO.halfBoundsX * 2, 0, registerMapLayout.ValueRO.halfBoundsZ * 2);
                    break;
            }
            gridObjectPlacementInstance.GetGridPosFromWorldPos(
                localTransform.ValueRO.Position - positionOffset,
                out int x, out int z);
            List<Vector2Int> gridPosList = gridObjectPlacementInstance.GetGridPosList(x, z);

            gridObjectPlacementInstance.PlaceBuilding(x, z, gridPosList, false);
        }
    }
}
