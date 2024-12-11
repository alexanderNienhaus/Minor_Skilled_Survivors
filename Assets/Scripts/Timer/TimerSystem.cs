using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class TimerSystem : SystemBase
{
    private WaveSystem waveSystem;
    private Timer timer;
    private bool isFighting;
    private float currentBuildTime;

    protected override void OnCreate()
    {
        //RequireForUpdate<WaveSystem>();
        RequireForUpdate<Timer>();
        BuildPhaseStart();
    }

    protected override void OnUpdate()
    {
        if (isFighting)
        {
            return;
        }

        //Debug.Log("Building Phase: " + currentBuildTime);
        timer = SystemAPI.GetSingleton<Timer>();
        waveSystem = World.GetExistingSystemManaged<WaveSystem>();

        currentBuildTime += SystemAPI.Time.DeltaTime;
        if (currentBuildTime > timer.maxBuildTime)
        {
            isFighting = true;
            waveSystem.NextWave();
        }
    }

    public void BuildPhaseStart()
    {
        isFighting = false;
        currentBuildTime = 0;
    }
}
