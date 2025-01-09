using Unity.Entities;
using UnityEngine;

public class SkipTurn : MonoBehaviour
{
    public void SkipTurnBtn()
    {
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimerSystem>().SkipTurn();   
    }
}
