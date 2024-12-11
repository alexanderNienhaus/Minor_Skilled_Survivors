using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial class TimerSystem : SystemBase
{
    private Timer timer;
    private bool isFighting;
    private float currentBuildTime;    

    protected override void OnCreate()
    {
        timer = SystemAPI.GetSingleton<Timer>();
    }

    protected override void OnUpdate()
    {
    }

    public void BuildPhaseStart()
    {
        isFighting = false;
        currentBuildTime = timer.maxBuildTime;
    }
}
