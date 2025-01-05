using Unity.Entities;
using UnityEngine;

public partial class RessourceSystem : SystemBase
{
    private RefRW<Ressource> ressource;

    protected override void OnCreate()
    {
        RequireForUpdate<Ressource>();
    }

    protected override void OnUpdate()
    {
        ressource = SystemAPI.GetSingletonRW<Ressource>();
        EventBus<OnResourceChangedEvent>.Publish(new OnResourceChangedEvent(ressource.ValueRW.currentRessourceCount));
    }

    public void AddRessource(int add)
    {
        ressource.ValueRW.currentRessourceCount += add;
        //Debug.Log("Ressources: " + ressource.ValueRO.currentRessourceCount);
    }

    public int GetResourceCount()
    {
        return ressource.ValueRO.currentRessourceCount;
    }
}
