using Unity.Collections;
using Unity.Entities;

public class Event
{
}

public class OnSelectedPlacableObjectChanged : Event
{

    public OnSelectedPlacableObjectChanged()
    {
    }
}

public class OnMoveCommandIssued : Event
{
    public NativeArray<Entity> units;

    public OnMoveCommandIssued(NativeArray<Entity> pUnits)
    {
        units = pUnits;
    }
}

public class OnSelectedUnitCountChangeEvent : Event
{
    public int selectedUnitCount;

    public OnSelectedUnitCountChangeEvent(int pUnitCount)
    {
        selectedUnitCount = pUnitCount;
    }
}
