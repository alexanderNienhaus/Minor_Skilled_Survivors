using Unity.Entities;

public partial class RessourceSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<Resource>();
        EventBus<OnResourceChangedEvent>.OnEvent += OnResourceChangedUI;
    }

    protected override void OnDestroy()
    {
        EventBus<OnResourceChangedEvent>.OnEvent -= OnResourceChangedUI;
    }

    private void OnResourceChangedUI(OnResourceChangedEvent pOnResourceChangedEvent)
    {
        SystemAPI.GetSingletonRW<Resource>().ValueRW.currentRessourceCount += pOnResourceChangedEvent.resource;
    }

    protected override void OnUpdate()
    {
        EventBus<OnResourceChangedUIEvent>.Publish(new OnResourceChangedUIEvent(SystemAPI.GetSingleton<Resource>().currentRessourceCount));
    }

    public int GetResourceCount()
    {
        return SystemAPI.GetSingleton<Resource>().currentRessourceCount;
    }
}
