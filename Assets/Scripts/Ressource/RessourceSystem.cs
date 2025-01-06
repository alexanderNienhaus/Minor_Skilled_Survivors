using Unity.Entities;

public partial class RessourceSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<Ressource>();
    }

    protected override void OnUpdate()
    {
        EventBus<OnResourceChangedUIEvent>.Publish(new OnResourceChangedUIEvent(SystemAPI.GetSingleton<Ressource>().currentRessourceCount));
    }

    public int GetResourceCount()
    {
        return SystemAPI.GetSingleton<Ressource>().currentRessourceCount;
    }
}
