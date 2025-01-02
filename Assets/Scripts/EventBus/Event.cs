using Unity.Collections;
using Unity.Entities;

public class CustomEvent
{
}

public class OnSelectedPlacableObjectChanged : CustomEvent
{

    public OnSelectedPlacableObjectChanged()
    {
    }
}

public class OnMoveCommandIssued : CustomEvent
{
    public NativeArray<Entity> units;

    public OnMoveCommandIssued(NativeArray<Entity> pUnits)
    {
        units = pUnits;
    }
}

public class OnSelectedUnitCountChangeEvent : CustomEvent
{
    public int selectedUnitCount;

    public OnSelectedUnitCountChangeEvent(int pUnitCount)
    {
        selectedUnitCount = pUnitCount;
    }
}

public class OnBaseHPEvent : CustomEvent
{
    public float baseHP;

    public OnBaseHPEvent(float pBaseHP)
    {
        baseHP = pBaseHP;
    }
}

public class OnResourceChangedEvent : CustomEvent
{
    public float resource;

    public OnResourceChangedEvent(float pResource)
    {
        resource = pResource;
    }
}

public class OnTimeChangedEvent : CustomEvent
{
    public int time;
    public bool isActive;

    public OnTimeChangedEvent(int pTime, bool pIsActive)
    {
        time = pTime;
        isActive = pIsActive;
    }
}

public class OnWaveNumberChangedEvent : CustomEvent
{
    public int waveNumber;

    public OnWaveNumberChangedEvent(int pWaveNumber)
    {
        waveNumber = pWaveNumber;
    }
}