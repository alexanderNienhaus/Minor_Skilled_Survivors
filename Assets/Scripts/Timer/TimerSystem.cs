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

        timer = SystemAPI.GetSingleton<Timer>();
        waveSystem = World.GetExistingSystemManaged<WaveSystem>();

        currentBuildTime += SystemAPI.Time.DeltaTime;
        EventBus<OnTimeChangedEvent>.Publish(new OnTimeChangedEvent((int)math.floor(timer.maxBuildTime - currentBuildTime), true));

        if (currentBuildTime <= timer.maxBuildTime - 1)
            return;

        isFighting = true;
        waveSystem.NextWave();
    }

    private void BuildPhaseEnd()
    {
        timer = SystemAPI.GetSingleton<Timer>();
        currentBuildTime = timer.maxBuildTime;
    }

    public void SkipTurn()
    {
        if (isFighting)
            return;

        BuildPhaseEnd();
    }

    public void BuildPhaseStart()
    {
        isFighting = false;
        currentBuildTime = 0;
    }

    public bool IsBuildTime()
    {
        return !isFighting;
    }
}
