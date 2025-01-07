using Unity.Entities;
using Unity.Burst;

[BurstCompile]
public struct SelectionIndicatorData : ICleanupComponentData
{
    public Entity selectionIndicator;
}