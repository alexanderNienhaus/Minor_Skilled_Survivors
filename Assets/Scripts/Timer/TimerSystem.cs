using Unity.Entities;
using Unity.Mathematics;

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
            EventBus<OnTimeChangedEvent>.Publish(new OnTimeChangedEvent(0, false));
            return;
        }

        //Debug.Log("Building Phase: " + currentBuildTime);
        timer = SystemAPI.GetSingleton<Timer>();
        waveSystem = World.GetExistingSystemManaged<WaveSystem>();

        currentBuildTime += SystemAPI.Time.DeltaTime;
        EventBus<OnTimeChangedEvent>.Publish(new OnTimeChangedEvent((int)math.floor(timer.maxBuildTime - currentBuildTime), true));

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
