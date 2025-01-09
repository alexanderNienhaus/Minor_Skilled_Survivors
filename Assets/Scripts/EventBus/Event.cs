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
    public int resource;

    public OnResourceChangedEvent(int pResource)
    {
        resource = pResource;
    }
}

public class OnResourceChangedUIEvent : CustomEvent
{
    public int resource;

    public OnResourceChangedUIEvent(int pResource)
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

public class OnEndGameEvent : CustomEvent
{
    public bool won;

    public OnEndGameEvent(bool pWon)
    {
        won = pWon;
    }
}

public class OnInfoMenuTextChangeEvent : CustomEvent
{
    public string text;

    public OnInfoMenuTextChangeEvent(string pText)
    {
        text = pText;
    }
}

public class OnSceneChangeEvent : CustomEvent
{
    public int id;

    public OnSceneChangeEvent(int pId)
    {
        id = pId;
    }
}