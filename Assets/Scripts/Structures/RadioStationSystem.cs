using Unity.Entities;
using Unity.Burst;
using Unity.Transforms;

[BurstCompile]
public partial struct RadioStationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        /*
        foreach ((RefRW<RadioStation> radioStation, RefRO<LocalTransform> localTransform)
            in SystemAPI.Query<RefRW<RadioStation>, RefRO<LocalTransform>>())
        {

        }
        */
    }
}