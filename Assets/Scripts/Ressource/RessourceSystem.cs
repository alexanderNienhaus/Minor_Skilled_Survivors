using Unity.Entities;

public partial class RessourceSystem : SystemBase
{
    private void OnResource(OnResourceChangedEvent pOnResourceChangedEvent)
    {
        SystemAPI.GetSingletonRW<Ressource>().ValueRW.currentRessourceCount += pOnResourceChangedEvent.resource;
        EventBus<OnResourceChangedUIEvent>.Publish(new OnResourceChangedUIEvent(SystemAPI.GetSingleton<Ressource>().currentRessourceCount));
    }

    protected override void OnCreate()
    {
        EventBus<OnResourceChangedEvent>.OnEvent += OnResource;
        RequireForUpdate<Ressource>();
    }

    protected override void OnUpdate()
    {
        Enabled = false;
        EventBus<OnResourceChangedEvent>.Publish(new OnResourceChangedEvent(0));
    }

    protected override void OnDestroy()
    {
        EventBus<OnResourceChangedEvent>.OnEvent -= OnResource;
    }

    public int GetResourceCount()
    {
        return SystemAPI.GetSingleton<Ressource>().currentRessourceCount;
    }
}
