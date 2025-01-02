using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class RegisterMapLayoutSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Enabled = false;

        GridObjectPlacement gridObjectPlacementInstance = GridObjectPlacement.Instance;
        foreach ((RefRO<RegisterMapLayout> registerMapLayout, RefRO<LocalTransform> localTransform)
            in SystemAPI.Query<RefRO<RegisterMapLayout>, RefRO<LocalTransform>>())
        {
            PlacedObjectTypeSO.Dir direction = PlacedObjectTypeSO.GetRotationDirection(registerMapLayout.ValueRO.rotationAngle);
            gridObjectPlacementInstance.SetDirection(direction);
            gridObjectPlacementInstance.SetPlacedObjectTypeSO(registerMapLayout.ValueRO.type);

            float3 positionOffset = float3.zero;
            switch (direction)
            {
                case PlacedObjectTypeSO.Dir.Left:
                    positionOffset = new float3(0, 0, registerMapLayout.ValueRO.halfBoundsX * 2);
                    break;
                case PlacedObjectTypeSO.Dir.Right:
                    positionOffset = new float3(registerMapLayout.ValueRO.halfBoundsZ * 2, 0, 0);
                    break;
                case PlacedObjectTypeSO.Dir.Up:
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
